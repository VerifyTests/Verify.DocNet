public class TextOnlyTests
{
    [Test]
    public Task VerifyPdf() =>
        VerifyFile("sample.pdf");
}
