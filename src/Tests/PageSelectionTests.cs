[TestFixture]
public class PageSelectionTests
{
    [Test]
    public void SinglePageRejectsNegativeIndex()
    {
        // A negative index would render nothing and produce a confusing downstream error; reject it
        // at the call site instead. Zero (the first page) remains valid.
        var settings = new VerifySettings();
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.SinglePage(-1));
    }

    [Test]
    public void PagesToIncludeRejectsNonPositiveCount()
    {
        // Zero or fewer pages leaves nothing to verify and drives a negative page range into the
        // pdf subset; require at least one page.
        var settings = new VerifySettings();
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.PagesToInclude(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.PagesToInclude(-1));
    }

    [Test]
    public void AcceptsBoundaryValues()
    {
        // The lowest valid values must pass: page index 0 and a count of 1.
        var settings = new VerifySettings();
        Assert.DoesNotThrow(() => settings.SinglePage(0));
        Assert.DoesNotThrow(() => settings.PagesToInclude(1));
    }
}
