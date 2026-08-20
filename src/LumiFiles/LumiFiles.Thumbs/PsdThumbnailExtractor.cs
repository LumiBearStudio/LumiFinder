using System;
using System.IO;
using System.Threading;

namespace LumiFiles.Thumbs;

/// <summary>
/// Issue #56: .psd(Adobe Photoshop) 파일에 내장된 썸네일을 직접 추출한다.
///
/// 기존에는 셸 썸네일 핸들러에 의존해서, Windows 에 PSD 코덱이 등록돼 있어야만 썸네일이 보였다.
/// 최신 Photoshop 은 Windows 용 썸네일 코덱을 설치해주지 않으므로 대부분의 환경에서
/// 기본 아이콘만 표시된다(업스트림 이슈에서 제보된 상황).
///
/// PSD 는 파일 안에 JPEG 썸네일을 이미 갖고 있다. Image Resources 섹션의
/// 리소스 블록 중 ID 1036(Photoshop 5.0+) 또는 1033(Photoshop 4.0)이 그것이다.
/// 이를 직접 읽어내면 Photoshop 설치 여부·코덱 유무와 무관하게 동작한다.
///
/// 파일 구조 (모두 big-endian):
///   [Header 26]  "8BPS" | version(2) | reserved(6) | channels(2) | height(4) | width(4) | depth(2) | mode(2)
///   [Color Mode] length(4) + data
///   [Image Res ] length(4) + 리소스 블록 반복
///       블록: "8BIM" | id(2) | Pascal 이름(길이1+본문, 전체 짝수 패딩) | size(4) | data(짝수 패딩)
///   [Layer/Mask] ...  (여기까지 오면 썸네일 없음)
///
///   썸네일 리소스 data 앞 28바이트가 헤더이고 그 뒤가 JPEG 본문:
///     format(4) 1=kJpegRGB 0=kRawRGB | width(4) | height(4) | widthBytes(4)
///     totalSize(4) | compressedSize(4) | bitsPerPixel(2) | planes(2)
///
/// 격리 워커(LumiFiles.Thumbs.exe)에서만 호출되므로 어떤 실패도 메인 앱에 영향을 주지 않는다.
/// 손상/비표준 파일은 조용히 null 을 반환하고 호출측이 셸 경로로 폴백한다.
/// </summary>
internal static class PsdThumbnailExtractor
{
    private const int ThumbnailResourcePs5 = 1036;  // kJpegRGB (BGR 순서 아님)
    private const int ThumbnailResourcePs4 = 1033;  // 구버전, 동일 레이아웃
    private const int ThumbnailHeaderSize = 28;

    /// <summary>리소스 섹션 탐색 상한 — 손상 파일에서 무한 루프를 막는다.</summary>
    private const int MaxResourceBlocks = 4096;

    /// <summary>썸네일 JPEG 상한(16MB). 비정상 길이로 대용량 할당하는 것을 방지.</summary>
    private const int MaxThumbnailBytes = 16 * 1024 * 1024;

    /// <summary>
    /// PSD 내장 썸네일(JPEG) 바이트를 반환. 없거나 읽을 수 없으면 null.
    /// </summary>
    public static byte[]? TryExtractThumbnailJpeg(string filePath, CancellationToken ct)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                bufferSize: 64 * 1024, FileOptions.SequentialScan);

            // ── Header ──
            Span<byte> header = stackalloc byte[26];
            if (!ReadExactly(fs, header)) return null;
            if (header[0] != (byte)'8' || header[1] != (byte)'B' ||
                header[2] != (byte)'P' || header[3] != (byte)'S')
            {
                WorkerLogger.Log($"[PsdThumb] not a PSD (bad signature): {Path.GetFileName(filePath)}");
                return null;
            }

            // ── Color Mode Data: 길이만 읽고 건너뜀 ──
            if (!SkipLengthPrefixedSection(fs)) return null;
            ct.ThrowIfCancellationRequested();

            // ── Image Resources: 이 안에 썸네일이 있다 ──
            if (!ReadU32BE(fs, out uint resourcesLength)) return null;
            long resourcesEnd = fs.Position + resourcesLength;
            if (resourcesLength == 0 || resourcesEnd > fs.Length)
            {
                WorkerLogger.Log($"[PsdThumb] no image resources section: {Path.GetFileName(filePath)}");
                return null;
            }

            for (int block = 0; block < MaxResourceBlocks && fs.Position + 12 <= resourcesEnd; block++)
            {
                ct.ThrowIfCancellationRequested();

                Span<byte> sig = stackalloc byte[4];
                if (!ReadExactly(fs, sig)) return null;
                if (sig[0] != (byte)'8' || sig[1] != (byte)'B' ||
                    sig[2] != (byte)'I' || sig[3] != (byte)'M')
                {
                    // 리소스 정렬이 깨졌다 — 더 진행해도 의미 없음
                    WorkerLogger.Log($"[PsdThumb] resource sync lost at block {block}: {Path.GetFileName(filePath)}");
                    return null;
                }

                if (!ReadU16BE(fs, out ushort resourceId)) return null;
                if (!SkipPascalStringPadded(fs)) return null;
                if (!ReadU32BE(fs, out uint dataSize)) return null;

                long dataStart = fs.Position;
                long paddedSize = dataSize + (dataSize % 2);  // 데이터는 짝수 바이트 정렬

                if (resourceId is ThumbnailResourcePs5 or ThumbnailResourcePs4)
                {
                    var jpeg = ReadThumbnailData(fs, dataSize, filePath, ct);
                    if (jpeg != null) return jpeg;
                    // 썸네일 리소스가 raw(비JPEG)였거나 손상 — 뒤에 다른 리소스는 없으므로 종료
                    return null;
                }

                fs.Position = dataStart + paddedSize;
            }

            WorkerLogger.Log($"[PsdThumb] no embedded thumbnail resource: {Path.GetFileName(filePath)}");
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            WorkerLogger.Log($"[PsdThumb] extract failed ({ex.GetType().Name}): {ex.Message}");
            return null;
        }
    }

    /// <summary>썸네일 리소스 본문에서 JPEG 부분만 떼어낸다. kRawRGB 형식이면 null.</summary>
    private static byte[]? ReadThumbnailData(FileStream fs, uint dataSize, string filePath, CancellationToken ct)
    {
        if (dataSize <= ThumbnailHeaderSize)
        {
            WorkerLogger.Log($"[PsdThumb] thumbnail resource too small ({dataSize}): {Path.GetFileName(filePath)}");
            return null;
        }

        Span<byte> thumbHeader = stackalloc byte[ThumbnailHeaderSize];
        if (!ReadExactly(fs, thumbHeader)) return null;

        uint format = ReadU32BE(thumbHeader[..4]);
        uint width = ReadU32BE(thumbHeader.Slice(4, 4));
        uint height = ReadU32BE(thumbHeader.Slice(8, 4));

        if (format != 1)
        {
            // kRawRGB — 실사용 파일에서는 거의 나오지 않는다. 디코딩 없이 폴백.
            WorkerLogger.Log($"[PsdThumb] unsupported thumbnail format {format} (not JPEG): {Path.GetFileName(filePath)}");
            return null;
        }
        if (width == 0 || height == 0) return null;

        long jpegLength = dataSize - ThumbnailHeaderSize;
        if (jpegLength <= 0 || jpegLength > MaxThumbnailBytes)
        {
            WorkerLogger.Log($"[PsdThumb] implausible thumbnail size {jpegLength}: {Path.GetFileName(filePath)}");
            return null;
        }

        ct.ThrowIfCancellationRequested();
        var jpeg = new byte[jpegLength];
        if (!ReadExactly(fs, jpeg)) return null;

        // JPEG SOI 확인 — 잘못 해석한 오프셋을 디코더에 넘기지 않는다
        if (jpeg.Length < 2 || jpeg[0] != 0xFF || jpeg[1] != 0xD8)
        {
            WorkerLogger.Log($"[PsdThumb] thumbnail is not JPEG (no SOI): {Path.GetFileName(filePath)}");
            return null;
        }

        WorkerLogger.Log($"[PsdThumb] extracted {width}x{height} JPEG ({jpeg.Length} bytes): {Path.GetFileName(filePath)}");
        return jpeg;
    }

    // ── 저수준 읽기 유틸 (모두 big-endian) ──────────────────────────────

    private static bool SkipLengthPrefixedSection(FileStream fs)
    {
        if (!ReadU32BE(fs, out uint length)) return false;
        long next = fs.Position + length;
        if (next > fs.Length) return false;
        fs.Position = next;
        return true;
    }

    /// <summary>Pascal 문자열(길이 1바이트 + 본문)을 건너뛴다. 전체 길이가 짝수가 되도록 패딩됨.</summary>
    private static bool SkipPascalStringPadded(FileStream fs)
    {
        int len = fs.ReadByte();
        if (len < 0) return false;
        int total = 1 + len;
        if (total % 2 != 0) total++;      // 짝수 정렬
        long next = fs.Position + (total - 1);
        if (next > fs.Length) return false;
        fs.Position = next;
        return true;
    }

    private static bool ReadU32BE(FileStream fs, out uint value)
    {
        Span<byte> b = stackalloc byte[4];
        if (!ReadExactly(fs, b)) { value = 0; return false; }
        value = ReadU32BE(b);
        return true;
    }

    private static bool ReadU16BE(FileStream fs, out ushort value)
    {
        Span<byte> b = stackalloc byte[2];
        if (!ReadExactly(fs, b)) { value = 0; return false; }
        value = (ushort)((b[0] << 8) | b[1]);
        return true;
    }

    internal static uint ReadU32BE(ReadOnlySpan<byte> b) =>
        ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];

    private static bool ReadExactly(Stream s, Span<byte> buf)
    {
        int read = 0;
        while (read < buf.Length)
        {
            int n = s.Read(buf[read..]);
            if (n <= 0) return false;
            read += n;
        }
        return true;
    }
}
