public class Samples
{
    #region VerifyPdf

    [Test]
    public Task VerifyPdf() =>
        VerifyFile("sample.pdf");

    #endregion

    #region SkipPdfNormalization

    [Test]
    public Task SkipPdfNormalization() =>
        VerifyFile("sample.pdf")
            .SkipPdfNormalization();

    #endregion

    #region PreserveTransparency

    [Test]
    public Task VerifyPreserveTransparency() =>
        VerifyFile("sample.pdf")
            .PreserveTransparency();

    #endregion

    #region PageDimensions

    [Test]
    public Task VerifyPageDimensions() =>
        VerifyFile("sample.pdf")
            .PageDimensions(new(1080, 1920));

    #endregion

    #region VerifyPdfStream

    [Test]
    public Task VerifyPdfStream()
    {
        var stream = File.OpenRead("sample.pdf");
        return Verify(stream, "pdf");
    }

    #endregion

    #region PagesToInclude

    [Test]
    public Task VerifyFirstPage()
    {
        var stream = File.OpenRead("sample.pdf");
        return Verify(stream, "pdf")
            .PagesToInclude(1);
    }

    [Test]
    public Task VerifySecondPage()
    {
        var stream = File.OpenRead("sample.pdf");
        return Verify(stream, "pdf")
            .PagesToInclude(_ => _ == 2);
    }

    #endregion

    #region PageTextPerPage

    [Test]
    public Task PageTextPerPage() =>
        VerifyFile("sample.pdf")
            .PageText(PageTextPlacement.PerPage)
            .ExcludeDerivedTargets("png");

    #endregion

    #region InfoOnly

    [Test]
    public Task InfoOnly() =>
        VerifyFile("sample.pdf")
            .ExcludeTargets("pdf")
            .ExcludeDerivedTargets("png");

    #endregion

    // With neither text nor pages, what is left is the pdf, its version and its page count
    [Test]
    public Task DocumentOnly() =>
        VerifyFile("sample.pdf")
            .PageText(PageTextPlacement.None)
            .ExcludeDerivedTargets("png");

    // A reader has no bytes to snapshot, so there is no pdf for its pages to be derived from
    [Test]
    public async Task VerifyDocReader()
    {
        var bytes = await File.ReadAllBytesAsync("sample.pdf");
        using var reader = DocLib.Instance.GetDocReader(bytes, new(scalingFactor: 2));
        await Verify(reader)
            .ExcludeDerivedTargets("png");
    }
}