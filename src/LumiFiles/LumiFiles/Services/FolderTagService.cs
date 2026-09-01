using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using LumiFiles.Models;

namespace LumiFiles.Services
{
    /// <summary>
    /// Span #58: 폴더 컬러 태그를 폴더 안의 <c>desktop.ini</c> 에 저장/조회한다.
    ///
    /// 저장 위치를 desktop.ini 로 잡은 이유(업스트림 논의 결론):
    ///  - 폴더 <b>안</b>에 있으므로 탐색기·스크립트 등 LumiFiles 밖에서 폴더를 옮기거나
    ///    이름을 바꿔도 태그가 함께 따라간다 → 경로 기반 DB 의 '고아 태그' 문제가 없다.
    ///  - 평범한 파일이라 NTFS 대체 데이터 스트림과 달리 USB/exFAT/네트워크 공유에서도 살아남는다.
    ///
    /// <b>폴더 속성을 건드리지 않는다</b> — 이 결정이 핵심이다.
    /// Windows 가 desktop.ini 를 <i>셸에서</i> 해석하려면 폴더에 System 또는 ReadOnly 속성이
    /// 필요하지만 둘 다 실제 피해가 있다(둘 다 실측 확인):
    ///   - System  : 숨김 표시 OFF(기본값)에서 그 폴더가 목록에서 사라진다
    ///               (FolderViewModel 열거의 Hidden|System 필터).
    ///   - ReadOnly: <c>Directory.Delete(recursive)</c> 가 IOException 으로 실패해
    ///               태그를 단 폴더를 삭제할 수 없게 된다.
    /// 태그 색은 LumiFiles 가 INI 를 직접 읽어 그리므로 셸 해석이 필요 없다.
    /// 대가로 탐색기에는 태그가 보이지 않는다(의도된 트레이드오프).
    ///
    /// 형식 (기존 내용은 보존하고 우리 섹션만 갱신):
    /// <code>
    /// [LumiFiles]
    /// Tag=Blue
    /// </code>
    ///
    /// <b>성능</b>: 목록 열거 루프에는 I/O 를 추가하지 않는다. 조회는 캐시 우선이고,
    /// 실제 읽기는 화면에 보이는 항목에 대해서만 일어난다
    /// (<c>FolderViewModel.InjectTagIfNeeded</c> — 클라우드/Git 뱃지와 동일한 지연 주입).
    /// </summary>
    public sealed class FolderTagService
    {
        private const string IniFileName = "desktop.ini";
        private const string SectionName = "LumiFiles";
        private const string KeyName = "Tag";

        /// <summary>경로 → 태그. 쓰기 시 갱신, 폴더 이탈 시 Invalidate.</summary>
        private readonly ConcurrentDictionary<string, FolderTagColor> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>캐시된 태그를 즉시 반환. 아직 읽지 않았으면 null.</summary>
        public FolderTagColor? TryGetCached(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return null;
            return _cache.TryGetValue(folderPath, out var v) ? v : null;
        }

        /// <summary>
        /// 태그를 읽는다(캐시 우선, 없으면 desktop.ini 1회 조회 후 캐시).
        /// 지연 주입 경로에서만 호출할 것 — 열거 루프에서 부르면 폴더 수만큼 I/O 가 생긴다.
        /// </summary>
        public FolderTagColor Get(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return FolderTagColor.None;
            if (_cache.TryGetValue(folderPath, out var cached)) return cached;

            var tag = ReadFromDisk(folderPath);
            _cache[folderPath] = tag;
            return tag;
        }

        private static FolderTagColor ReadFromDisk(string folderPath)
        {
            try
            {
                if (!IsTaggable(folderPath)) return FolderTagColor.None;

                var iniPath = Path.Combine(folderPath, IniFileName);
                if (!File.Exists(iniPath)) return FolderTagColor.None;

                var sb = new StringBuilder(64);
                Helpers.NativeMethods.GetPrivateProfileStringW(
                    SectionName, KeyName, string.Empty, sb, (uint)sb.Capacity, iniPath);

                var value = sb.ToString().Trim();
                if (value.Length == 0) return FolderTagColor.None;

                return Enum.TryParse<FolderTagColor>(value, ignoreCase: true, out var parsed)
                       && Enum.IsDefined(typeof(FolderTagColor), parsed)
                    ? parsed
                    : FolderTagColor.None;
            }
            catch (Exception ex)
            {
                // 권한 없음·잠김 등은 '태그 없음' — 목록 표시를 막지 않는다.
                Helpers.DebugLogger.Log($"[FolderTag] read failed '{folderPath}': {ex.GetType().Name}");
                return FolderTagColor.None;
            }
        }

        /// <summary>
        /// 폴더에 태그를 지정한다. <see cref="FolderTagColor.None"/> 이면 해제.
        /// desktop.ini 의 다른 내용(커스텀 아이콘 설정 등)은 보존한다.
        /// </summary>
        /// <returns>성공 여부. 읽기 전용 미디어·권한 부족·비대상 경로면 false.</returns>
        public bool SetTag(string folderPath, FolderTagColor color)
        {
            if (string.IsNullOrEmpty(folderPath)) return false;

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Helpers.DebugLogger.Log($"[FolderTag] SetTag: folder missing '{folderPath}'");
                    return false;
                }
                if (!IsTaggable(folderPath))
                {
                    Helpers.DebugLogger.Log($"[FolderTag] SetTag: unsupported location '{folderPath}'");
                    return false;
                }

                var iniPath = Path.Combine(folderPath, IniFileName);

                // WritePrivateProfileStringW 는 UTF-8 BOM 파일의 섹션을 찾지 못해 중복 생성한다
                // → 기존 [.ShellClassInfo] 커스텀 아이콘 설정이 깨진다. 미리 정규화.
                NormalizeUtf8BomIfNeeded(iniPath);

                bool ok = color == FolderTagColor.None
                    // 키가 아니라 섹션을 통째로 지운다 (빈 [LumiFiles] 잔여 방지)
                    ? Helpers.NativeMethods.WritePrivateProfileStringW(SectionName, null, null, iniPath)
                    : Helpers.NativeMethods.WritePrivateProfileStringW(SectionName, KeyName, color.ToString(), iniPath);

                if (!ok)
                {
                    Helpers.DebugLogger.Log($"[FolderTag] SetTag write failed '{folderPath}' (Win32)");
                    return false;
                }

                // 새로 만든 desktop.ini 는 관례대로 숨김+시스템으로 (탐색기에 노출되지 않도록).
                // 폴더 속성은 건드리지 않는다 — 클래스 주석 참조.
                TryHideIniFile(iniPath);

                // 태그 해제로 파일에 남은 내용이 없으면 파일 자체를 정리
                if (color == FolderTagColor.None) DeleteIniIfEmpty(iniPath);

                _cache[folderPath] = color;
                Helpers.DebugLogger.Log($"[FolderTag] '{folderPath}' -> {color}");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Helpers.DebugLogger.Log($"[FolderTag] SetTag denied (read-only/protected): '{folderPath}'");
                return false;
            }
            catch (IOException ex)
            {
                Helpers.DebugLogger.Log($"[FolderTag] SetTag IO error '{folderPath}': {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[FolderTag] SetTag failed '{folderPath}': {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>캐시에서 제거 (폴더 삭제/이동 후).</summary>
        public void Invalidate(string folderPath)
        {
            if (!string.IsNullOrEmpty(folderPath)) _cache.TryRemove(folderPath, out _);
        }

        // ── 내부 ────────────────────────────────────────────────────────

        /// <summary>
        /// 태그를 쓸 수 있는 위치인지. 로컬 경로만 허용한다 —
        /// 압축 내부(archive://)·원격(FTP/SFTP)·UNC 는 desktop.ini 를 쓸 수 없거나 의미가 없다.
        /// </summary>
        internal static bool IsTaggable(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return false;
            if (Helpers.ArchivePathHelper.IsArchivePath(folderPath)) return false;
            if (FileSystemRouter.IsRemotePath(folderPath)) return false;
            if (folderPath.StartsWith(@"\\", StringComparison.Ordinal)) return false;  // UNC
            if (!Path.IsPathRooted(folderPath)) return false;
            return true;
        }

        /// <summary>
        /// UTF-8 BOM 으로 저장된 desktop.ini 는 Win32 INI API 가 섹션을 못 찾아 중복 생성한다.
        /// BOM 이 있으면 UTF-16LE 로 다시 써서 정규화한다(내용은 그대로).
        /// </summary>
        private static void NormalizeUtf8BomIfNeeded(string iniPath)
        {
            try
            {
                if (!File.Exists(iniPath)) return;

                var head = new byte[3];
                using (var fs = new FileStream(iniPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (fs.Read(head, 0, 3) < 3) return;
                }
                if (!(head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF)) return;

                var text = File.ReadAllText(iniPath, Encoding.UTF8);

                var saved = File.GetAttributes(iniPath);
                File.SetAttributes(iniPath, FileAttributes.Normal);
                File.WriteAllText(iniPath, text, new UnicodeEncoding(false, true));
                File.SetAttributes(iniPath, saved);

                Helpers.DebugLogger.Log($"[FolderTag] normalized UTF-8 BOM ini: {iniPath}");
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[FolderTag] BOM normalize skipped: {ex.GetType().Name}");
            }
        }

        private static void TryHideIniFile(string iniPath)
        {
            try
            {
                if (!File.Exists(iniPath)) return;
                var attrs = File.GetAttributes(iniPath);
                const FileAttributes want = FileAttributes.Hidden | FileAttributes.System;
                if ((attrs & want) != want)
                    File.SetAttributes(iniPath, attrs | want);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[FolderTag] hide ini skipped: {ex.GetType().Name}");
            }
        }

        /// <summary>우리 섹션을 지운 뒤 실질 내용이 없으면 desktop.ini 를 제거한다.</summary>
        private static void DeleteIniIfEmpty(string iniPath)
        {
            try
            {
                if (!File.Exists(iniPath)) return;

                foreach (var raw in File.ReadAllLines(iniPath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line[0] == ';') continue;
                    return;  // 사용자의 다른 설정이 남아 있다 → 파일 유지
                }

                File.SetAttributes(iniPath, FileAttributes.Normal);
                File.Delete(iniPath);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[FolderTag] ini cleanup skipped: {ex.GetType().Name}");
            }
        }
    }
}
