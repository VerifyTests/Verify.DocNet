public class PdfSnapshotTests
{
    // The SinglePage snapshots are subset to only the rendered page, so the accepted pdf must load
    // as a one-page document. A readable guard alongside the opaque binary verified files.
    [Arguments("Samples.VerifyFirstPage#pdf.verified.pdf", 1)]
    [Arguments("Samples.VerifySecondPage#pdf.verified.pdf", 1)]
    [Arguments("Samples.VerifyPdf#pdf.verified.pdf", 2)]
    [Test]
    public async Task SnapshotHasExpectedPageCount(string file, int expectedPages)
    {
        var data = File.ReadAllBytes(Path.Combine(SourceDirectory(), file));
        using var reader = DocLib.Instance.GetDocReader(data, new(scalingFactor: 2));
        await Assert.That(reader.GetPageCount()).IsEqualTo(expectedPages);
    }

    static string SourceDirectory([CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
