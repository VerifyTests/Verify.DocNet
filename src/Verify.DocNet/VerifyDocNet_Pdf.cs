namespace VerifyTests;

public static partial class VerifyDocNet
{
    static ConversionResult Convert(string? name, Stream stream, IReadOnlyDictionary<string, object> settings)
    {
        var bytes = stream.ToBytes();
        var dimensions = settings.GetPageDimensions(new(scalingFactor: 2));

        List<Target> targets;
        PdfInfo info;
        using (var reader = DocLib.Instance.GetDocReader(bytes, dimensions))
        {
            (targets, info) = Render(name, reader, settings);
        }

        // Neutralize the volatile fields for the pdf snapshot only once the reader, which reads
        // lazily from the same buffer, has been released.
        PdfNormalizer.Normalize(bytes);
        targets.Add(
            new("pdf", new MemoryStream(bytes), "pdf")
            {
                BypassComparersForSubsequentOnDifference = true
            });

        return new(info, targets);
    }

    static ConversionResult Convert(string? name, IDocReader document, IReadOnlyDictionary<string, object> settings)
    {
        var (targets, info) = Render(name, document, settings);
        return new(info, targets);
    }

    static NaiveTransparencyRemover transparencyRemover = new();

    static (List<Target> targets, PdfInfo info) Render(string? name, IDocReader document, IReadOnlyDictionary<string, object> settings)
    {
        var numberOfPages = document.GetPageCount();
        var pagesToInclude = settings.GetPagesToInclude(numberOfPages);

        var start = 0;
        if (settings.TryGetSinglePage(out var singlePage))
        {
            if (singlePage >= numberOfPages)
            {
                throw new ($"Cannot Verify Page {singlePage} (0-based index) document contains only {numberOfPages} Page(s).");
            }

            start = singlePage;
            pagesToInclude = singlePage + 1;
        }

        var preserveTransparency = settings.GetPreserveTransparency();
        var targets = new List<Target>();
        var pages = new List<PageInfo>();
        for (var index = start; index < pagesToInclude; index++)
        {
            using var reader = document.GetPageReader(index);

            var rawBytes = preserveTransparency ?
                reader.GetImage() :
                reader.GetImage(transparencyRemover);

            var width = reader.GetPageWidth();
            var height = reader.GetPageHeight();

            var stream = new MemoryStream();
            PngEncoder.WriteBgraAsPng(rawBytes, width, height, stream);
            targets.Add(new("png", stream, name));

            pages.Add(new() { Text = reader.GetText() });
        }

        var info = new PdfInfo
        {
            Version = document.GetPdfVersion().ToString(),
            PageCount = numberOfPages,
            Pages = pages
        };
        return (targets, info);
    }
}