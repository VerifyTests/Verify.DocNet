public class PdfSnapshotTests
{
    // PagesToInclude limits the pages that are rendered, not the pdf, so the accepted pdf must load
    // as the whole document whichever pages were verified. A readable guard alongside the opaque
    // binary verified files.
    [Arguments("Samples.VerifyFirstPage.verified.pdf", 2)]
    [Arguments("Samples.VerifySecondPage.verified.pdf", 2)]
    [Arguments("Samples.VerifyPdf.verified.pdf", 2)]
    [Test]
    public async Task SnapshotHasExpectedPageCount(string file, int expectedPages)
    {
        var data = File.ReadAllBytes(Path.Combine(ProjectFiles.ProjectDirectory, file));
        using var reader = DocLib.Instance.GetDocReader(data, new(scalingFactor: 2));
        await Assert.That(reader.GetPageCount()).IsEqualTo(expectedPages);
    }
}
