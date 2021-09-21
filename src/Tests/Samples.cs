using System.IO;
using System.Threading.Tasks;
using VerifyNUnit;
using NUnit.Framework;
using VerifyTests;

[TestFixture]
public class Samples
{
    #region VerifyPdf

    [Test]
    public Task VerifyPdf()
    {
        return Verifier.VerifyFile("sample.pdf")
            .PageDimensions(new(1080, 1920));
    }

    #endregion

    #region VerifyPdfStream

    [Test]
    public Task VerifyPdfStream()
    {
        return Verifier.Verify(File.OpenRead("sample.pdf"))
            .UseExtension("pdf")
            .PageDimensions(new(1080, 1920));
    }

    #endregion
}