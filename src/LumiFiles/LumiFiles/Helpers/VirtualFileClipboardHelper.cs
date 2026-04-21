using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

namespace LumiFiles.Helpers;

/// <summary>
/// OLE 클립보드에서 가상 파일(FileGroupDescriptorW)을 읽어 대상 폴더에 기록.
/// RDP 원격 복사, Outlook 첨부파일 등에서 사용.
/// </summary>
public static class VirtualFileClipboardHelper
{
    // ── FILEDESCRIPTORW 네이티브 구조체 ──

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    private struct FILEDESCRIPTORW
    {
        public uint dwFlags;
        public Guid clsid;
        public int sizelCx;     // SIZE.cx
        public int sizelCy;     // SIZE.cy
        public int pointlX;     // POINTL.x
        public int pointlY;     // POINTL.y
        public uint dwFileAttributes;
        public FILETIME ftCreationTime;
        public FILETIME ftLastAccessTime;
        public FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
    }

    private const uint FD_FILESIZE = 0x00000040;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    private const int S_OK = 0;
    private const int BufferSize = 65536; // 64KB 청크

    // 클립보드 포맷 ID 캐시 (한 번만 등록)
    private static ushort _cfFileGroupDescriptorW;
    private static ushort _cfFileContents;

    private static ushort CfFileGroupDescriptorW =>
        _cfFileGroupDescriptorW != 0
            ? _cfFileGroupDescriptorW
            : (_cfFileGroupDescriptorW = NativeMethods.RegisterClipboardFormatW("FileGroupDescriptorW"));

    private static ushort CfFileContents =>
        _cfFileContents != 0
            ? _cfFileContents
            : (_cfFileContents = NativeMethods.RegisterClipboardFormatW("FileContents"));

    /// <summary>
    /// 클립보드에 FileGroupDescriptorW 가상 파일이 있는지 확인
    /// </summary>
    public static bool IsVirtualFileDataAvailable()
    {
        try
        {
            int hr = NativeMethods.OleGetClipboard(out IDataObject dataObj);
            if (hr != S_OK || dataObj == null) return false;

            try
            {
                var fmt = new FORMATETC
                {
                    cfFormat = (short)CfFileGroupDescriptorW,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex = -1,
                    ptd = IntPtr.Zero,
                    tymed = TYMED.TYMED_HGLOBAL
                };
                return dataObj.QueryGetData(ref fmt) == S_OK;
            }
            finally
            {
                Marshal.ReleaseComObject(dataObj);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"[VirtualFile] IsVirtualFileDataAvailable 예외: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 가상 파일을 대상 폴더에 기록. RDP, Outlook 첨부파일 등에서 사용.
    /// </summary>
    /// <returns>생성된 파일 경로 리스트</returns>
    public static async Task<List<string>> PasteVirtualFilesAsync(
        string destinationFolder,
        IProgress<(string fileName, long totalBytes, long processedBytes, int fileIndex, int totalFiles)>? progress = null,
        CancellationToken ct = default)
    {
        var createdFiles = new List<string>();

        int hr = NativeMethods.OleGetClipboard(out IDataObject dataObj);
        if (hr != S_OK || dataObj == null)
            throw new InvalidOperationException($"OleGetClipboard 실패 (HRESULT=0x{hr:X8})");

        try
        {
            // FileGroupDescriptorW 읽기
            var descriptors = ReadFileDescriptors(dataObj);
            int totalFiles = descriptors.Count;

            DebugLogger.Log($"[VirtualFile] 가상 파일 {totalFiles}개 감지됨, 대상: {destinationFolder}");

            for (int i = 0; i < totalFiles; i++)
            {
                ct.ThrowIfCancellationRequested();

                var desc = descriptors[i];
                string relativePath = desc.cFileName ?? "";

                // 빈 파일명 방어
                if (string.IsNullOrWhiteSpace(relativePath))
                    relativePath = $"unnamed_{i}";

                // 보안: 경로 탐색 공격 방지 — Path.GetFullPath로 정규화 후 대상 폴더 하위인지 검증
                relativePath = relativePath.Replace('/', '\\');
                string targetPath = Path.GetFullPath(Path.Combine(destinationFolder, relativePath));
                string destNorm = Path.GetFullPath(destinationFolder).TrimEnd('\\') + "\\";
                if (!targetPath.StartsWith(destNorm, StringComparison.OrdinalIgnoreCase))
                {
                    DebugLogger.Log($"[VirtualFile] 경로 탐색 시도 차단: {relativePath}");
                    continue;
                }

                try
                {
                    // 디렉터리 항목
                    if ((desc.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
                    {
                        Directory.CreateDirectory(targetPath);
                        DebugLogger.Log($"[VirtualFile] 디렉터리 생성: {targetPath}");
                        createdFiles.Add(targetPath);
                        continue;
                    }

                    // 부모 디렉터리 보장
                    string? parentDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(parentDir))
                        Directory.CreateDirectory(parentDir);

                    // 파일 크기 (알 수 있는 경우)
                    long totalBytes = (desc.dwFlags & FD_FILESIZE) != 0
                        ? ((long)desc.nFileSizeHigh << 32) | desc.nFileSizeLow
                        : -1;

                    // 중복 파일명 처리
                    targetPath = GetUniqueFilePath(targetPath);

                    string fileName = Path.GetFileName(targetPath);
                    progress?.Report((fileName, totalBytes, 0, i, totalFiles));

                    // FileContents 가져오기
                    long processedBytes = await ExtractFileContentsAsync(
                        dataObj, i, targetPath, totalBytes, fileName, totalFiles, progress, ct);

                    DebugLogger.Log($"[VirtualFile] 파일 저장 완료: {targetPath} ({processedBytes:#,0} bytes)");
                    createdFiles.Add(targetPath);
                }
                catch (OperationCanceledException)
                {
                    // 취소 시 부분 기록 파일 정리
                    TryDeletePartialFile(targetPath);
                    throw;
                }
                catch (Exception ex)
                {
                    DebugLogger.Log($"[VirtualFile] 파일 {i} ({relativePath}) 저장 실패: {ex.Message}");
                    TryDeletePartialFile(targetPath);
                    // 개별 파일 실패는 건너뛰고 계속 진행
                }
            }
        }
        finally
        {
            Marshal.ReleaseComObject(dataObj);
        }

        DebugLogger.Log($"[VirtualFile] 붙여넣기 완료: {createdFiles.Count}개 파일 생성");
        return createdFiles;
    }

    /// <summary>
    /// FileGroupDescriptorW에서 파일 디스크립터 배열 파싱
    /// </summary>
    private static List<FILEDESCRIPTORW> ReadFileDescriptors(IDataObject dataObj)
    {
        var fmt = new FORMATETC
        {
            cfFormat = (short)CfFileGroupDescriptorW,
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            lindex = -1,
            ptd = IntPtr.Zero,
            tymed = TYMED.TYMED_HGLOBAL
        };

        dataObj.GetData(ref fmt, out STGMEDIUM medium);
        var descriptors = new List<FILEDESCRIPTORW>();

        try
        {
            IntPtr ptr = NativeMethods.GlobalLock(medium.unionmember);
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException("GlobalLock 실패 — FileGroupDescriptorW");

            try
            {
                // 첫 4바이트: 파일 개수 (cItems)
                uint cItems = (uint)Marshal.ReadInt32(ptr);
                int descriptorSize = Marshal.SizeOf<FILEDESCRIPTORW>();
                IntPtr arrayStart = ptr + 4; // cItems 뒤부터 배열 시작

                for (uint i = 0; i < cItems; i++)
                {
                    var desc = Marshal.PtrToStructure<FILEDESCRIPTORW>(arrayStart + (int)(i * descriptorSize));
                    descriptors.Add(desc);
                }
            }
            finally
            {
                NativeMethods.GlobalUnlock(medium.unionmember);
            }
        }
        finally
        {
            NativeMethods.ReleaseStgMedium(ref medium);
        }

        return descriptors;
    }

    /// <summary>
    /// FileContents에서 개별 파일 데이터를 추출하여 디스크에 기록
    /// </summary>
    private static async Task<long> ExtractFileContentsAsync(
        IDataObject dataObj,
        int fileIndex,
        string targetPath,
        long totalBytes,
        string fileName,
        int totalFiles,
        IProgress<(string fileName, long totalBytes, long processedBytes, int fileIndex, int totalFiles)>? progress,
        CancellationToken ct)
    {
        var fmt = new FORMATETC
        {
            cfFormat = (short)CfFileContents,
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            lindex = fileIndex,
            ptd = IntPtr.Zero,
            tymed = TYMED.TYMED_ISTREAM | TYMED.TYMED_HGLOBAL
        };

        dataObj.GetData(ref fmt, out STGMEDIUM medium);

        try
        {
            if (medium.tymed == TYMED.TYMED_ISTREAM)
            {
                return await ExtractFromIStreamAsync(
                    medium, targetPath, totalBytes, fileName, fileIndex, totalFiles, progress, ct);
            }
            else if (medium.tymed == TYMED.TYMED_HGLOBAL)
            {
                return ExtractFromHGlobal(
                    medium, targetPath, totalBytes, fileName, fileIndex, totalFiles, progress);
            }
            else
            {
                throw new NotSupportedException($"지원하지 않는 TYMED: {medium.tymed}");
            }
        }
        finally
        {
            NativeMethods.ReleaseStgMedium(ref medium);
        }
    }

    /// <summary>
    /// IStream에서 64KB 청크 단위로 읽어 파일에 기록
    /// </summary>
    private static async Task<long> ExtractFromIStreamAsync(
        STGMEDIUM medium,
        string targetPath,
        long totalBytes,
        string fileName,
        int fileIndex,
        int totalFiles,
        IProgress<(string fileName, long totalBytes, long processedBytes, int fileIndex, int totalFiles)>? progress,
        CancellationToken ct)
    {
        var istream = (IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
        long processedBytes = 0;
        byte[] buffer = new byte[BufferSize];

        try
        {
            using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None,
                BufferSize, FileOptions.Asynchronous);

            IntPtr bytesReadPtr = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                // 무한 루프 방어: 알려진 파일 크기의 3배 또는 10GB 중 큰 값을 상한으로 설정
                long maxBytes = Math.Max(totalBytes > 0 ? totalBytes * 3 : long.MaxValue, 10L * 1024 * 1024 * 1024);
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    Marshal.WriteInt32(bytesReadPtr, 0);
                    istream.Read(buffer, buffer.Length, bytesReadPtr);
                    int bytesRead = Marshal.ReadInt32(bytesReadPtr);
                    if (bytesRead == 0) break;

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    processedBytes += bytesRead;

                    if (processedBytes > maxBytes)
                    {
                        DebugLogger.Log($"[VirtualFile] 스트림 읽기 상한 초과: {processedBytes} > {maxBytes}");
                        break;
                    }

                    progress?.Report((fileName, totalBytes, processedBytes, fileIndex, totalFiles));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(bytesReadPtr);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(istream);
        }

        return processedBytes;
    }

    /// <summary>
    /// HGLOBAL 메모리 블록에서 파일 데이터를 한 번에 복사
    /// </summary>
    private static long ExtractFromHGlobal(
        STGMEDIUM medium,
        string targetPath,
        long totalBytes,
        string fileName,
        int fileIndex,
        int totalFiles,
        IProgress<(string fileName, long totalBytes, long processedBytes, int fileIndex, int totalFiles)>? progress)
    {
        IntPtr ptr = NativeMethods.GlobalLock(medium.unionmember);
        if (ptr == IntPtr.Zero)
            throw new InvalidOperationException("GlobalLock 실패 — FileContents");

        try
        {
            long size = (long)(ulong)NativeMethods.GlobalSize(medium.unionmember);
            if (size == 0 && totalBytes > 0)
                size = totalBytes;
            if (size == 0)
            {
                DebugLogger.Log($"[VirtualFile] HGLOBAL 크기 0 — 빈 파일 생성: {fileName}");
            }

            // 2GB 초과 방어: Marshal.Copy는 int length만 지원
            if (size > int.MaxValue)
            {
                DebugLogger.Log($"[VirtualFile] HGLOBAL 크기 초과 ({size} bytes), 청크 기록: {fileName}");
                using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
                long remaining = size;
                long offset = 0;
                byte[] chunk = new byte[BufferSize];
                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(remaining, BufferSize);
                    Marshal.Copy(ptr + (int)offset, chunk, 0, toRead);
                    fs.Write(chunk, 0, toRead);
                    offset += toRead;
                    remaining -= toRead;
                }
            }
            else
            {
                byte[] data = new byte[size];
                if (size > 0) Marshal.Copy(ptr, data, 0, (int)size);
                File.WriteAllBytes(targetPath, data);
            }

            progress?.Report((fileName, size, size, fileIndex, totalFiles));
            return size;
        }
        finally
        {
            NativeMethods.GlobalUnlock(medium.unionmember);
        }
    }

    /// <summary>
    /// 파일 경로 충돌 시 고유 이름 생성: file.txt → file (1).txt → file (2).txt ...
    /// </summary>
    private static string GetUniqueFilePath(string path)
    {
        if (!File.Exists(path)) return path;

        string dir = Path.GetDirectoryName(path)!;
        string nameWithoutExt = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);

        int counter = 1;
        string newPath;
        do
        {
            newPath = Path.Combine(dir, $"{nameWithoutExt} ({counter}){ext}");
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }

    /// <summary>
    /// 부분 기록된 파일 삭제 시도 (취소/오류 시)
    /// </summary>
    private static void TryDeletePartialFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"[VirtualFile] 부분 파일 삭제 실패: {path} — {ex.Message}");
        }
    }
}
