public class TextOnlyTests
{
    [Test]
    public Task VerifyPdf() =>
        VerifyFile(ProjectFiles.sample_pdf.Path);
}
