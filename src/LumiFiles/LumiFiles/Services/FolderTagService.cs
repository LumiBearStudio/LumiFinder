using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
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
    ///  - 커스텀 폴더 아이콘도 이미 desktop.ini 를 쓰므로 '폴더 외형' 정보의 위치가 일관된다.
    ///
    /// 형식 (기존 내용은 보존하고 우리 섹션만 갱신):
    /// <code>
    /// [LumiFiles]
    /// TagColor=Blue
    /// </code>
    ///
    /// <b>성능</b>: 목록 표시 중에 폴더마다 파일을 여는 것은 (특히 네트워크 공유에서) 위험하다.
    /// 그래서 조회는 항상 캐시 우선이고, 캐시에 없으면 <see cref="TryGetCached"/> 는 즉시
    /// None 을 반환한다. 실제 읽기는 호출측이 백그라운드에서 <see cref="Load"/> 로 수행한다.
    /// </summary>
    public sealed class FolderTagService
    {
        private const string DesktopIni = "desktop.ini";
        private const string SectionName = "LumiFiles";
        private const string KeyName = "TagColor";

        /// <summary>경로 → (태그, desktop.ini 최종수정시각). 시각이 바뀌면 다시 읽는다.</summary>
        private readonly ConcurrentDictionary<string, (FolderTagColor Tag, DateTime Stamp)> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>태그가 바뀌었을 때 UI 갱신용. 인자는 폴더 경로.</summary>
        public event Action<string>? TagChanged;

        /// <summary>
        /// 캐시된 태그를 즉시 반환. 아직 읽지 않았으면 None.
        /// 목록 렌더링처럼 지연이 허용되지 않는 경로에서 사용한다.
        /// </summary>
        public FolderTagColor TryGetCached(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return FolderTagColor.None;
            return _cache.TryGetValue(folderPath, out var e) ? e.Tag : FolderTagColor.None;
        }

        /// <summary>
        /// desktop.ini 를 실제로 읽어 태그를 확인하고 캐시에 넣는다(디스크 접근 발생).
        /// 백그라운드에서 호출할 것. 값이 바뀌었으면 <see cref="TagChanged"/> 를 발생시킨다.
        /// </summary>
        public FolderTagColor Load(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return FolderTagColor.None;

            try
            {
                var iniPath = Path.Combine(folderPath, DesktopIni);

                // 대부분의 폴더에는 desktop.ini 가 없다 → 존재 확인 한 번으로 끝낸다.
                if (!File.Exists(iniPath))
                {
                    Update(folderPath, FolderTagColor.None, DateTime.MinValue);
                    return FolderTagColor.None;
                }

                var stamp = File.GetLastWriteTimeUtc(iniPath);
                if (_cache.TryGetValue(folderPath, out var cached) && cached.Stamp == stamp)
                    return cached.Tag;

                var tag = ParseTag(ReadAllTextSafe(iniPath));
                Update(folderPath, tag, stamp);
                return tag;
            }
            catch (Exception ex)
            {
                // 권한 없음·잠김 등은 '태그 없음'으로 처리 — 목록 표시를 막지 않는다.
                Helpers.DebugLogger.Log($"[FolderTag] Load failed '{folderPath}': {ex.GetType().Name}");
                return FolderTagColor.None;
            }
        }

        /// <summary>
        /// 폴더에 태그를 지정한다. <see cref="FolderTagColor.None"/> 이면 해제(키 제거).
        /// desktop.ini 의 다른 내용(아이콘 지정 등)은 보존한다.
        /// </summary>
        /// <returns>성공 여부. 읽기 전용 미디어·권한 부족 시 false.</returns>
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

                var iniPath = Path.Combine(folderPath, DesktopIni);
                string original = File.Exists(iniPath) ? ReadAllTextSafe(iniPath) : string.Empty;
                string updated = ApplyTag(original, color);

                // desktop.ini 는 보통 숨김+시스템 속성이라 그대로 쓰면 UnauthorizedAccess 가 난다.
                // 속성을 잠시 벗기고 쓴 뒤 되돌린다.
                FileAttributes? saved = null;
                if (File.Exists(iniPath))
                {
                    saved = File.GetAttributes(iniPath);
                    File.SetAttributes(iniPath, FileAttributes.Normal);
                }

                if (string.IsNullOrWhiteSpace(updated))
                {
                    // 우리 태그가 유일한 내용이었고 해제된 경우 → 파일 자체를 남기지 않는다.
                    if (File.Exists(iniPath)) File.Delete(iniPath);
                }
                else
                {
                    // Windows 관례에 맞춰 UTF-16LE(BOM)로 기록 — 유니코드 폴더명 주석 등에 안전.
                    File.WriteAllText(iniPath, updated, new UnicodeEncoding(false, true));
                    File.SetAttributes(iniPath,
                        saved ?? (FileAttributes.Hidden | FileAttributes.System));
                }

                // Windows 는 폴더에 ReadOnly 또는 System 속성이 있을 때만 desktop.ini 를 해석한다.
                // 우리 목록 코드도 이 속성을 '커스터마이즈된 폴더' 사전 게이트로 재사용하므로
                // (FolderItem.MaybeHasCustomIcon) 태그를 남길 때 함께 표시해 둔다.
                // → 대부분의 폴더는 desktop.ini 존재 확인조차 하지 않는다 = 목록 성능 보호.
                //
                // ⚠ 반드시 ReadOnly 여야 한다. System 을 쓰면 태그를 단 폴더가 목록에서 사라진다:
                //   FolderViewModel 의 열거가 숨김 표시 OFF(기본값)일 때 Hidden|System 을 건너뛴다.
                //   ReadOnly 는 그 필터에 걸리지 않고, 탐색기에서 '폴더 사용자 지정' 을 했을 때
                //   Windows 가 실제로 붙이는 속성도 ReadOnly 다(폴더의 ReadOnly 는 내용을 읽기
                //   전용으로 만들지 않는다 — '커스터마이즈됨' 표시일 뿐).
                if (color != FolderTagColor.None)
                {
                    try
                    {
                        var dirAttrs = File.GetAttributes(folderPath);
                        if ((dirAttrs & (FileAttributes.ReadOnly | FileAttributes.System)) == 0)
                            File.SetAttributes(folderPath, dirAttrs | FileAttributes.ReadOnly);
                    }
                    catch (Exception ex)
                    {
                        // 속성 지정 실패해도 태그 자체는 유효 — 우리는 파일을 직접 읽는다.
                        Helpers.DebugLogger.Log($"[FolderTag] could not mark folder as customized: {ex.GetType().Name}");
                    }
                }

                var stamp = File.Exists(iniPath) ? File.GetLastWriteTimeUtc(iniPath) : DateTime.MinValue;
                Update(folderPath, color, stamp);
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

        private void Update(string folderPath, FolderTagColor tag, DateTime stamp)
        {
            bool changed = !_cache.TryGetValue(folderPath, out var prev) || prev.Tag != tag;
            _cache[folderPath] = (tag, stamp);
            if (changed)
            {
                try { TagChanged?.Invoke(folderPath); } catch { }
            }
        }

        // ── INI 처리 (순수 문자열 — 단위 테스트 대상) ─────────────────

        /// <summary>desktop.ini 본문에서 우리 태그를 읽는다. 없거나 알 수 없는 값이면 None.</summary>
        internal static FolderTagColor ParseTag(string? iniText)
        {
            if (string.IsNullOrEmpty(iniText)) return FolderTagColor.None;

            bool inSection = false;
            foreach (var raw in iniText.Split('\n'))
            {
                var line = raw.Trim().TrimEnd('\r');
                if (line.Length == 0 || line[0] == ';') continue;

                if (line[0] == '[')
                {
                    inSection = line.Equals($"[{SectionName}]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!inSection) continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                if (!line[..eq].Trim().Equals(KeyName, StringComparison.OrdinalIgnoreCase)) continue;

                var value = line[(eq + 1)..].Trim();
                return Enum.TryParse<FolderTagColor>(value, ignoreCase: true, out var parsed)
                       && Enum.IsDefined(typeof(FolderTagColor), parsed)
                    ? parsed
                    : FolderTagColor.None;
            }
            return FolderTagColor.None;
        }

        /// <summary>
        /// 기존 desktop.ini 내용을 보존하면서 태그만 설정/해제한 새 본문을 만든다.
        /// None 이면 우리 키(및 비게 된 우리 섹션)를 제거한다.
        /// </summary>
        internal static string ApplyTag(string? iniText, FolderTagColor color)
        {
            var lines = (iniText ?? string.Empty)
                .Replace("\r\n", "\n").Split('\n').ToList();

            // 우리 섹션 범위 찾기
            int sectionStart = -1, sectionEnd = lines.Count;
            for (int i = 0; i < lines.Count; i++)
            {
                var t = lines[i].Trim();
                if (t.Length == 0 || t[0] != '[') continue;
                if (sectionStart < 0 && t.Equals($"[{SectionName}]", StringComparison.OrdinalIgnoreCase))
                {
                    sectionStart = i;
                }
                else if (sectionStart >= 0 && i > sectionStart)
                {
                    sectionEnd = i;
                    break;
                }
            }

            if (sectionStart >= 0)
            {
                // 섹션 안의 기존 TagColor 줄 제거
                for (int i = sectionEnd - 1; i > sectionStart; i--)
                {
                    var t = lines[i].Trim();
                    int eq = t.IndexOf('=');
                    if (eq > 0 && t[..eq].Trim().Equals(KeyName, StringComparison.OrdinalIgnoreCase))
                        lines.RemoveAt(i);
                }

                if (color == FolderTagColor.None)
                {
                    // 섹션에 남은 내용이 없으면 섹션 헤더도 제거
                    bool empty = true;
                    for (int i = sectionStart + 1; i < lines.Count && (i < sectionEnd || sectionEnd == lines.Count); i++)
                    {
                        var t = lines[i].Trim();
                        if (t.Length == 0) continue;
                        if (t[0] == '[') break;
                        empty = false;
                        break;
                    }
                    if (empty) lines.RemoveAt(sectionStart);
                }
                else
                {
                    lines.Insert(sectionStart + 1, $"{KeyName}={color}");
                }
            }
            else if (color != FolderTagColor.None)
            {
                if (lines.Count > 0 && lines[^1].Trim().Length != 0) lines.Add(string.Empty);
                lines.Add($"[{SectionName}]");
                lines.Add($"{KeyName}={color}");
            }

            var result = string.Join("\r\n", lines).Trim('\r', '\n');
            return result.Length == 0 ? string.Empty : result + "\r\n";
        }

        /// <summary>BOM 유무·인코딩(UTF-16/UTF-8/ANSI)에 관계없이 텍스트를 읽는다.</summary>
        private static string ReadAllTextSafe(string path)
        {
            // detectEncodingFromByteOrderMarks: UTF-16LE/BE·UTF-8 BOM 자동 인식,
            // BOM 이 없으면 UTF-8(ASCII 호환)로 읽는다 — 우리 키는 ASCII 라 안전.
            using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
    }
}
