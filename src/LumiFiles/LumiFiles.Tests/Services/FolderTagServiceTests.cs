using System;
using System.IO;
using System.Text;
using LumiFiles.Models;
using LumiFiles.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LumiFiles.Tests.Services;

/// <summary>
/// Span #58: 폴더 컬러 태그.
///
/// 이 기능은 <b>사용자의 실제 폴더에 파일을 쓴다</b>. 그래서 검증의 중심은 두 가지다:
///   1. 기존 desktop.ini 내용(커스텀 아이콘 설정 등)을 망가뜨리지 않는가
///   2. 폴더 속성을 건드리지 않는가 — 건드리면 폴더가 목록에서 사라지거나(System)
///      삭제가 불가능해진다(ReadOnly). 둘 다 실측으로 확인된 피해다.
/// </summary>
[TestClass]
public class FolderTagServiceTests
{
    private string _dir = null!;

    [TestInitialize]
    public void Setup()
    {
        _dir = Path.Combine(Path.GetTempPath(), "LumiFilesTagTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            foreach (var f in Directory.GetFiles(_dir))
                File.SetAttributes(f, FileAttributes.Normal);
            Directory.Delete(_dir, recursive: true);
        }
        catch { }
    }

    private string IniPath => Path.Combine(_dir, "desktop.ini");

    private static void WriteIni(string path, string content, Encoding encoding)
    {
        File.WriteAllText(path, content, encoding);
    }

    // ── 기본 왕복 ────────────────────────────────────────────────

    [TestMethod]
    public void SetTag_ThenGet_RoundTripsThroughDisk()
    {
        Assert.IsTrue(new FolderTagService().SetTag(_dir, FolderTagColor.Green));

        // 다른 인스턴스 = 캐시 없음 → 디스크에서 읽어야 한다
        Assert.AreEqual(FolderTagColor.Green, new FolderTagService().Get(_dir));
    }

    [TestMethod]
    public void SetTag_Overwrite_KeepsSingleValue()
    {
        var svc = new FolderTagService();
        Assert.IsTrue(svc.SetTag(_dir, FolderTagColor.Red));
        Assert.IsTrue(svc.SetTag(_dir, FolderTagColor.Blue));

        Assert.AreEqual(FolderTagColor.Blue, new FolderTagService().Get(_dir));
    }

    [TestMethod]
    public void Get_NoDesktopIni_ReturnsNone()
    {
        Assert.AreEqual(FolderTagColor.None, new FolderTagService().Get(_dir));
    }

    [TestMethod]
    public void Get_UnknownColorValue_ReturnsNone()
    {
        WriteIni(IniPath, "[LumiFiles]\r\nTag=Chartreuse\r\n", new UnicodeEncoding(false, true));
        Assert.AreEqual(FolderTagColor.None, new FolderTagService().Get(_dir));
    }

    // ── 폴더 속성을 건드리지 않는다 (회귀 방지) ──────────────────

    /// <summary>
    /// System 을 붙이면 숨김 표시 OFF(기본값)에서 폴더가 목록에서 사라지고,
    /// ReadOnly 를 붙이면 Directory.Delete(recursive) 가 실패한다. 둘 다 금지.
    /// </summary>
    [TestMethod]
    public void SetTag_DoesNotChangeFolderAttributes()
    {
        var before = File.GetAttributes(_dir);

        new FolderTagService().SetTag(_dir, FolderTagColor.Blue);

        var after = File.GetAttributes(_dir);
        Assert.IsFalse(after.HasFlag(FileAttributes.System),
            "System 을 붙이면 숨김 표시 OFF 에서 폴더가 목록에서 사라진다");
        Assert.IsFalse(after.HasFlag(FileAttributes.ReadOnly),
            "ReadOnly 를 붙이면 Directory.Delete(recursive) 가 실패해 폴더를 지울 수 없다");
        Assert.AreEqual(before, after, "폴더 속성이 변경되었다");
    }

    /// <summary>속성 회귀의 실제 피해를 직접 확인 — 태그 후에도 폴더가 지워져야 한다.</summary>
    [TestMethod]
    public void SetTag_FolderRemainsDeletable()
    {
        var target = Path.Combine(_dir, "tagged");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "child.txt"), "x");

        new FolderTagService().SetTag(target, FolderTagColor.Purple);

        Directory.Delete(target, recursive: true);   // 실패하면 테스트가 예외로 깨진다
        Assert.IsFalse(Directory.Exists(target));
    }

    // ── 기존 desktop.ini 보존 ────────────────────────────────────

    [TestMethod]
    public void SetTag_PreservesExistingIniContent()
    {
        WriteIni(IniPath,
            "[.ShellClassInfo]\r\nIconResource=C:\\icons\\my.ico,0\r\nInfoTip=hello\r\n",
            new UnicodeEncoding(false, true));

        Assert.IsTrue(new FolderTagService().SetTag(_dir, FolderTagColor.Orange));

        var text = File.ReadAllText(IniPath);
        StringAssert.Contains(text, "IconResource=C:\\icons\\my.ico,0", "커스텀 아이콘 설정이 사라졌다");
        StringAssert.Contains(text, "InfoTip=hello");
        Assert.AreEqual(FolderTagColor.Orange, new FolderTagService().Get(_dir));
    }

    /// <summary>
    /// UTF-8 BOM 으로 저장된 desktop.ini 는 Win32 INI API 가 섹션을 못 찾아 중복 생성한다.
    /// 정규화가 없으면 기존 [.ShellClassInfo] 설정이 파괴된다.
    /// </summary>
    [TestMethod]
    public void SetTag_Utf8BomIni_DoesNotDestroyExistingSection()
    {
        WriteIni(IniPath,
            "[.ShellClassInfo]\r\nIconResource=C:\\icons\\my.ico,0\r\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        Assert.IsTrue(new FolderTagService().SetTag(_dir, FolderTagColor.Yellow));

        var text = File.ReadAllText(IniPath);
        StringAssert.Contains(text, "IconResource=C:\\icons\\my.ico,0",
            "UTF-8 BOM 파일에서 기존 섹션이 파괴됐다");
        Assert.AreEqual(FolderTagColor.Yellow, new FolderTagService().Get(_dir));

        // 섹션이 중복 생성되지 않아야 한다
        var occurrences = text.Split("[.ShellClassInfo]").Length - 1;
        Assert.AreEqual(1, occurrences, "[.ShellClassInfo] 섹션이 중복 생성되었다");
    }

    // ── 해제 ─────────────────────────────────────────────────────

    [TestMethod]
    public void SetTag_None_RemovesTag()
    {
        var svc = new FolderTagService();
        svc.SetTag(_dir, FolderTagColor.Blue);
        Assert.IsTrue(svc.SetTag(_dir, FolderTagColor.None));

        Assert.AreEqual(FolderTagColor.None, new FolderTagService().Get(_dir));
    }

    [TestMethod]
    public void SetTag_None_DeletesIniWhenItWasOnlyOurs()
    {
        var svc = new FolderTagService();
        svc.SetTag(_dir, FolderTagColor.Blue);
        svc.SetTag(_dir, FolderTagColor.None);

        Assert.IsFalse(File.Exists(IniPath),
            "우리 태그만 있던 desktop.ini 는 해제 시 남기지 않는다");
    }

    [TestMethod]
    public void SetTag_None_KeepsIniWhenOtherContentExists()
    {
        WriteIni(IniPath, "[.ShellClassInfo]\r\nIconResource=x.ico,0\r\n",
            new UnicodeEncoding(false, true));

        var svc = new FolderTagService();
        svc.SetTag(_dir, FolderTagColor.Blue);
        svc.SetTag(_dir, FolderTagColor.None);

        Assert.IsTrue(File.Exists(IniPath), "다른 내용이 있는 desktop.ini 를 지워버렸다");
        StringAssert.Contains(File.ReadAllText(IniPath), "IconResource=x.ico,0");
    }

    // ── desktop.ini 파일 속성 ────────────────────────────────────

    /// <summary>desktop.ini 자체는 숨김+시스템이 관례 (탐색기에 노출되지 않도록).</summary>
    [TestMethod]
    public void SetTag_MarksIniHiddenAndSystem()
    {
        new FolderTagService().SetTag(_dir, FolderTagColor.Blue);

        var attrs = File.GetAttributes(IniPath);
        Assert.IsTrue(attrs.HasFlag(FileAttributes.Hidden), "Hidden 속성 없음");
        Assert.IsTrue(attrs.HasFlag(FileAttributes.System), "System 속성 없음");
    }

    /// <summary>
    /// 숨김+시스템 파일에 다시 쓸 수 있어야 한다.
    /// (File.WriteAllText 는 여기서 UnauthorizedAccessException 을 던진다 —
    ///  그래서 구현이 WritePrivateProfileStringW 를 쓴다)
    /// </summary>
    [TestMethod]
    public void SetTag_CanRewriteHiddenSystemIni()
    {
        var svc = new FolderTagService();
        Assert.IsTrue(svc.SetTag(_dir, FolderTagColor.Blue));
        Assert.IsTrue(svc.SetTag(_dir, FolderTagColor.Red), "두 번째 쓰기 실패");

        Assert.AreEqual(FolderTagColor.Red, new FolderTagService().Get(_dir));
    }

    // ── 대상 경로 제한 ───────────────────────────────────────────

    [DataTestMethod]
    [DataRow(@"archive://C:\a.zip\inner")]
    [DataRow(@"\\server\share\folder")]      // UNC
    [DataRow("")]
    [DataRow("relative\\path")]
    public void IsTaggable_RejectsUnsupportedLocations(string path)
    {
        Assert.IsFalse(FolderTagService.IsTaggable(path));
    }

    [TestMethod]
    public void IsTaggable_AcceptsLocalPath()
    {
        Assert.IsTrue(FolderTagService.IsTaggable(_dir));
    }

    [TestMethod]
    public void SetTag_UncPath_ReturnsFalse()
    {
        Assert.IsFalse(new FolderTagService().SetTag(@"\\server\share\folder", FolderTagColor.Red));
    }

    [TestMethod]
    public void SetTag_MissingFolder_ReturnsFalse()
    {
        Assert.IsFalse(new FolderTagService().SetTag(Path.Combine(_dir, "nope"), FolderTagColor.Red));
    }

    // ── 캐시 ─────────────────────────────────────────────────────

    /// <summary>렌더링 경로는 디스크를 건드리지 않는다 — 읽기 전에는 null.</summary>
    [TestMethod]
    public void TryGetCached_BeforeRead_ReturnsNull()
    {
        new FolderTagService().SetTag(_dir, FolderTagColor.Purple);

        var svc = new FolderTagService();      // 새 인스턴스 = 빈 캐시
        Assert.IsNull(svc.TryGetCached(_dir));

        svc.Get(_dir);
        Assert.AreEqual(FolderTagColor.Purple, svc.TryGetCached(_dir));
    }

    [TestMethod]
    public void Invalidate_ClearsCachedValue()
    {
        var svc = new FolderTagService();
        svc.SetTag(_dir, FolderTagColor.Gray);
        Assert.IsNotNull(svc.TryGetCached(_dir));

        svc.Invalidate(_dir);
        Assert.IsNull(svc.TryGetCached(_dir));
    }
}
