using System.IO.Compression;
using LumiFiles.Helpers;

namespace LumiFiles.Services.Archive;

/// <summary>
/// Materializes <c>archive://</c> entries as real files in a temp folder (Issue #64).
///
/// Copying a file out of an archive, or dragging one to another app, both fail for the
/// same reason: <c>archive://C:\x.zip/inner.txt</c> is not a path any program can open.
/// Windows hands the receiving app a file path, so there has to be a real file.
///
/// Both call sites go through <c>SetDataProvider</c>, which runs only when the receiving
/// app actually asks for the data — so a drag that is started and abandoned costs nothing.
/// Nothing here should be called eagerly.
///
/// ZIP only. Archive browsing goes through ArchiveReaderService, which reads ZIP, so
/// there is no way to select an entry inside a .7z in the first place.
/// </summary>
internal static class ArchiveEntryStaging
{
    /// <summary>Staged folders older than this are removed on the next call.</summary>
    private static readonly TimeSpan StagingLifetime = TimeSpan.FromHours(1);

    private const string StagingPrefix = "LumiArchive_";

    internal static bool ContainsArchiveEntry(IEnumerable<string>? paths)
        => paths is not null && paths.Any(ArchivePathHelper.IsArchivePath);

    /// <summary>
    /// Returns the given paths with every <c>archive://</c> entry replaced by a real file
    /// extracted to a temp folder. Non-archive paths pass through untouched, and an entry
    /// that cannot be extracted is dropped rather than failing the whole batch — a drag of
    /// five files should not be lost because one of them is unreadable.
    /// </summary>
    internal static async Task<List<string>> MaterializeAsync(
        IReadOnlyList<string> paths,
        CancellationToken ct = default)
    {
        var result = new List<string>(paths.Count);
        var archiveEntries = new List<string>();

        foreach (var p in paths)
        {
            if (ArchivePathHelper.IsArchivePath(p)) archiveEntries.Add(p);
            else result.Add(p);
        }

        if (archiveEntries.Count == 0) return result;

        CleanupStaleStagingFolders();

        string stagingRoot;
        try
        {
            stagingRoot = Path.Combine(Path.GetTempPath(), StagingPrefix + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(stagingRoot);
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"[ArchiveStaging] cannot create staging folder: {ex.GetType().Name}: {ex.Message}");
            return result;
        }

        var reserver = new StagingNameReserver(stagingRoot);

        // Group by archive so each file is opened once even when several entries are dragged.
        foreach (var group in archiveEntries.GroupBy(
                     p => ArchivePathHelper.Parse(p).ArchiveFilePath,
                     StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();

            var internalPaths = group
                .Select(p => ArchivePathHelper.Parse(p).InternalPath)
                .ToList();

            try
            {
                var staged = await Task.Run(
                    () => StageFromArchive(group.Key, internalPaths, stagingRoot, reserver, ct), ct);
                result.AddRange(staged);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                DebugLogger.Log($"[ArchiveStaging] failed for {group.Key}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return result;
    }

    // --- preview staging -----------------------------------------------------
    // Preview runs on every selection change, so this path has tighter rules than the
    // drag/paste one above: it reuses a single folder keyed by content identity, so
    // arrowing back and forth re-extracts nothing, and it prunes itself.

    private const string PreviewFolderName = StagingPrefix + "preview";

    /// <summary>Most recently staged preview files to keep before pruning the oldest.</summary>
    private const int MaxPreviewFiles = 48;

    /// <summary>
    /// Stages one archive entry for preview and returns its real path, or null when it
    /// cannot or should not be staged.
    ///
    /// The caller must pass the entry's known uncompressed size so an oversized entry is
    /// rejected without opening the archive at all — selecting a 2 GB file inside a ZIP
    /// must not extract it just to render a preview.
    /// </summary>
    internal static async Task<string?> StageForPreviewAsync(
        string archiveEntryPath,
        long entrySize,
        long maxBytes,
        CancellationToken ct = default)
    {
        if (!ArchivePathHelper.IsArchivePath(archiveEntryPath)) return null;
        if (entrySize > maxBytes) return null;

        try
        {
            var (archiveFile, internalPath) = ArchivePathHelper.Parse(archiveEntryPath);
            if (!File.Exists(archiveFile)) return null;

            var folder = Path.Combine(Path.GetTempPath(), PreviewFolderName);
            Directory.CreateDirectory(folder);

            // Identity covers the archive's own timestamp, so re-creating the archive with
            // different content does not serve a stale preview from the cache.
            var stamp = File.GetLastWriteTimeUtc(archiveFile).Ticks;
            var key = $"{archiveFile}|{internalPath}|{entrySize}|{stamp}".ToLowerInvariant();
            var name = $"{StableHash(key):x8}_{SanitizeName(Path.GetFileName(internalPath.Replace('\\', '/').TrimEnd('/')))}";
            var target = Path.Combine(folder, name);

            // Already staged and the right size — nothing to do.
            if (File.Exists(target) && new FileInfo(target).Length == entrySize)
                return target;

            var staged = await Task.Run(() =>
            {
                using var stream = new FileStream(archiveFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false,
                    entryNameEncoding: ZipEntryNameEncoding.Instance);

                var normalized = internalPath.Replace('\\', '/').TrimEnd('/');
                var entry = archive.Entries.FirstOrDefault(e =>
                    !string.IsNullOrEmpty(e.Name) &&
                    string.Equals(e.FullName.Replace('\\', '/'), normalized, StringComparison.OrdinalIgnoreCase));

                if (entry is null) return null;

                ExtractEntry(entry, target, ct);
                return target;
            }, ct);

            PrunePreviewFolder(folder);
            return staged;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            DebugLogger.Log($"[ArchiveStaging] preview staging failed for {archiveEntryPath}: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>Keeps the preview folder bounded — browsing a large archive would otherwise fill temp.</summary>
    private static void PrunePreviewFolder(string folder)
    {
        try
        {
            var files = new DirectoryInfo(folder).GetFiles();
            if (files.Length <= MaxPreviewFiles) return;

            foreach (var stale in files.OrderByDescending(f => f.LastWriteTimeUtc).Skip(MaxPreviewFiles))
            {
                try { stale.Delete(); } catch { /* in use by a preview still rendering */ }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"[ArchiveStaging] preview prune skipped: {ex.GetType().Name}");
        }
    }

    /// <summary>
    /// Deterministic across runs, unlike string.GetHashCode which is randomized per process —
    /// a randomized hash would miss the cache on every restart.
    /// </summary>
    private static uint StableHash(string value)
    {
        uint hash = 2166136261u;
        foreach (var c in value)
        {
            hash ^= c;
            hash *= 16777619u;
        }
        return hash;
    }

    /// <summary>
    /// 스테이징된 항목마다 겹치지 않는 실제 경로를 내주되 원래 파일 이름은 유지한다
    /// (드롭 받은 쪽에 보이는 이름이 그것이기 때문).
    ///
    /// 스테이징 폴더 안은 평평해서, 서로 다른 폴더에 있던 "readme.txt" 두 개 —
    /// 또는 한 번의 드래그에 포함된 서로 다른 아카이브의 같은 이름 —— 가 같은 경로로
    /// 떨어져 뒤엣것이 앞엣것을 조용히 덮어쓴다. 사용자는 두 개를 끌었는데 하나만
    /// 받게 된다. 충돌하는 항목만 번호 붙인 하위 폴더로 보낸다.
    /// </summary>
    private sealed class StagingNameReserver(string root)
    {
        private readonly HashSet<string> _taken = new(StringComparer.OrdinalIgnoreCase);
        private int _slot;

        internal string Reserve(string fileName)
        {
            if (_taken.Add(fileName))
                return Path.Combine(root, fileName);

            var dir = Path.Combine(root, (++_slot).ToString());
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, fileName);
        }
    }

    private static List<string> StageFromArchive(
        string archiveFilePath,
        List<string> internalPaths,
        string stagingRoot,
        StagingNameReserver reserver,
        CancellationToken ct)
    {
        var staged = new List<string>();

        using var stream = new FileStream(archiveFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false,
            entryNameEncoding: ZipEntryNameEncoding.Instance);

        foreach (var internalPath in internalPaths)
        {
            ct.ThrowIfCancellationRequested();

            var normalized = internalPath.Replace('\\', '/').TrimEnd('/');
            if (normalized.Length == 0) continue;

            // An exact match is a file; otherwise anything under "<path>/" makes it a folder.
            var file = archive.Entries.FirstOrDefault(e =>
                !string.IsNullOrEmpty(e.Name) &&
                string.Equals(e.FullName.Replace('\\', '/'), normalized, StringComparison.OrdinalIgnoreCase));

            try
            {
                if (file is not null)
                {
                    var target = reserver.Reserve(SanitizeName(Path.GetFileName(normalized)));
                    ExtractEntry(file, target, ct);
                    staged.Add(target);
                    continue;
                }

                var prefix = normalized + "/";
                var children = archive.Entries
                    .Where(e => e.FullName.Replace('\\', '/').StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (children.Count == 0)
                {
                    DebugLogger.Log($"[ArchiveStaging] entry not found: {internalPath} in {archiveFilePath}");
                    continue;
                }

                var folderRoot = reserver.Reserve(SanitizeName(Path.GetFileName(normalized)));
                Directory.CreateDirectory(folderRoot);
                var folderRootWithSep = folderRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

                foreach (var child in children)
                {
                    ct.ThrowIfCancellationRequested();

                    var relative = child.FullName.Replace('\\', '/')[prefix.Length..];
                    if (relative.Length == 0) continue;

                    var target = Path.GetFullPath(Path.Combine(folderRoot, relative.Replace('/', Path.DirectorySeparatorChar)));

                    // Entry names are attacker-controlled; keep them inside the staging folder.
                    if (!target.StartsWith(folderRootWithSep, StringComparison.OrdinalIgnoreCase))
                    {
                        DebugLogger.Log($"[ArchiveStaging] skipped (outside staging root): {child.FullName}");
                        continue;
                    }

                    if (string.IsNullOrEmpty(child.Name))
                        Directory.CreateDirectory(target);
                    else
                        ExtractEntry(child, target, ct);
                }

                staged.Add(folderRoot);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                DebugLogger.Log($"[ArchiveStaging] entry failed {internalPath}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return staged;
    }

    private static void ExtractEntry(ZipArchiveEntry entry, string targetPath, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        using var source = entry.Open();
        using var target = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81920, FileOptions.SequentialScan);

        var buffer = new byte[81920];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            target.Write(buffer, 0, read);
        }
    }

    /// <summary>Replaces characters Windows will not accept in a file name.</summary>
    private static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "item";

        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var cleaned = new string(chars).Trim();
        return cleaned.Length == 0 ? "item" : cleaned;
    }

    /// <summary>
    /// Best-effort removal of earlier staged folders.
    ///
    /// The files have to outlive the drag or paste that produced them, and there is no
    /// signal telling us the receiving app finished copying — so they are cleaned on a
    /// timer basis on the next call rather than immediately.
    /// </summary>
    private static void CleanupStaleStagingFolders()
    {
        try
        {
            var temp = Path.GetTempPath();
            var cutoff = DateTime.UtcNow - StagingLifetime;

            foreach (var dir in Directory.EnumerateDirectories(temp, StagingPrefix + "*"))
            {
                // 미리보기 폴더는 계속 재사용되므로 생성 시각으로 지우면 안 된다.
                // 그쪽은 파일 개수 상한으로 스스로 정리한다.
                if (string.Equals(Path.GetFileName(dir), PreviewFolderName, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    if (Directory.GetCreationTimeUtc(dir) < cutoff)
                        Directory.Delete(dir, recursive: true);
                }
                catch
                {
                    // In use, locked, or already gone — nothing to do about it here.
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"[ArchiveStaging] cleanup skipped: {ex.GetType().Name}");
        }
    }
}
