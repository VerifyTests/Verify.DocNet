[TestFixture]
public class PdfSnapshotTests
{
    // The SinglePage snapshots are subset to only the rendered page, so the accepted pdf must load
    // as a one-page document. A readable guard alongside the opaque binary verified files.
    [TestCase("Samples.VerifyFirstPage#pdf.verified.pdf", 1)]
    [TestCase("Samples.VerifySecondPage#pdf.verified.pdf", 1)]
    [TestCase("Samples.VerifyPdf#pdf.verified.pdf", 2)]
    public void SnapshotHasExpectedPageCount(string file, int expectedPages)
    {
        var data = File.ReadAllBytes(Path.Combine(SourceDirectory(), file));
        using var reader = DocLib.Instance.GetDocReader(data, new(scalingFactor: 2));
        Assert.That(reader.GetPageCount(), Is.EqualTo(expectedPages));
    }

    static string SourceDirectory([CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
