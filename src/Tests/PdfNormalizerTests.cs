// The neutralizing algorithm itself is owned and tested by the DeterministicPdf package. What is
// worth asserting here is the wiring: that this package applies it, and that a normalized document
// is still loadable by pdfium.
public class PdfNormalizerTests
{
    [Test]
    public async Task NormalizedDocumentStillLoads()
    {
        var data = PdfNormalizer.Normalize(await File.ReadAllBytesAsync("sample.pdf"));

        using var reader = DocLib.Instance.GetDocReader(data, new(scalingFactor: 2));
        await Assert.That(reader.GetPageCount()).IsEqualTo(2);
    }

    [Test]
    public async Task NeutralizesFopStyleXmp()
    {
        // sample-fop.pdf carries an uncompressed FOP-style XMP packet whose dc:date render time is
        // nested in rdf:Seq/rdf:li. It must be neutralized while the document still loads.
        var data = PdfNormalizer.Normalize(await File.ReadAllBytesAsync("sample-fop.pdf"));

        var text = Encoding.Latin1.GetString(data);
        await Assert.That(text).Contains("<rdf:li>0000-00-00T00:00:00+00:00</rdf:li>");
        await Assert.That(text).DoesNotContain("2024-01-15");

        using var reader = DocLib.Instance.GetDocReader(data, new(scalingFactor: 2));
        await Assert.That(reader.GetPageCount()).IsEqualTo(1);
    }

    [Test]
    public async Task CanonicalizesXmpAcrossJreSerializers()
    {
        // The same FOP document rendered on two machines. Apache FOP serializes the XMP packet through
        // the platform's XML writer, so the JDK decides the indentation: one build emits a compact
        // packet, the other indents every element, and the raw bytes differ. Once normalized, the pdf
        // snapshot must collapse to identical bytes on both, and still load.
        var compactRaw = await File.ReadAllBytesAsync("sample-fop-compact.pdf");
        var indentedRaw = await File.ReadAllBytesAsync("sample-fop-indented.pdf");
        await Assert.That(compactRaw.SequenceEqual(indentedRaw)).IsFalse();

        var compact = PdfNormalizer.Normalize(compactRaw);
        var indented = PdfNormalizer.Normalize(indentedRaw);
        await Assert.That(compact).IsEquivalentTo(indented);

        using var reader = DocLib.Instance.GetDocReader(compact, new(scalingFactor: 2));
        await Assert.That(reader.GetPageCount()).IsEqualTo(1);
    }

    [Test]
    public async Task NormalizedSinglePageSplitStillLoads()
    {
        // A page subset is re-serialized by pdfium (reintroducing volatile fields) then normalized;
        // it must remain a valid one-page document.
        var data = await File.ReadAllBytesAsync("sample.pdf");
        var split = DocLib.Instance.Split(data, 1, 1);
        split = PdfNormalizer.Normalize(split);

        using var reader = DocLib.Instance.GetDocReader(split, new(scalingFactor: 2));
        await Assert.That(reader.GetPageCount()).IsEqualTo(1);
    }
}
