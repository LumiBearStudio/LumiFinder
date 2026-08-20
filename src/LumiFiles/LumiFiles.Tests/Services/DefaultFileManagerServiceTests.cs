using LumiFiles.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LumiFiles.Tests.Services;

/// <summary>
/// Span #55: 기본 파일 관리자 등록이 사라진 실행 파일을 가리켜 모든 폴더가 열리지 않는 문제.
/// 감지 로직의 핵심은 레지스트리 값에서 실행 파일 경로를 뽑아내는 파싱이므로 이를 검증한다.
/// (레지스트리를 실제로 읽는 DetectStaleRegistration/RepairStaleRegistrationIfOwn 은
///  머신 상태에 의존하므로 단위 테스트 대상에서 제외)
/// </summary>
[TestClass]
public class DefaultFileManagerServiceTests
{
    // ── 따옴표로 감싼 정상 형식 ────────────────────────────────
    [TestMethod]
    public void ExtractExePath_QuotedPathWithArgument()
    {
        var cmd = "\"C:\\Program Files\\LumiFiles\\LumiFiles.exe\" \"%1\"";
        Assert.AreEqual("C:\\Program Files\\LumiFiles\\LumiFiles.exe",
            DefaultFileManagerService.ExtractExePath(cmd));
    }

    [TestMethod]
    public void ExtractExePath_QuotedPathWithoutArgument()
    {
        var cmd = "\"C:\\Apps\\LumiFiles.exe\"";
        Assert.AreEqual("C:\\Apps\\LumiFiles.exe", DefaultFileManagerService.ExtractExePath(cmd));
    }

    /// <summary>공백이 든 경로가 따옴표 없이 잘리지 않는지 — 오탐으로 남의 등록을 지우면 안 된다.</summary>
    [TestMethod]
    public void ExtractExePath_UnquotedPathWithArgument_StopsAtFirstSpace()
    {
        var cmd = "C:\\Apps\\LumiFiles.exe %1";
        Assert.AreEqual("C:\\Apps\\LumiFiles.exe", DefaultFileManagerService.ExtractExePath(cmd));
    }

    [TestMethod]
    public void ExtractExePath_UnquotedPathOnly()
    {
        var cmd = "C:\\Apps\\LumiFiles.exe";
        Assert.AreEqual("C:\\Apps\\LumiFiles.exe", DefaultFileManagerService.ExtractExePath(cmd));
    }

    /// <summary>앞뒤 공백이 있어도 경로를 정확히 뽑아야 한다.</summary>
    [TestMethod]
    public void ExtractExePath_TrimsSurroundingWhitespace()
    {
        var cmd = "   \"C:\\Apps\\LumiFiles.exe\" \"%1\"   ";
        Assert.AreEqual("C:\\Apps\\LumiFiles.exe", DefaultFileManagerService.ExtractExePath(cmd));
    }

    /// <summary>환경 변수를 쓴 등록 — 확장은 호출측에서 하므로 원문 그대로 반환.</summary>
    [TestMethod]
    public void ExtractExePath_KeepsEnvironmentVariableUnexpanded()
    {
        var cmd = "\"%LOCALAPPDATA%\\Microsoft\\WindowsApps\\LumiFiles.exe\" \"%1\"";
        Assert.AreEqual("%LOCALAPPDATA%\\Microsoft\\WindowsApps\\LumiFiles.exe",
            DefaultFileManagerService.ExtractExePath(cmd));
    }

    // ── 잘못된 입력은 null (→ 감지에서 건너뜀) ────────────────
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ExtractExePath_NullOrBlank_ReturnsNull(string? cmd)
    {
        Assert.IsNull(DefaultFileManagerService.ExtractExePath(cmd));
    }

    /// <summary>닫는 따옴표가 없는 손상된 값 — 잘못 잘라내지 말고 null 이어야 한다.</summary>
    [TestMethod]
    public void ExtractExePath_UnterminatedQuote_ReturnsNull()
    {
        Assert.IsNull(DefaultFileManagerService.ExtractExePath("\"C:\\Apps\\LumiFiles.exe"));
    }

    /// <summary>빈 따옴표쌍도 경로가 아니므로 null.</summary>
    [TestMethod]
    public void ExtractExePath_EmptyQuotes_ReturnsNull()
    {
        Assert.IsNull(DefaultFileManagerService.ExtractExePath("\"\" \"%1\""));
    }
}
