static class PngEncoder
{
    static ReadOnlySpan<byte> Signature => [137, 80, 78, 71, 13, 10, 26, 10];

    /// <summary>
    /// Encodes raw BGRA32 pixel data as a PNG (8-bit RGBA, no interlacing) and writes it to <paramref name="stream"/>.
    /// </summary>
    public static void WriteBgraAsPng(byte[] bgra, int width, int height, Stream stream)
    {
        stream.Write(Signature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header[..4], width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..8], height);
        header[8] = 8; // bit depth
        header[9] = 6; // color type: truecolor with alpha (RGBA)
        header[10] = 0; // compression: deflate
        header[11] = 0; // filter: adaptive
        header[12] = 0; // interlace: none
        WriteChunk(stream, "IHDR"u8, header);

        WriteChunk(stream, "IDAT"u8, Compress(BuildScanlines(bgra, width, height)));

        WriteChunk(stream, "IEND"u8, []);
    }

    // Each scanline is prefixed with a filter-type byte (0 = None) and pixels are converted from BGRA to RGBA.
    static byte[] BuildScanlines(byte[] bgra, int width, int height)
    {
        var stride = width * 4;
        var scanlines = new byte[height * (stride + 1)];
        var source = 0;
        var target = 0;
        for (var y = 0; y < height; y++)
        {
            scanlines[target++] = 0; // filter: None
            for (var x = 0; x < width; x++)
            {
                var b = bgra[source++];
                var g = bgra[source++];
                var r = bgra[source++];
                var a = bgra[source++];
                scanlines[target++] = r;
                scanlines[target++] = g;
                scanlines[target++] = b;
                scanlines[target++] = a;
            }
        }

        return scanlines;
    }

    static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(data);
        }

        return output.ToArray();
    }

    static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        stream.Write(type);
        stream.Write(data);

        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32.Compute(type, data));
        stream.Write(crc);
    }

    static class Crc32
    {
        static uint[] table = BuildTable();

        static uint[] BuildTable()
        {
            var table = new uint[256];
            for (var n = 0; n < 256; n++)
            {
                var c = (uint)n;
                for (var k = 0; k < 8; k++)
                {
                    if ((c & 1) == 0)
                    {
                        c >>= 1;
                    }
                    else
                    {
                        c = 0xEDB88320 ^ (c >> 1);
                    }
                }

                table[n] = c;
            }

            return table;
        }

        public static uint Compute(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
        {
            var crc = 0xFFFFFFFFu;
            crc = Update(crc, type);
            crc = Update(crc, data);
            return crc ^ 0xFFFFFFFFu;
        }

        static uint Update(uint crc, ReadOnlySpan<byte> data)
        {
            foreach (var b in data)
            {
                crc = table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }

            return crc;
        }
    }
}
