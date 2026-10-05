namespace VerifyTests;

public static partial class VerifyDocNet
{
    static ConversionResult Convert(Stream stream, IReadOnlyDictionary<string, object> settings)
    {
        var bytes = stream.ToBytes();
        var dimensions = settings.GetPageDimensions(new(scalingFactor: 2));

        var conversion = new PagedConversion(settings);
        using (var reader = DocLib.Instance.GetDocReader(bytes, dimensions))
        {
            AddPages(conversion, reader, settings);
        }

        // The pdf is the source the pages were derived from, so it is snapshotted whole even when
        // PagesToInclude limits which of its pages are rendered.
        if (!settings.IsTargetExcluded("pdf"))
        {
            if (settings.GetNormalize())
            {
                // Neutralize the volatile fields for the pdf snapshot. This must happen only after
                // the reader, which reads lazily from the same buffer, has been released.
                bytes = PdfNormalizer.Normalize(bytes);
            }

            conversion.Source(new("pdf", new MemoryStream(bytes)));
        }

        return conversion.Build();
    }

    // Registered for callers that supply an IDocReader directly. No pdf snapshot is produced here
    // since the original bytes are not available to normalize, so the pages have no source and
    // stand alone.
    static ConversionResult Convert(IDocReader document, IReadOnlyDictionary<string, object> settings)
    {
        var conversion = new PagedConversion(settings);
        AddPages(conversion, document, settings);
        return conversion.Build();
    }

    static NaiveTransparencyRemover transparencyRemover = new();

    // PagedConversion names the pages, places their text, and says which pages and which of their
    // outputs the verification wants, so a page that is not wanted is neither rendered nor read.
    static void AddPages(PagedConversion conversion, IDocReader document, IReadOnlyDictionary<string, object> settings)
    {
        conversion.Info = new PdfInfo
        {
            Version = document.GetPdfVersion().ToString()
        };

        var preserveTransparency = settings.GetPreserveTransparency();
        var includeImages = conversion.IncludeImages;
        var includeText = conversion.IncludeText;
        foreach (var number in conversion.Pages(document.GetPageCount()))
        {
            using var reader = document.GetPageReader(number - 1);

            string? text = null;
            if (includeText)
            {
                text = reader.GetText();
            }

            Stream? image = null;
            if (includeImages)
            {
                image = Render(reader, preserveTransparency);
            }

            conversion.AddPage(number, image, text);
        }
    }

    static MemoryStream Render(IPageReader reader, bool preserveTransparency)
    {
        var rawBytes = preserveTransparency ?
            reader.GetImage() :
            reader.GetImage(transparencyRemover);

        var width = reader.GetPageWidth();
        var height = reader.GetPageHeight();

        var stream = new MemoryStream();
        PngEncoder.WriteBgraAsPng(rawBytes, width, height, stream);
        return stream;
    }
}
