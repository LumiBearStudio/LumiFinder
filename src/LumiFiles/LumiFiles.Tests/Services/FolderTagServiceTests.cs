using System;
using System.IO;
using LumiFiles.Models;
using LumiFiles.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LumiFiles.Tests.Services;

/// <summary>
/// Span #58: 폴더 컬러 태그. 저장 위치가 사용자의 기존 desktop.ini 이므로
/// <b>남의 내용을 망가뜨리지 않는 것</b>이 가장 중요하다 (커스텀 폴더 아이콘 설정 등).
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

    // ── ParseTag ────────────────────────────────────────────────

    [TestMethod]
    public void ParseTag_ReadsOurSection()
    {
        var ini = "[LumiFiles]\r\nTagColor=Blue\r\n";
        Assert.AreEqual(FolderTagColor.Blue, FolderTagService.ParseTag(ini));
    }

    /// <summary>다른 섹션에 같은 이름의 키가 있어도 우리 것만 읽어야 한다.</summary>
    [TestMethod]
    public void ParseTag_IgnoresSameKeyInOtherSection()
    {
        var ini = "[.ShellClassInfo]\r\nTagColor=Red\r\n\r\n[LumiFiles]\r\nTagColor=Green\r\n";
        Assert.AreEqual(FolderTagColor.Green, FolderTagService.ParseTag(ini));
    }

    [TestMethod]
    public void ParseTag_IsCaseInsensitive()
    {
        var ini = "[lumifiles]\r\ntagcolor=purple\r\n";
        Assert.AreEqual(FolderTagColor.Purple, FolderTagService.ParseTag(ini));
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow("[.ShellClassInfo]\r\nIconResource=x.ico,0\r\n")]     // 우리 섹션 없음
    [DataRow("[LumiFiles]\r\nTagColor=Chartreuse\r\n")]            // 팔레트에 없는 값
    [DataRow("[LumiFiles]\r\n; TagColor=Blue\r\n")]                // 주석 처리됨
    public void ParseTag_UnknownOrAbsent_ReturnsNone(string? ini)
    {
        Assert.AreEqual(FolderTagColor.None, FolderTagService.ParseTag(ini));
    }

    // ── ApplyTag: 기존 내용 보존 ─────────────────────────────────

    /// <summary>커스텀 아이콘 설정이 있는 desktop.ini 에 태그를 추가해도 아이콘 설정이 살아야 한다.</summary>
    [TestMethod]
    public void ApplyTag_PreservesExistingSections()
    {
        var original = "[.ShellClassInfo]\r\nIconResource=C:\\icons\\my.ico,0\r\nInfoTip=hello\r\n";

        var updated = FolderTagService.ApplyTag(original, FolderTagColor.Orange);

        StringAssert.Contains(updated, "IconResource=C:\\icons\\my.ico,0");
        StringAssert.Contains(updated, "InfoTip=hello");
        Assert.AreEqual(FolderTagColor.Orange, FolderTagService.ParseTag(updated));
    }

    [TestMethod]
    public void ApplyTag_ReplacesExistingTag()
    {
        var original = "[LumiFiles]\r\nTagColor=Red\r\n";

        var updated = FolderTagService.ApplyTag(original, FolderTagColor.Blue);

        Assert.AreEqual(FolderTagColor.Blue, FolderTagService.ParseTag(updated));
        // 중복 키가 남지 않아야 한다
        var occurrences = updated.Split("TagColor=").Length - 1;
        Assert.AreEqual(1, occurrences, "TagColor 키가 중복되었다");
    }

    [TestMethod]
    public void ApplyTag_NoneRemovesOnlyOurKey()
    {
        var original = "[.ShellClassInfo]\r\nIconResource=x.ico,0\r\n\r\n[LumiFiles]\r\nTagColor=Green\r\n";

        var updated = FolderTagService.ApplyTag(original, FolderTagColor.None);

        Assert.AreEqual(FolderTagColor.None, FolderTagService.ParseTag(updated));
        StringAssert.Contains(updated, "IconResource=x.ico,0", "다른 섹션이 사라졌다");
    }

    /// <summary>우리 태그만 있던 파일에서 해제하면 내용이 비어야 한다(호출측이 파일을 지움).</summary>
    [TestMethod]
    public void ApplyTag_NoneOnOurOnlyContent_YieldsEmpty()
    {
        var updated = FolderTagService.ApplyTag("[LumiFiles]\r\nTagColor=Gray\r\n", FolderTagColor.None);
        Assert.AreEqual(string.Empty, updated.Trim());
    }

    [TestMethod]
    public void ApplyTag_OnEmptyInput_CreatesSection()
    {
        var updated = FolderTagService.ApplyTag(string.Empty, FolderTagColor.Yellow);
        Assert.AreEqual(FolderTagColor.Yellow, FolderTagService.ParseTag(updated));
    }

    /// <summary>설정 → 변경 → 해제를 반복해도 원본이 그대로 복원돼야 한다.</summary>
    [TestMethod]
    public void ApplyTag_RoundTrip_RestoresOriginal()
    {
        var original = "[.ShellClassInfo]\r\nIconResource=x.ico,0\r\n";

        var tagged = FolderTagService.ApplyTag(original, FolderTagColor.Red);
        var retagged = FolderTagService.ApplyTag(tagged, FolderTagColor.Blue);
        var cleared = FolderTagService.ApplyTag(retagged, FolderTagColor.None);

        Assert.AreEqual(original.Trim(), cleared.Trim());
    }

    // ── 파일 입출력 ──────────────────────────────────────────────

    [TestMethod]
    public void SetTag_ThenLoad_RoundTripsThroughDisk()
    {
        var svc = new FolderTagService();

        Assert.IsTrue(svc.SetTag(_dir, FolderTagColor.Green));
        Assert.AreEqual(FolderTagColor.Green, new FolderTagService().Load(_dir),
            "다른 인스턴스(=캐시 없음)에서도 디스크에서 읽혀야 한다");
    }

    /// <summary>desktop.ini 는 숨김+시스템 속성이 관례 — 그래야 탐색기가 노출하지 않는다.</summary>
    [TestMethod]
    public void SetTag_MarksIniHiddenAndSystem()
    {
        var svc = new FolderTagService();
        svc.SetTag(_dir, FolderTagColor.Blue);

        var attrs = File.GetAttributes(Path.Combine(_dir, "desktop.ini"));
        Assert.IsTrue(attrs.HasFlag(FileAttributes.Hidden), "Hidden 속성 없음");
        Assert.IsTrue(attrs.HasFlag(FileAttributes.System), "System 속성 없음");
    }

    /// <summary>이미 숨김+시스템인 파일에도 다시 쓸 수 있어야 한다(속성 때문에 실패하면 안 됨).</summary>
    [TestMethod]
    public void SetTag_CanRewriteHiddenSystemFile()
    {
        var svc = new FolderTagService();
        Assert.IsTrue(svc.SetTag(_dir, FolderTagColor.Blue));
        Assert.IsTrue(svc.SetTag(_dir, FolderTagColor.Red), "두 번째 쓰기 실패");
        Assert.AreEqual(FolderTagColor.Red, new FolderTagService().Load(_dir));
    }

    /// <summary>
    /// 회귀 방지: 태그를 단 폴더에 <b>System</b> 속성을 붙이면 안 된다.
    /// FolderViewModel 의 열거가 숨김 표시 OFF(기본값)일 때 Hidden|System 을 건너뛰므로
    /// System 을 붙이면 태그를 다는 순간 그 폴더가 목록에서 사라진다.
    /// Windows 가 '폴더 사용자 지정' 시 실제로 쓰는 속성도 ReadOnly 다.
    /// </summary>
    [TestMethod]
    public void SetTag_MarksFolderReadOnly_NotSystem()
    {
        var svc = new FolderTagService();
        svc.SetTag(_dir, FolderTagColor.Blue);

        var attrs = File.GetAttributes(_dir);
        Assert.IsTrue(attrs.HasFlag(FileAttributes.ReadOnly),
            "desktop.ini 가 해석되려면 폴더에 ReadOnly 가 필요하다");
        Assert.IsFalse(attrs.HasFlag(FileAttributes.System),
            "System 을 붙이면 숨김 표시 OFF 상태에서 폴더가 목록에서 사라진다");
        Assert.IsFalse(attrs.HasFlag(FileAttributes.Hidden),
            "폴더 자체를 숨기면 안 된다");
    }

    [TestMethod]
    public void SetTag_None_DeletesIniWhenItWasOnlyOurs()
    {
        var svc = new FolderTagService();
        svc.SetTag(_dir, FolderTagColor.Blue);
        svc.SetTag(_dir, FolderTagColor.None);

        Assert.IsFalse(File.Exists(Path.Combine(_dir, "desktop.ini")),
            "우리 태그만 있던 desktop.ini 는 해제 시 남기지 않는다");
    }

    /// <summary>사용자의 기존 desktop.ini 는 태그 해제 후에도 남아 있어야 한다.</summary>
    [TestMethod]
    public void SetTag_None_KeepsIniWhenOtherContentExists()
    {
        var ini = Path.Combine(_dir, "desktop.ini");
        File.WriteAllText(ini, "[.ShellClassInfo]\r\nIconResource=x.ico,0\r\n");

        var svc = new FolderTagService();
        svc.SetTag(_dir, FolderTagColor.Blue);
        svc.SetTag(_dir, FolderTagColor.None);

        Assert.IsTrue(File.Exists(ini), "다른 내용이 있는 desktop.ini 를 지워버렸다");
        StringAssert.Contains(File.ReadAllText(ini), "IconResource=x.ico,0");
    }

    [TestMethod]
    public void Load_NoDesktopIni_ReturnsNone()
    {
        Assert.AreEqual(FolderTagColor.None, new FolderTagService().Load(_dir));
    }

    [TestMethod]
    public void Load_MissingFolder_ReturnsNone()
    {
        var svc = new FolderTagService();
        Assert.AreEqual(FolderTagColor.None, svc.Load(Path.Combine(_dir, "nope")));
    }

    [TestMethod]
    public void SetTag_MissingFolder_ReturnsFalse()
    {
        var svc = new FolderTagService();
        Assert.IsFalse(svc.SetTag(Path.Combine(_dir, "nope"), FolderTagColor.Red));
    }

    // ── 캐시 동작 ───────────────────────────────────────────────

    /// <summary>목록 렌더링 경로는 디스크를 건드리지 않는다 — 읽기 전에는 None.</summary>
    [TestMethod]
    public void TryGetCached_BeforeLoad_ReturnsNoneWithoutDiskAccess()
    {
        var svc = new FolderTagService();
        new FolderTagService().SetTag(_dir, FolderTagColor.Purple);

        Assert.AreEqual(FolderTagColor.None, svc.TryGetCached(_dir));
        svc.Load(_dir);
        Assert.AreEqual(FolderTagColor.Purple, svc.TryGetCached(_dir));
    }

    [TestMethod]
    public void TagChanged_FiresOnlyWhenValueChanges()
    {
        var svc = new FolderTagService();
        int count = 0;
        svc.TagChanged += _ => count++;

        svc.Load(_dir);            // None → 변화 있음(최초 기록)
        int afterFirst = count;
        svc.Load(_dir);            // 동일 → 이벤트 없어야 함

        Assert.AreEqual(afterFirst, count, "값이 그대로인데 이벤트가 또 발생했다");
    }
}
