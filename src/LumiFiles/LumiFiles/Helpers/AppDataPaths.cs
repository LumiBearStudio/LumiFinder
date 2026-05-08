// =============================================================================
//  Helpers/AppDataPaths.cs (S-3.40)
//
//  LocalAppData 폴더 경로 중앙화 + 'Lumi Files' → 'LumiFinder' 1회 자동 마이그레이션.
//
//  배경: 리브랜딩 전(Lumi Files)에 사용자가 만든 워크스페이스 / 액션 로그 / 디버그
//  로그 / 크래시 덤프 / 설정 파일이 %LocalAppData%\Lumi Files\ 에 쌓여있음. 단순히
//  새 코드에서 폴더 이름을 바꾸면 기존 사용자가 데이터 잃음.
//
//  전략: GetAppDataFolder() 첫 호출 시 새 폴더(LumiFinder)가 없으면 옛 폴더
//  (Lumi Files)에서 모든 파일/하위폴더를 옮기고 옛 폴더는 두 개 다 존재하면
//  옛 것은 그대로 둠 (수동 정리 가능). Move-after-CreateNew 패턴이라 idempotent.
// =============================================================================

using System;
using System.IO;

namespace LumiFiles.Helpers
{
    public static class AppDataPaths
    {
        private const string OldFolderName = "Lumi Files";   // 리브랜딩 전 폴더명
        private const string NewFolderName = "LumiFinder";   // 신규 폴더명

        private static readonly object _migrationLock = new();
        private static bool _migrated;

        /// <summary>
        /// LumiFinder 의 LocalAppData 폴더 경로. 첫 호출 시 옛 'Lumi Files' 폴더가 있고
        /// 새 'LumiFinder' 폴더가 없으면 1회 마이그레이션. 폴더는 항상 존재 보장.
        /// </summary>
        public static string GetAppDataFolder()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var newPath = Path.Combine(localAppData, NewFolderName);
            var oldPath = Path.Combine(localAppData, OldFolderName);

            // Fast path: 이미 마이그레이션 끝남 + 새 폴더 존재
            if (_migrated && Directory.Exists(newPath)) return newPath;

            lock (_migrationLock)
            {
                if (_migrated && Directory.Exists(newPath)) return newPath;

                try
                {
                    bool newExists = Directory.Exists(newPath);
                    bool oldExists = Directory.Exists(oldPath);

                    if (!newExists && oldExists)
                    {
                        // 옛 폴더만 있음 → 통째로 이름 변경 (가장 빠르고 안전)
                        try
                        {
                            Directory.Move(oldPath, newPath);
                            DebugLogger.Log($"[AppDataPaths] Migrated '{oldPath}' -> '{newPath}'");
                        }
                        catch (IOException ex)
                        {
                            // Move 실패 시(다른 프로세스 잠금 등) 새 폴더 만들고 파일 단위 복사 fallback
                            DebugLogger.Log($"[AppDataPaths] Move failed: {ex.Message} — falling back to file copy");
                            Directory.CreateDirectory(newPath);
                            CopyDirectory(oldPath, newPath);
                        }
                    }
                    else if (!newExists)
                    {
                        Directory.CreateDirectory(newPath);
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Log($"[AppDataPaths] Migration error: {ex.Message}");
                    // 에러 시에도 새 폴더는 보장
                    try { Directory.CreateDirectory(newPath); } catch { }
                }

                _migrated = true;
                return newPath;
            }
        }

        /// <summary>
        /// 옛 폴더에서 새 폴더로 파일/디렉토리 재귀 복사 (Move 실패 fallback).
        /// 기존 새 폴더 파일은 덮어쓰지 않음 (충돌 시 옛 데이터 우선 보존).
        /// </summary>
        private static void CopyDirectory(string src, string dst)
        {
            foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dir.Replace(src, dst));
            }
            foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                var target = file.Replace(src, dst);
                if (!File.Exists(target))
                {
                    try { File.Copy(file, target, overwrite: false); } catch { }
                }
            }
        }
    }
}
