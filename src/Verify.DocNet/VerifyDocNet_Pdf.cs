namespace VerifyTests;

public static partial class VerifyDocNet
{
    static ConversionResult Convert(string? name, Stream stream, IReadOnlyDictionary<string, object> settings)
    {
        var dimensions = settings.GetPageDimensions(new(scalingFactor: 2));
        using var reader = DocLib.Instance.GetDocReader(stream.ToBytes(), dimensions);

        return Convert(name, reader, settings);
    }

    static ConversionResult Convert(string? name, IDocReader document, IReadOnlyDictionary<string, object> settings)
    {
        var targets = GetStreams(name, document, settings).ToList();
        return new(null, targets);
    }

    static NaiveTransparencyRemover transparencyRemover = new();

    static IEnumerable<Target> GetStreams(string? name, IDocReader document, IReadOnlyDictionary<string, object> settings)
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
        for (var index = start; index < pagesToInclude; index++)
        {
            using var reader = document.GetPageReader(index);

            var rawBytes = preserveTransparency ?
                reader.GetImage() :
                reader.GetImage(transparencyRemover);

            var width = reader.GetPageWidth();
            var height = reader.GetPageHeight();

            var image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);

            var stream = new MemoryStream();
            image.SaveAsPng(stream);
            yield return new("png", stream, name);
        }
    }
}