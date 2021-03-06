[TestFixture]
public class Samples
{
    #region VerifyPdf

    [Test]
    public Task VerifyPdf() =>
        VerifyFile("sample.pdf");

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
    public Task VerifyPdfStream() =>
        Verify(File.OpenRead("sample.pdf"))
            .UseExtension("pdf");

    #endregion
}