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
        static ConversionResult Convert(Stream stream, IReadOnlyDictionary<string, object> settings)
        {
            var pageDimensions = settings.GetPageDimensions(new(scalingFactor: 2));
            IDocReader reader = DocLib.Instance.GetDocReader(stream.ToBytes(), pageDimensions);

            return Convert(reader, settings);
        }

        static ConversionResult Convert(IDocReader document, IReadOnlyDictionary<string, object> settings)
        {
            var targets = GetStreams(document, settings).ToList();
            return new(null, targets);
        }

        static readonly NaiveTransparencyRemover TransparencyRemover = new();

        static IEnumerable<Target> GetStreams(IDocReader document, IReadOnlyDictionary<string, object> settings)
        {
            var pagesToInclude = settings.GetPagesToInclude(document.GetPageCount());
            var preserveTransparency = settings.GetPreserveTransparency();
            for (var index = 0; index < pagesToInclude; index++)
            {
                using var reader = document.GetPageReader(index);

                var rawBytes = preserveTransparency ? reader.GetImage() : reader.GetImage(TransparencyRemover);

                var width = reader.GetPageWidth();
                var height = reader.GetPageHeight();

                using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                AddBytes(bitmap, rawBytes);

                var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
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