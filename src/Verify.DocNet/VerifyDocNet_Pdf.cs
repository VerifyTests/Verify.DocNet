using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Converters;
using Docnet.Core.Readers;

namespace VerifyTests
{
    public static partial class VerifyDocNet
    {
        static ConversionResult ConvertPdf(Stream stream, IReadOnlyDictionary<string, object> settings)
        {
            IDocReader reader = DocLib.Instance.GetDocReader(stream.ToBytes(), new(1080, 1920));

            return ConvertPdf(reader, settings);
        }

        static ConversionResult ConvertPdf(IDocReader document, IReadOnlyDictionary<string, object> settings)
        {
            var targets = GetPdfStreams(document, settings).ToList();
            return new(null, targets);
        }

        static NaiveTransparencyRemover transparencyRemover = new();

        static IEnumerable<Target> GetPdfStreams(IDocReader document, IReadOnlyDictionary<string, object> settings)
        {
            var pagesToInclude = settings.GetPagesToInclude(document.GetPageCount());
            for (var index = 0; index < pagesToInclude; index++)
            {
                using var reader = document.GetPageReader(index);

                var rawBytes = reader.GetImage(transparencyRemover);

                var width = reader.GetPageWidth();
                var height = reader.GetPageHeight();

                using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                AddBytes(bmp, rawBytes);

                var stream = new MemoryStream();
                bmp.Save(stream, ImageFormat.Png);
                yield return new("png", stream);
            }
        }

        static void AddBytes(Bitmap bmp, byte[] rawBytes)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            var pNative = bmpData.Scan0;

            Marshal.Copy(rawBytes, 0, pNative, rawBytes.Length);
            bmp.UnlockBits(bmpData);
        }
    }
}