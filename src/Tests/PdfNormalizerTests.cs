public class PdfNormalizerTests
{
    [Test]
    public async Task NeutralizesVolatileValues()
    {
        var input =
            "/ID [<A1B2C3D4E5F60718> <1122334455667788>] " +
            "/CreationDate(D:20240115093000+05'30') " +
            "/ModDate(D:20240115093000Z) " +
            "<xmp:CreateDate>2024-01-15T09:30:00+05:30</xmp:CreateDate>" +
            "<xmp:ModifyDate>2024-01-15T09:30:00Z</xmp:ModifyDate>" +
            "<xmp:MetadataDate>2024-01-15T09:30:00Z</xmp:MetadataDate>" +
            "<xmpMM:DocumentID>uuid:0f7b2c9a-1234-5678-9abc-def012345678</xmpMM:DocumentID>" +
            "<xmpMM:InstanceID>xmp.iid:1a2b3c4d</xmpMM:InstanceID>";
        var expected =
            "/ID [<0000000000000000> <0000000000000000>] " +
            "/CreationDate(D:00000000000000+00'00') " +
            "/ModDate(D:00000000000000Z) " +
            "<xmp:CreateDate>0000-00-00T00:00:00+00:00</xmp:CreateDate>" +
            "<xmp:ModifyDate>0000-00-00T00:00:00Z</xmp:ModifyDate>" +
            "<xmp:MetadataDate>0000-00-00T00:00:00Z</xmp:MetadataDate>" +
            $"<xmpMM:DocumentID>{new string('0', 41)}</xmpMM:DocumentID>" +
            $"<xmpMM:InstanceID>{new string('0', 16)}</xmpMM:InstanceID>";
        await Assert.That(Normalize(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task NeutralizesDublinCoreDate()
    {
        // Some producers (for example older Apache FOP) write the render time straight into the
        // Dublin Core <dc:date> element as simple text content.
        var input = "<dc:date>2024-01-15T09:30:00+05:30</dc:date>";
        var expected = "<dc:date>0000-00-00T00:00:00+00:00</dc:date>";
        await Assert.That(Normalize(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task NeutralizesDublinCoreDateSeq()
    {
        // Per the XMP spec dc:date is an ordered array (seq Date), so a spec-compliant producer
        // (current Apache FOP) nests the render time in rdf:Seq/rdf:li rather than as direct text.
        var input = "<dc:date><rdf:Seq><rdf:li>2024-01-15T09:30:00+05:30</rdf:li></rdf:Seq></dc:date>";
        var expected = "<dc:date><rdf:Seq><rdf:li>0000-00-00T00:00:00+00:00</rdf:li></rdf:Seq></dc:date>";
        await Assert.That(Normalize(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task NeutralizesDublinCoreDateSeqWithWhitespaceAndMultipleEntries()
    {
        // Pretty-printed with indentation and more than one date in the sequence: markup and
        // whitespace are preserved while every date value is zeroed.
        var input =
            """
            <dc:date>
              <rdf:Seq>
                <rdf:li>2024-01-15T09:30:00+05:30</rdf:li>
                <rdf:li>2019-12-31T23:59:59Z</rdf:li>
              </rdf:Seq>
            </dc:date>
            """;
        var expected =
            """
            <dc:date>
              <rdf:Seq>
                <rdf:li>0000-00-00T00:00:00+00:00</rdf:li>
                <rdf:li>0000-00-00T00:00:00Z</rdf:li>
              </rdf:Seq>
            </dc:date>
            """;
        await Assert.That(Normalize(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task LeavesNonDateRdfArraysUntouched()
    {
        // The rdf:li descent is scoped to dc:date, so digits in a sibling array (here dc:subject)
        // must survive.
        var input =
            "<dc:subject><rdf:Bag><rdf:li>topic 2024</rdf:li></rdf:Bag></dc:subject>" +
            "<dc:date><rdf:Seq><rdf:li>2024-01-15T09:30:00Z</rdf:li></rdf:Seq></dc:date>";
        var expected =
            "<dc:subject><rdf:Bag><rdf:li>topic 2024</rdf:li></rdf:Bag></dc:subject>" +
            "<dc:date><rdf:Seq><rdf:li>0000-00-00T00:00:00Z</rdf:li></rdf:Seq></dc:date>";
        await Assert.That(Normalize(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task CollapsesDifferingValuesToTheSameOutput()
    {
        // The same producer emits a stable structure across runs, so two documents differing only
        // in the volatile digits/hex normalize to identical bytes.
        var a = "/ID [<A1B2C3D4>] /CreationDate(D:20240115093000+05'30')";
        var b = "/ID [<99887766>] /CreationDate(D:19991231235959+11'45')";
        await Assert.That(a).IsNotEqualTo(b);
        await Assert.That(Normalize(a)).IsEqualTo(Normalize(b));
    }

    [Test]
    public async Task LeavesLookalikeKeysUntouched()
    {
        // /IDTree is a name-tree key (not the file identifier), /ModDateStamp is a different name,
        // and a self-closing date element has no content: none should be altered.
        var input = "/IDTree [1 2] /ModDateStamp(20240101) <xmp:CreateDate/>2024";
        await Assert.That(Normalize(input)).IsEqualTo(input);
    }

    [Test]
    public async Task HandlesUnterminatedFileIdWithoutOverrunning()
    {
        // A truncated /ID whose string runs to the end of the buffer with no closing '>'/')' (and no
        // closing ']') must not scan past the buffer: the value is zeroed as far as it exists and the
        // scan stops cleanly rather than throwing. Hex string form (no closing '>'):
        await Assert.That(Normalize("/ID [<ABCD")).IsEqualTo("/ID [<0000");
        // Literal string form (no closing ')'):
        await Assert.That(Normalize("/ID [(ABCD")).IsEqualTo("/ID [(0000");
    }

    [Test]
    public async Task NormalizedDocumentStillLoads()
    {
        var data = File.ReadAllBytes("sample.pdf");
        PdfNormalizer.Normalize(data);

        using var reader = DocLib.Instance.GetDocReader(data, new(scalingFactor: 2));
        await Assert.That(reader.GetPageCount()).IsEqualTo(2);
    }

    [Test]
    public async Task NeutralizesFopStyleXmp()
    {
        // sample-fop.pdf carries an uncompressed FOP-style XMP packet whose dc:date render time is
        // nested in rdf:Seq/rdf:li. It must be neutralized while the document still loads.
        var data = File.ReadAllBytes("sample-fop.pdf");
        PdfNormalizer.Normalize(data);

        var text = Encoding.Latin1.GetString(data);
        await Assert.That(text).Contains("<rdf:li>0000-00-00T00:00:00+00:00</rdf:li>");
        await Assert.That(text).DoesNotContain("2024-01-15");

        using var reader = DocLib.Instance.GetDocReader(data, new(scalingFactor: 2));
        await Assert.That(reader.GetPageCount()).IsEqualTo(1);
    }

    [Test]
    public async Task IsIdempotent()
    {
        // A second pass has nothing left to change: normalizing already-normalized bytes is a no-op.
        var once = File.ReadAllBytes("sample.pdf");
        PdfNormalizer.Normalize(once);
        var twice = (byte[])once.Clone();
        PdfNormalizer.Normalize(twice);
        await Assert.That(twice).IsEquivalentTo(once);
    }

    [Test]
    public async Task NormalizedSinglePageSplitStillLoads()
    {
        // A page subset is re-serialized by pdfium (reintroducing volatile fields) then normalized;
        // it must remain a valid one-page document.
        var data = File.ReadAllBytes("sample.pdf");
        var split = DocLib.Instance.Split(data, 1, 1);
        PdfNormalizer.Normalize(split);

        using var reader = DocLib.Instance.GetDocReader(split, new(scalingFactor: 2));
        await Assert.That(reader.GetPageCount()).IsEqualTo(1);
    }

    static string Normalize(string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        PdfNormalizer.Normalize(bytes);
        return Encoding.Latin1.GetString(bytes);
    }
}
