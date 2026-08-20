using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LumiFiles.Services
{
    public class DefaultFileManagerService
    {
        private const string FolderOpenCommandKey = @"Software\Classes\Folder\shell\open\command";
        private const string DirectoryOpenCommandKey = @"Software\Classes\Directory\shell\open\command";
        private const string DriveOpenCommandKey = @"Software\Classes\Drive\shell\open\command";

        /// <summary>SetAsDefault가 기록하는 세 키 — 감지/복구 시 함께 다룬다.</summary>
        private static readonly string[] OpenCommandKeys =
        {
            FolderOpenCommandKey, DirectoryOpenCommandKey, DriveOpenCommandKey,
        };

        /// <summary>
        /// 스스로가 남긴 것으로 간주할 실행 파일 이름.
        /// Span 시절 등록분도 포함 — LumiFiles 는 Span 포크라 같은 사용자가
        /// Span 을 기본 파일 관리자로 두고 삭제해 폴더가 안 열리는 상태일 수 있다.
        /// </summary>
        private static readonly string[] OwnExeNames =
        {
            "LumiFiles.exe", "LumiFinder.exe", "Span.exe", "SpanFinder.exe",
        };

        // ── SHChangeNotify: 셸에게 파일 association 변경됨을 알림 (explorer 재시작 없이 즉시 반영) ──
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;
        private const uint SHCNF_FLUSH = 0x1000;

        private static void NotifyShellAssocChanged()
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[DefaultFM] SHChangeNotify failed: {ex.Message}");
            }
        }

        /// <summary>
        /// [주의: 현재 호출 측에서 비활성 처리됨]
        /// Win11 explorer.exe 셸 association 캐시 무효화 — 강제 재시작.
        /// SHChangeNotify만으로는 explorer가 갱신 안 하는 경우가 있음.
        /// 사용자가 열어둔 탐색기 창이 모두 닫히는 위험 있음 → 명시 동의 후에만 호출할 것.
        /// </summary>
        public static void RestartExplorer()
        {
            try
            {
                Helpers.DebugLogger.Log("[DefaultFM] Restarting explorer.exe to refresh association cache");
                var kill = Process.Start(new ProcessStartInfo
                {
                    FileName = "taskkill.exe",
                    Arguments = "/F /IM explorer.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                kill?.WaitForExit(3000);

                // OS가 자동 재시작 안 한 경우 fallback (보통은 자동 시작됨)
                System.Threading.Thread.Sleep(400);
                var running = Process.GetProcessesByName("explorer");
                if (running.Length == 0)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        UseShellExecute = true
                    });
                    Helpers.DebugLogger.Log("[DefaultFM] explorer.exe manually restarted");
                }
                else
                {
                    Helpers.DebugLogger.Log($"[DefaultFM] explorer.exe auto-restarted ({running.Length} instance)");
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[DefaultFM] RestartExplorer failed: {ex.Message}");
            }
        }

        // AppExecutionAlias 경로 (WindowsApps에 등록됨)
        private static readonly string AliasPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WindowsApps", "LumiFiles.exe");

        /// <summary>
        /// regedit /s + runas로 기본 파일 관리자 등록.
        /// UAC 다이얼로그가 표시됨.
        /// </summary>
        public async Task<bool> SetAsDefaultAsync()
        {
            try
            {
                var regContent = GenerateSetDefaultReg();
                var tempPath = Path.Combine(Path.GetTempPath(), "LumiFilesSetDefault.reg");
                await File.WriteAllTextAsync(tempPath, regContent);

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "regedit.exe",
                    Arguments = $"/s \"{tempPath}\"",
                    UseShellExecute = true,
                    Verb = "runas"  // UAC 승격
                });

                if (process != null)
                    await process.WaitForExitAsync();

                // temp 파일 정리
                try { File.Delete(tempPath); } catch { }

                // 셸에 association 변경 알림 — 안 하면 explorer 재시작 전까지 옛 핸들러 유지
                NotifyShellAssocChanged();

                // 검증
                return IsDefault();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // 사용자가 UAC를 취소한 경우
                return false;
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[DefaultFM] SetAsDefault failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 기본 파일 관리자 해제.
        /// </summary>
        public async Task<bool> UnsetDefaultAsync()
        {
            try
            {
                var regContent = GenerateRestoreReg();
                var tempPath = Path.Combine(Path.GetTempPath(), "SpanRestoreDefault.reg");
                await File.WriteAllTextAsync(tempPath, regContent);

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "regedit.exe",
                    Arguments = $"/s \"{tempPath}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                });

                if (process != null)
                    await process.WaitForExitAsync();

                try { File.Delete(tempPath); } catch { }

                // 셸에 association 변경 알림
                NotifyShellAssocChanged();

                return !IsDefault();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[DefaultFM] UnsetDefault failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 현재 LumiFiles가 기본 파일 관리자인지 확인.
        /// HKCU에서 Folder\shell\open\command를 읽어 LumiFiles.exe 포함 여부 확인.
        /// </summary>
        public bool IsDefault()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(FolderOpenCommandKey);
                var command = key?.GetValue("")?.ToString();
                return !string.IsNullOrEmpty(command)
                    && command.Contains("LumiFiles.exe", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Issue #55: 삭제 후 남은 등록으로 모든 폴더가 열리지 않는 문제
        //
        //  MSIX 앱은 삭제 시 사용자 코드를 실행할 수 없어(uninstall hook 미지원)
        //  HKCU\...\shell\open\command 가 사라진 실행 파일을 계속 가리킨다.
        //  → 탐색기가 폴더를 못 열고, 사용자는 원인을 알기 어렵다.
        //
        //  삭제 자체를 가로챌 방법은 없으므로 아래 3중으로 대응한다.
        //   (1) 감지·복구 : 앱 실행 시 끊어진 등록을 찾아 정리 (본 영역)
        //   (2) 사전 경고 : 설정에서 켤 때 삭제 전 해제 안내 (SettingsModeView)
        //   (3) 복구 파일 : 켤 때 패키지 밖에 restore .reg 를 남겨 삭제 후에도 사용 가능
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 등록된 open 명령에서 실행 파일 경로만 추출. 없으면 null.
        /// 값 형식: "C:\path\App.exe" "%1"
        /// </summary>
        internal static string? ExtractExePath(string? command)
        {
            if (string.IsNullOrWhiteSpace(command)) return null;
            command = command.Trim();
            if (command.StartsWith('"'))
            {
                int end = command.IndexOf('"', 1);
                if (end > 1) return command.Substring(1, end - 1);
                return null;
            }
            int space = command.IndexOf(' ');
            return space > 0 ? command.Substring(0, space) : command;
        }

        /// <summary>
        /// 끊어진(대상 실행 파일이 존재하지 않는) 기본 파일 관리자 등록을 찾는다.
        /// 발견 시 해당 경로를 반환, 정상이면 null.
        /// </summary>
        public string? DetectStaleRegistration()
        {
            foreach (var keyPath in OpenCommandKeys)
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(keyPath);
                    var exe = ExtractExePath(key?.GetValue("")?.ToString());
                    if (string.IsNullOrEmpty(exe)) continue;
                    // 환경 변수를 쓴 등록도 있을 수 있으므로 확장 후 판정
                    var expanded = Environment.ExpandEnvironmentVariables(exe);
                    if (!File.Exists(expanded))
                    {
                        Helpers.DebugLogger.Log($"[DefaultFM] Stale registration: {keyPath} -> '{expanded}' (not found)");
                        return expanded;
                    }
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[DefaultFM] DetectStale({keyPath}) failed: {ex.Message}");
                }
            }
            return null;
        }

        /// <summary>
        /// 끊어진 등록이 <b>우리(또는 Span 시절) 실행 파일</b>을 가리킬 때만 조용히 정리한다.
        /// 다른 파일 관리자의 등록은 사용자가 일시적으로 앱을 지운 것일 수 있어 건드리지 않는다.
        ///
        /// HKCU 하위이므로 UAC 없이 삭제 가능 — SetAsDefault 가 regedit+runas 를 쓰는 것과 달리
        /// 여기서는 레지스트리 API 로 직접 지운다(승격 프롬프트 없이 시작 시 처리하기 위함).
        /// </summary>
        /// <returns>정리한 경우 true</returns>
        public bool RepairStaleRegistrationIfOwn()
        {
            var stale = DetectStaleRegistration();
            if (stale == null) return false;

            var fileName = Path.GetFileName(stale);
            bool isOurs = Array.Exists(OwnExeNames,
                n => string.Equals(n, fileName, StringComparison.OrdinalIgnoreCase));
            if (!isOurs)
            {
                Helpers.DebugLogger.Log($"[DefaultFM] Stale registration '{stale}' is not ours — leaving untouched");
                return false;
            }

            bool removed = false;
            foreach (var keyPath in OpenCommandKeys)
            {
                try
                {
                    // 대상이 존재하는 키는 건드리지 않는다 (부분 손상 대비)
                    using (var check = Registry.CurrentUser.OpenSubKey(keyPath))
                    {
                        var exe = ExtractExePath(check?.GetValue("")?.ToString());
                        if (string.IsNullOrEmpty(exe)) continue;
                        if (File.Exists(Environment.ExpandEnvironmentVariables(exe))) continue;
                    }
                    Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
                    removed = true;
                    Helpers.DebugLogger.Log($"[DefaultFM] Removed stale key: {keyPath}");
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[DefaultFM] Repair({keyPath}) failed: {ex.Message}");
                }
            }

            if (removed) NotifyShellAssocChanged();
            return removed;
        }

        /// <summary>
        /// 삭제 후에도 남는 위치(패키지 밖 사용자 문서 폴더)에 복구용 .reg 를 저장한다.
        /// MSIX 의 LocalAppData 는 삭제 시 함께 정리되므로 그곳에 두면 의미가 없다.
        /// </summary>
        /// <returns>저장된 파일 경로. 실패 시 null.</returns>
        public string? WriteRecoveryRegFile()
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LumiFinder");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "restore-default-file-manager.reg");
                File.WriteAllText(path, GenerateRestoreReg());
                Helpers.DebugLogger.Log($"[DefaultFM] Recovery .reg written: {path}");
                return path;
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[DefaultFM] WriteRecoveryRegFile failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 등록용 .reg 파일을 사용자 지정 경로에 내보내기 (fallback용).
        /// </summary>
        public async Task ExportSetDefaultRegAsync(string filePath)
        {
            await File.WriteAllTextAsync(filePath, GenerateSetDefaultReg());
        }

        /// <summary>
        /// 복원용 .reg 파일을 사용자 지정 경로에 내보내기.
        /// </summary>
        public async Task ExportRestoreRegAsync(string filePath)
        {
            await File.WriteAllTextAsync(filePath, GenerateRestoreReg());
        }

        /// <summary>등록용 .reg 내용 생성</summary>
        private string GenerateSetDefaultReg()
        {
            // %LOCALAPPDATA%\Microsoft\WindowsApps\LumiFiles.exe 전체 경로 사용 (안정성)
            var exePath = AliasPath.Replace("\\", "\\\\");
            //
            // ⚠ DelegateExecute는 반드시 "" (빈 문자열). "-" (삭제) 아님.
            //
            //   HKLM\Folder\shell\open\command 에 DelegateExecute={11dbb47c-...} (Windows.FileExplorer COM)이 기본 설정됨.
            //   HKCR(merged) = HKCU overlay on HKLM. HKCU에 키가 없으면 HKLM값이 상속되어
            //   explorer가 CoCreateInstance로 COM 핸들러를 우선 호출 → 우리 명령줄 우회됨.
            //   → HKCU에 빈 문자열로 명시 설정하여 CLSID 파싱 실패 유도 → (기본값) 명령줄 fallback.
            //
            // Folder(meta) + Directory(파일시스템 폴더) + Drive(루트 볼륨) 모두 커버.
            return $"""
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Classes\Folder\shell\open\command]
@="\"{exePath}\" \"%1\""
"DelegateExecute"=""

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\open\command]
@="\"{exePath}\" \"%1\""
"DelegateExecute"=""

[HKEY_CURRENT_USER\Software\Classes\Drive\shell\open\command]
@="\"{exePath}\" \"%1\""
"DelegateExecute"=""
""";
        }

        /// <summary>복원용 .reg 내용 생성</summary>
        private string GenerateRestoreReg()
        {
            return """
Windows Registry Editor Version 5.00

[-HKEY_CURRENT_USER\Software\Classes\Folder\shell\open\command]
[-HKEY_CURRENT_USER\Software\Classes\Directory\shell\open\command]
[-HKEY_CURRENT_USER\Software\Classes\Drive\shell\open\command]
""";
        }
    }
}
