public class PageSelectionTests
{
    [Test]
    public async Task SinglePageRejectsNegativeIndex()
    {
        // A negative index would render nothing and produce a confusing downstream error; reject it
        // at the call site instead. Zero (the first page) remains valid.
        var settings = new VerifySettings();
        await Assert.That(() => settings.SinglePage(-1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task PagesToIncludeRejectsNonPositiveCount()
    {
        // Zero or fewer pages leaves nothing to verify and drives a negative page range into the
        // pdf subset; require at least one page.
        var settings = new VerifySettings();
        await Assert.That(() => settings.PagesToInclude(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => settings.PagesToInclude(-1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task AcceptsBoundaryValues()
    {
        // The lowest valid values must pass: page index 0 and a count of 1.
        var settings = new VerifySettings();
        await Assert.That(() => settings.SinglePage(0)).ThrowsNothing();
        await Assert.That(() => settings.PagesToInclude(1)).ThrowsNothing();
    }
}
