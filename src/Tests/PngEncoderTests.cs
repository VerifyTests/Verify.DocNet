[TestFixture]
public class PngEncoderTests
{
    [Test]
    public void StartsWithPngSignature()
    {
        var png = Encode(Gradient(1, 1), 1, 1);

        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        Assert.That(png.AsSpan(0, 8).SequenceEqual(signature), Is.True);
    }

    [TestCase(1, 1)]
    [TestCase(2, 3)]
    [TestCase(7, 5)]
    [TestCase(64, 48)]
    public void HeaderDescribesDimensionsAndRgbaFormat(int width, int height)
    {
        var decoded = Decode(Encode(Gradient(width, height), width, height));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Width, Is.EqualTo(width));
            Assert.That(decoded.Height, Is.EqualTo(height));
            Assert.That(decoded.BitDepth, Is.EqualTo(8));
            Assert.That(decoded.ColorType, Is.EqualTo(6)); // truecolor with alpha
            Assert.That(decoded.Compression, Is.EqualTo(0)); // deflate
            Assert.That(decoded.Filter, Is.EqualTo(0)); // adaptive
            Assert.That(decoded.Interlace, Is.EqualTo(0)); // none
        });
    }

    [Test]
    public void ChunkOrderIsIhdrThenIdatThenIend()
    {
        var types = ReadChunks(Encode(Gradient(4, 4), 4, 4))
            .Select(_ => _.type)
            .ToList();

        Assert.That(types, Is.EqualTo(["IHDR", "IDAT", "IEND"]));
    }

    [Test]
    public void EndChunkIsEmpty()
    {
        var end = ReadChunks(Encode(Gradient(4, 4), 4, 4))
            .Single(_ => _.type == "IEND");

        Assert.That(end.data, Is.Empty);
    }

    [Test]
    public void EveryChunkHasValidCrc()
    {
        foreach (var chunk in ReadChunks(Encode(Gradient(10, 6), 10, 6)))
        {
            Assert.That(chunk.crcValid, Is.True, $"Invalid CRC for chunk {chunk.type}");
        }
    }

    [Test]
    public void EveryScanlineUsesNoneFilter()
    {
        const int width = 5;
        const int height = 4;
        var decoded = Decode(Encode(Gradient(width, height), width, height));

        for (var y = 0; y < height; y++)
        {
            Assert.That(decoded.FilterByte(y), Is.EqualTo(0), $"Row {y} should use the None filter");
        }
    }

    [Test]
    public void RoundTripsPixelsConvertingBgraToRgba()
    {
        const int width = 3;
        const int height = 2;
        var bgra = Gradient(width, height);
        var decoded = Decode(Encode(bgra, width, height));

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var source = (y * width + x) * 4;
            var b = bgra[source];
            var g = bgra[source + 1];
            var r = bgra[source + 2];
            var a = bgra[source + 3];

            Assert.That(decoded.Pixel(x, y), Is.EqualTo((r, g, b, a)), $"Pixel ({x},{y})");
        }
    }

    [Test]
    public void PreservesAlphaChannel()
    {
        // a single fully transparent blue pixel (B=255, G=0, R=0, A=0)
        var decoded = Decode(Encode([255, 0, 0, 0], 1, 1));

        Assert.That(decoded.Pixel(0, 0), Is.EqualTo(((byte)0, (byte)0, (byte)255, (byte)0)));
    }

    [Test]
    public void OutputIsDeterministic()
    {
        var bgra = Gradient(20, 12);

        Assert.That(Encode(bgra, 20, 12), Is.EqualTo(Encode(bgra, 20, 12)));
    }

    [Test]
    public void ProducesPngDecodableByImageMagick()
    {
        const int width = 8;
        const int height = 6;
        var bgra = Gradient(width, height);

        using var image = new MagickImage(Encode(bgra, width, height));

        Assert.That(image.Format, Is.EqualTo(MagickFormat.Png));
        Assert.That((int)image.Width, Is.EqualTo(width));
        Assert.That((int)image.Height, Is.EqualTo(height));
    }

    static byte[] Encode(byte[] bgra, int width, int height)
    {
        using var stream = new MemoryStream();
        PngEncoder.WriteBgraAsPng(bgra, width, height, stream);
        return stream.ToArray();
    }

    // Deterministic pattern that varies every channel, including alpha.
    static byte[] Gradient(int width, int height)
    {
        var bytes = new byte[width * height * 4];
        var i = 0;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            bytes[i++] = (byte)(x * 7 + y); // B
            bytes[i++] = (byte)(x + y * 5); // G
            bytes[i++] = (byte)(x * 3 + y * 2); // R
            bytes[i++] = (byte)(255 - ((x + y) & 0xFF)); // A
        }

        return bytes;
    }

    static List<(string type, byte[] data, bool crcValid)> ReadChunks(byte[] png)
    {
        var chunks = new List<(string, byte[], bool)>();
        var pos = 8; // skip the signature
        while (pos < png.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(pos, 4));
            var type = Encoding.ASCII.GetString(png, pos + 4, 4);
            var data = png.AsSpan(pos + 8, length).ToArray();
            var storedCrc = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(pos + 8 + length, 4));
            // CRC covers the chunk type plus its data.
            var crcValid = storedCrc == Crc32(png.AsSpan(pos + 4, 4 + length));
            chunks.Add((type, data, crcValid));
            pos += 12 + length;
        }

        return chunks;
    }

    static DecodedPng Decode(byte[] png)
    {
        var chunks = ReadChunks(png);
        var header = chunks.Single(_ => _.type == "IHDR").data;

        using var compressed = new MemoryStream();
        foreach (var chunk in chunks.Where(_ => _.type == "IDAT"))
        {
            compressed.Write(chunk.data);
        }

        compressed.Position = 0;
        using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
        using var scanlines = new MemoryStream();
        zlib.CopyTo(scanlines);

        return new()
        {
            Width = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(0, 4)),
            Height = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(4, 4)),
            BitDepth = header[8],
            ColorType = header[9],
            Compression = header[10],
            Filter = header[11],
            Interlace = header[12],
            Scanlines = scanlines.ToArray()
        };
    }

    // Bitwise CRC-32, independent of the table-based implementation under test.
    static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }

        return crc ^ 0xFFFFFFFFu;
    }

    class DecodedPng
    {
        public int Width;
        public int Height;
        public byte BitDepth;
        public byte ColorType;
        public byte Compression;
        public byte Filter;
        public byte Interlace;
        public byte[] Scanlines = [];

        int Stride => Width * 4 + 1;

        public byte FilterByte(int y) => Scanlines[y * Stride];

        public (byte r, byte g, byte b, byte a) Pixel(int x, int y)
        {
            var offset = y * Stride + 1 + x * 4;
            return (Scanlines[offset], Scanlines[offset + 1], Scanlines[offset + 2], Scanlines[offset + 3]);
        }
    }
}
