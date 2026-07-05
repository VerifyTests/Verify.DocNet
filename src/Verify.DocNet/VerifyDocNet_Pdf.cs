namespace VerifyTests;

public static partial class VerifyDocNet
{
    static ConversionResult Convert(string? name, Stream stream, IReadOnlyDictionary<string, object> settings)
    {
        var bytes = stream.ToBytes();
        var dimensions = settings.GetPageDimensions(new(scalingFactor: 2));

        List<Target> targets;
        PdfInfo info;
        int start;
        int endExclusive;
        using (var reader = DocLib.Instance.GetDocReader(bytes, dimensions))
        {
            (targets, info, start, endExclusive) = Render(name, reader, settings);
        }

        // Subset the source to only the rendered pages so the pdf snapshot matches the png/info
        // pages. A full-document render reuses the original buffer; a filtered render is re-split
        // via pdfium into a fresh buffer.
        var coversAllPages = start == 0 && endExclusive == info.PageCount;
        var pdfBytes = coversAllPages ?
            bytes :
            DocLib.Instance.Split(bytes, start, endExclusive - 1);

        // Neutralize the volatile fields for the pdf snapshot. When the whole document is reused
        // this must happen only after the reader, which reads lazily from the same buffer, has been
        // released.
        PdfNormalizer.Normalize(pdfBytes);
        targets.Add(
            new("pdf", new MemoryStream(pdfBytes), "pdf")
            {
                BypassComparersForSubsequentOnDifference = true
            });

        return new(info, targets);
    }

    // Registered for callers that supply an IDocReader directly. No pdf snapshot is produced here
    // since the original bytes are not available to subset and normalize.
    static ConversionResult Convert(string? name, IDocReader document, IReadOnlyDictionary<string, object> settings)
    {
        var (targets, info, _, _) = Render(name, document, settings);
        return new(info, targets);
    }

    static NaiveTransparencyRemover transparencyRemover = new();

    static (List<Target> targets, PdfInfo info, int start, int endExclusive) Render(string? name, IDocReader document, IReadOnlyDictionary<string, object> settings)
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

            pages.Add(new() { Index = index, Text = reader.GetText() });
        }

        var info = new PdfInfo
        {
            Version = document.GetPdfVersion().ToString(),
            PageCount = numberOfPages,
            Pages = pages
        };
        return (targets, info, start, pagesToInclude);
    }
}
