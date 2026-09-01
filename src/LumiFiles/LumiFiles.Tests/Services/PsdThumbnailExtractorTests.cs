using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LumiFiles.Thumbs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LumiFiles.Tests.Services;

/// <summary>
/// Issue #56: .psd 썸네일이 Windows PSD 코덱 없이도 나오도록 파일 내장 썸네일을 직접 파싱한다.
/// 실제 PSD 를 저장소에 넣지 않고, 규격에 맞는 최소 PSD 를 만들어 파서를 검증한다.
///
/// PSD 구조(전부 big-endian):
///   Header(26) → Color Mode Data(len+data) → Image Resources(len + 8BIM 블록들) → ...
///   썸네일 블록: "8BIM" | id(2) | Pascal 이름(짝수 패딩) | size(4) | data(짝수 패딩)
///   data 앞 28바이트가 헤더, 그 뒤가 JPEG 본문.
/// </summary>
[TestClass]
public class PsdThumbnailExtractorTests
{
    private string _dir = null!;

    [TestInitialize]
    public void Setup()
    {
        _dir = Path.Combine(Path.GetTempPath(), "LumiFilesPsdTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    // ── 헬퍼: 바이트 조립 ────────────────────────────────────────
    private static void U32(List<byte> b, uint v)
    {
        b.Add((byte)(v >> 24)); b.Add((byte)(v >> 16)); b.Add((byte)(v >> 8)); b.Add((byte)v);
    }

    private static void U16(List<byte> b, ushort v)
    {
        b.Add((byte)(v >> 8)); b.Add((byte)v);
    }

    /// <summary>SOI/EOI 만 갖춘 최소 JPEG — 파서는 SOI 만 확인한다.</summary>
    private static byte[] FakeJpeg(int payload = 32)
    {
        var b = new List<byte> { 0xFF, 0xD8 };
        for (int i = 0; i < payload; i++) b.Add((byte)(i & 0xFF));
        b.Add(0xFF); b.Add(0xD9);
        return b.ToArray();
    }

    /// <summary>썸네일 리소스 블록 하나를 만든다.</summary>
    private static byte[] ThumbnailResource(ushort resourceId, uint format, byte[] jpeg, string name = "")
    {
        var data = new List<byte>();
        U32(data, format);        // format (1 = kJpegRGB)
        U32(data, 120);           // width
        U32(data, 80);            // height
        U32(data, 360);           // widthBytes
        U32(data, (uint)jpeg.Length);  // totalSize
        U32(data, (uint)jpeg.Length);  // compressedSize
        U16(data, 24);            // bitsPerPixel
        U16(data, 1);             // planes
        data.AddRange(jpeg);

        var block = new List<byte>();
        block.AddRange("8BIM"u8.ToArray());
        U16(block, resourceId);
        // Pascal 이름: 길이 1바이트 + 본문, 전체가 짝수가 되도록 패딩
        block.Add((byte)name.Length);
        foreach (var c in name) block.Add((byte)c);
        if ((1 + name.Length) % 2 != 0) block.Add(0);
        U32(block, (uint)data.Count);
        block.AddRange(data);
        if (data.Count % 2 != 0) block.Add(0);   // 데이터 짝수 패딩
        return block.ToArray();
    }

    /// <summary>임의의(썸네일 아닌) 리소스 블록 — 건너뛰기 로직 검증용.</summary>
    private static byte[] OtherResource(ushort resourceId, int dataLen, string name = "")
    {
        var block = new List<byte>();
        block.AddRange("8BIM"u8.ToArray());
        U16(block, resourceId);
        block.Add((byte)name.Length);
        foreach (var c in name) block.Add((byte)c);
        if ((1 + name.Length) % 2 != 0) block.Add(0);
        U32(block, (uint)dataLen);
        for (int i = 0; i < dataLen; i++) block.Add((byte)i);
        if (dataLen % 2 != 0) block.Add(0);
        return block.ToArray();
    }

    private string WritePsd(string signature, IEnumerable<byte[]> resources)
    {
        var f = new List<byte>();
        f.AddRange(System.Text.Encoding.ASCII.GetBytes(signature));  // "8BPS"
        U16(f, 1);                       // version
        for (int i = 0; i < 6; i++) f.Add(0);   // reserved
        U16(f, 3);                       // channels
        U32(f, 80);                      // height
        U32(f, 120);                     // width
        U16(f, 8);                       // depth
        U16(f, 3);                       // color mode (RGB)

        U32(f, 0);                       // Color Mode Data: 길이 0

        var res = new List<byte>();
        foreach (var r in resources) res.AddRange(r);
        U32(f, (uint)res.Count);         // Image Resources 길이
        f.AddRange(res);

        U32(f, 0);                       // Layer/Mask 길이 0

        var path = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".psd");
        File.WriteAllBytes(path, f.ToArray());
        return path;
    }

    // ── 정상 추출 ────────────────────────────────────────────────

    [TestMethod]
    public void Extract_Ps5ThumbnailResource_ReturnsJpeg()
    {
        var jpeg = FakeJpeg();
        var path = WritePsd("8BPS", new[] { ThumbnailResource(1036, format: 1, jpeg) });

        var result = PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None);

        CollectionAssert.AreEqual(jpeg, result, "내장 JPEG 바이트가 그대로 나와야 한다");
    }

    /// <summary>
    /// Photoshop 4.0 판(ID 1033)은 레이아웃은 같지만 JPEG 데이터가 BGR 순서라
    /// 그대로 쓰면 색이 뒤바뀐다. 틀린 색을 보여주느니 셸 폴백에 맡긴다 → null.
    /// </summary>
    [TestMethod]
    public void Extract_Ps4ThumbnailResource_IsIgnored()
    {
        var path = WritePsd("8BPS", new[] { ThumbnailResource(1033, format: 1, FakeJpeg(10)) });

        Assert.IsNull(PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None),
            "1033 은 BGR 순서라 사용하면 색이 뒤집힌다");
    }

    /// <summary>1033 과 1036 이 함께 있으면 1036 을 쓴다.</summary>
    [TestMethod]
    public void Extract_PrefersPs5OverPs4()
    {
        var correct = FakeJpeg(12);
        var path = WritePsd("8BPS", new[]
        {
            ThumbnailResource(1033, format: 1, FakeJpeg(30)),
            ThumbnailResource(1036, format: 1, correct),
        });

        CollectionAssert.AreEqual(correct,
            PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None));
    }

    /// <summary>앞선 다른 리소스들을 정확히 건너뛰어야 찾을 수 있다 (패딩 계산 검증).</summary>
    [TestMethod]
    public void Extract_SkipsPrecedingResources_IncludingOddSizes()
    {
        var jpeg = FakeJpeg(7);
        var path = WritePsd("8BPS", new[]
        {
            OtherResource(1005, dataLen: 16),
            OtherResource(1006, dataLen: 7),        // 홀수 → 패딩 1
            OtherResource(1010, dataLen: 3, name: "ab"),  // 이름 있는 블록
            ThumbnailResource(1036, format: 1, jpeg),
        });

        var result = PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None);

        CollectionAssert.AreEqual(jpeg, result);
    }

    /// <summary>Pascal 이름이 홀/짝일 때 모두 정렬이 맞아야 한다.</summary>
    [DataTestMethod]
    [DataRow("")]
    [DataRow("a")]
    [DataRow("ab")]
    [DataRow("abc")]
    public void Extract_HandlesPascalNamePadding(string name)
    {
        var jpeg = FakeJpeg(5);
        var path = WritePsd("8BPS", new[] { ThumbnailResource(1036, format: 1, jpeg, name) });

        var result = PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None);

        CollectionAssert.AreEqual(jpeg, result, $"이름 길이 {name.Length} 에서 정렬이 어긋났다");
    }

    // ── 폴백(null) 케이스 — 호출측이 셸 경로로 넘어간다 ──────────

    [TestMethod]
    public void Extract_NoThumbnailResource_ReturnsNull()
    {
        var path = WritePsd("8BPS", new[] { OtherResource(1005, dataLen: 8) });
        Assert.IsNull(PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None));
    }

    [TestMethod]
    public void Extract_NotAPsdSignature_ReturnsNull()
    {
        var path = WritePsd("8BPT", new[] { ThumbnailResource(1036, 1, FakeJpeg()) });
        Assert.IsNull(PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None));
    }

    /// <summary>kRawRGB(format=0)는 JPEG 이 아니므로 디코더에 넘기지 않는다.</summary>
    [TestMethod]
    public void Extract_RawRgbFormat_ReturnsNull()
    {
        var path = WritePsd("8BPS", new[] { ThumbnailResource(1036, format: 0, FakeJpeg()) });
        Assert.IsNull(PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None));
    }

    /// <summary>SOI 가 없으면 오프셋 해석이 틀린 것 — 잘못된 바이트를 넘기면 안 된다.</summary>
    [TestMethod]
    public void Extract_PayloadWithoutJpegSoi_ReturnsNull()
    {
        var notJpeg = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var path = WritePsd("8BPS", new[] { ThumbnailResource(1036, format: 1, notJpeg) });
        Assert.IsNull(PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None));
    }

    [TestMethod]
    public void Extract_TruncatedFile_ReturnsNull()
    {
        var path = WritePsd("8BPS", new[] { ThumbnailResource(1036, 1, FakeJpeg(64)) });
        var all = File.ReadAllBytes(path);
        File.WriteAllBytes(path, all[..(all.Length / 2)]);   // 중간에서 잘라냄

        Assert.IsNull(PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None));
    }

    [TestMethod]
    public void Extract_EmptyFile_ReturnsNull()
    {
        var path = Path.Combine(_dir, "empty.psd");
        File.WriteAllBytes(path, Array.Empty<byte>());
        Assert.IsNull(PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None));
    }

    [TestMethod]
    public void Extract_MissingFile_ReturnsNull()
    {
        var path = Path.Combine(_dir, "does-not-exist.psd");
        Assert.IsNull(PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, CancellationToken.None));
    }

    /// <summary>취소는 삼키지 않고 전파해야 한다 (워커의 취소 처리와 일관).</summary>
    [TestMethod]
    public void Extract_Cancelled_Throws()
    {
        var path = WritePsd("8BPS", new[] { ThumbnailResource(1036, 1, FakeJpeg()) });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsException<OperationCanceledException>(
            () => PsdThumbnailExtractor.TryExtractThumbnailJpeg(path, cts.Token));
    }
}
