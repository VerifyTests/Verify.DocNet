/// <summary>
/// Neutralizes the non-deterministic fields of a PDF (the trailer <c>/ID</c>, the document
/// information <c>/CreationDate</c> and <c>/ModDate</c>, and the equivalent XMP metadata dates and
/// identifiers) so that the same source document always produces byte-identical snapshot output.
/// </summary>
/// <remarks>
/// Neutralizing the values (dates and identifiers) is done in place and is length-preserving: only the
/// mutable characters inside each value are overwritten, so every cross-reference offset stays valid. A
/// value that has been compressed away (inside an <c>/ObjStm</c> object stream or a flate-compressed XMP
/// packet) no longer appears literally and is therefore left as-is.
/// <para>
/// The XMP packet is then canonicalized. Apache FOP serializes it through the platform's XML writer, so
/// the same document is indented differently depending on which JRE produced it; once the values are
/// zeroed, that whitespace is the only remaining cross-platform difference. It is collapsed to a single
/// canonical form. Because that changes the packet length, the metadata stream length and the classic
/// cross-reference table are repaired afterwards, which is why the method returns the resulting buffer.
/// A document that cannot be safely rewritten this way (a cross-reference stream, an incremental update,
/// or an unlocatable stream length) is returned unchanged.
/// </para>
/// <para>
/// Targets unencrypted documents. Encrypted PDFs seed their encryption key from the trailer
/// <c>/ID</c>; zeroing it would leave the document undecryptable, so encrypted input should not be
/// passed here.
/// </para>
/// </remarks>
static class PdfNormalizer
{
    enum Fill
    {
        // Zero the ASCII digits only, keeping separators (leaves a readable date).
        Digits,

        // Zero the hexadecimal digits (for hex string <...> values).
        Hex,

        // Zero every non-whitespace byte (for opaque identifiers).
        All
    }

    public static byte[] Normalize(byte[] data)
    {
        // Document information dictionary dates.
        ZeroPdfString(data, "/CreationDate"u8, Fill.Digits);
        ZeroPdfString(data, "/ModDate"u8, Fill.Digits);

        // Trailer / cross-reference-stream file identifier: /ID [<...> <...>].
        ZeroFileId(data);

        // XMP metadata dates (uncompressed metadata streams only).
        ZeroXmpElement(data, "<xmp:CreateDate"u8, Fill.Digits);
        ZeroXmpElement(data, "<xmp:ModifyDate"u8, Fill.Digits);
        ZeroXmpElement(data, "<xmp:MetadataDate"u8, Fill.Digits);

        // Dublin Core date. Unlike the xmp:* dates above it is an ordered array (seq Date), so the
        // value is nested inside rdf:Seq/rdf:li rather than being direct text content of the element
        // (this is what Apache FOP emits).
        ZeroXmpElementTree(data, "<dc:date"u8, "</dc:date>"u8, Fill.Digits);

        // XMP per-generation identifiers.
        ZeroXmpElement(data, "<xmpMM:DocumentID"u8, Fill.All);
        ZeroXmpElement(data, "<xmpMM:InstanceID"u8, Fill.All);
        ZeroXmpElement(data, "<xmpMM:OriginalDocumentID"u8, Fill.All);

        // Collapse the JRE-dependent XMP packet whitespace and repair the cross-reference table so the
        // snapshot is byte-identical across platforms. This can shrink the buffer, so the result of the
        // rewrite (a new buffer, or the same one when there is nothing to do) is returned to the caller.
        return CanonicalizeXmp(data);
    }

    // Finds a name key, then overwrites the string value that follows it. The value may be a
    // literal string "(...)" or a hex string "<...>".
    static void ZeroPdfString(byte[] data, ReadOnlySpan<byte> key, Fill fill)
    {
        var pos = 0;
        while (true)
        {
            var hit = data.AsSpan(pos).IndexOf(key);
            if (hit < 0)
            {
                return;
            }

            var i = pos + hit + key.Length;
            pos = i;

            i = SkipWhitespace(data, i);
            if (i >= data.Length)
            {
                return;
            }

            if (data[i] == (byte) '(')
            {
                var start = i + 1;
                var end = FindLiteralEnd(data, start);
                Overwrite(data, start, end, fill);
                pos = end;
            }
            else if (data[i] == (byte) '<' && (i + 1 >= data.Length || data[i + 1] != (byte) '<'))
            {
                var start = i + 1;
                var end = FindByte(data, start, (byte) '>');
                Overwrite(data, start, end, Fill.Hex);
                pos = end;
            }
        }
    }

    // Finds "/ID" followed by an array and zeroes each string element. Anything not shaped like the
    // identifier array (for example the "/IDTree" name-tree key) is skipped.
    static void ZeroFileId(byte[] data)
    {
        var key = "/ID"u8;
        var pos = 0;
        while (true)
        {
            var hit = data.AsSpan(pos).IndexOf(key);
            if (hit < 0)
            {
                return;
            }

            var i = pos + hit + key.Length;
            pos = i;

            i = SkipWhitespace(data, i);
            if (i >= data.Length || data[i] != (byte) '[')
            {
                continue;
            }

            i++;
            while (i < data.Length && data[i] != (byte) ']')
            {
                if (data[i] == (byte) '<')
                {
                    var start = i + 1;
                    i = FindByte(data, start, (byte) '>');
                    Overwrite(data, start, i, Fill.Hex);
                    if (i < data.Length)
                    {
                        i++;
                    }
                }
                else if (data[i] == (byte) '(')
                {
                    var start = i + 1;
                    i = FindLiteralEnd(data, start);
                    Overwrite(data, start, i, Fill.All);
                    if (i < data.Length)
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }

            pos = i;
        }
    }

    // Finds an XMP element by its opening tag and zeroes the text content up to the next '<'.
    static void ZeroXmpElement(byte[] data, ReadOnlySpan<byte> openTag, Fill fill)
    {
        var pos = 0;
        while (true)
        {
            var start = NextXmpElementContent(data, openTag, ref pos);
            if (start < 0)
            {
                return;
            }

            var end = FindByte(data, start, (byte) '<');
            Overwrite(data, start, end, fill);
            pos = end;
        }
    }

    // Like ZeroXmpElement, but descends through child markup and zeroes the content of every text
    // node up to the matching close tag. XMP array properties (for example dc:date, a "seq Date")
    // wrap their value in an rdf:Seq/rdf:li list, so the volatile value is not direct text content of
    // the named element and ZeroXmpElement alone would step over it.
    static void ZeroXmpElementTree(byte[] data, ReadOnlySpan<byte> openTag, ReadOnlySpan<byte> closeTag, Fill fill)
    {
        var pos = 0;
        while (true)
        {
            var start = NextXmpElementContent(data, openTag, ref pos);
            if (start < 0)
            {
                return;
            }

            var closeHit = data.AsSpan(start).IndexOf(closeTag);
            if (closeHit < 0)
            {
                return;
            }

            var end = start + closeHit;
            var i = start;
            while (i < end)
            {
                // Skip markup so only text nodes are altered, never element or attribute names.
                if (data[i] == (byte) '<')
                {
                    i = FindByte(data, i, (byte) '>');
                    if (i < end)
                    {
                        i++;
                    }

                    continue;
                }

                var textEnd = FindByte(data, i, (byte) '<');
                Overwrite(data, i, textEnd, fill);
                i = textEnd;
            }

            pos = end;
        }
    }

    // Locates the next element whose opening tag is 'openTag', returning the index of its content
    // (the byte after '>'), or -1 when no further match exists. A longer look-alike name or a
    // self-closing tag is skipped internally. 'pos' is advanced past the opening tag so scanning can
    // resume from the returned index.
    static int NextXmpElementContent(byte[] data, ReadOnlySpan<byte> openTag, ref int pos)
    {
        while (true)
        {
            var hit = data.AsSpan(pos).IndexOf(openTag);
            if (hit < 0)
            {
                return -1;
            }

            var i = pos + hit + openTag.Length;
            pos = i;

            // Reject a longer element name that merely shares this prefix.
            if (i < data.Length && data[i] != (byte) '>' && data[i] != (byte) '/' && !IsWhitespace(data[i]))
            {
                continue;
            }

            // Skip the remainder of the opening tag, remembering the last significant byte so a
            // self-closing "<tag/>" can be detected.
            var lastSignificant = (byte) 0;
            while (i < data.Length && data[i] != (byte) '>')
            {
                if (!IsWhitespace(data[i]))
                {
                    lastSignificant = data[i];
                }

                i++;
            }

            if (i >= data.Length)
            {
                return -1;
            }

            i++;
            pos = i;
            if (lastSignificant == (byte) '/')
            {
                continue;
            }

            return i;
        }
    }

    static void Overwrite(byte[] data, int start, int end, Fill fill)
    {
        for (var i = start; i < end; i++)
        {
            var c = data[i];
            var replace = fill switch
            {
                Fill.Digits => IsDigit(c),
                Fill.Hex => IsHexDigit(c),
                _ => !IsWhitespace(c)
            };
            if (replace)
            {
                data[i] = (byte) '0';
            }
        }
    }

    // Returns the index of the ')' that closes the literal string starting at 'start', honoring
    // backslash escapes and balanced parentheses, or the end of the buffer if unterminated.
    static int FindLiteralEnd(byte[] data, int start)
    {
        var depth = 1;
        var i = start;
        while (i < data.Length)
        {
            var c = data[i];
            if (c == (byte) '\\')
            {
                i += 2;
                continue;
            }

            if (c == (byte) '(')
            {
                depth++;
            }
            else if (c == (byte) ')')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }

            i++;
        }

        return data.Length;
    }

    static int FindByte(byte[] data, int start, byte target)
    {
        var i = start;
        while (i < data.Length && data[i] != target)
        {
            i++;
        }

        return i;
    }

    static int SkipWhitespace(byte[] data, int i)
    {
        while (i < data.Length && IsWhitespace(data[i]))
        {
            i++;
        }

        return i;
    }

    static bool IsDigit(byte b) =>
        b is >= (byte) '0' and <= (byte) '9';

    static bool IsHexDigit(byte b) =>
        b is >= (byte) '0' and <= (byte) '9' or >= (byte) 'a' and <= (byte) 'f' or >= (byte) 'A' and <= (byte) 'F';

    static bool IsWhitespace(byte b) =>
        b is (byte) ' ' or (byte) '\t' or (byte) '\r' or (byte) '\n' or (byte) '\f' or 0;

    // Collapses the whitespace of the single XMP metadata packet to a canonical form and repairs the
    // classic cross-reference table so the offsets stay valid. Returns the original array untouched when
    // there is nothing to canonicalize, or when the document is not a shape this can safely rewrite: no
    // packet, more than one packet, a cross-reference stream, more than one cross-reference section, or a
    // metadata stream whose length cannot be located.
    static byte[] CanonicalizeXmp(byte[] data)
    {
        var packetStart = IndexOf(data, "<?xpacket begin"u8, 0);
        if (packetStart < 0)
        {
            return data;
        }

        // Only a single packet is handled.
        if (IndexOf(data, "<?xpacket begin"u8, packetStart + 1) >= 0)
        {
            return data;
        }

        var endTag = IndexOf(data, "<?xpacket end"u8, packetStart);
        if (endTag < 0)
        {
            return data;
        }

        var closeMarker = IndexOf(data, "?>"u8, endTag);
        if (closeMarker < 0)
        {
            return data;
        }

        // The stream content runs from the packet up to endstream. The bytes between the packet's closing
        // marker and endstream are a platform-dependent end-of-line (Apache FOP emits it with the JRE's
        // line separator and counts it in the stream length), so they are folded into the region and
        // normalized too. They must be whitespace for the whole content region to be replaced wholesale.
        var packetEnd = closeMarker + 2;
        var contentEnd = IndexOf(data, "endstream"u8, packetEnd);
        if (contentEnd < 0 || !IsAllWhitespace(data, packetEnd, contentEnd))
        {
            return data;
        }

        // Canonical content: the packet with its inter-element whitespace collapsed, then a single
        // normalized end-of-line before endstream.
        var collapsed = CollapseInterTagWhitespace(data, packetStart, packetEnd);
        var canonical = new byte[collapsed.Length + 1];
        Array.Copy(collapsed, canonical, collapsed.Length);
        canonical[collapsed.Length] = (byte) '\n';

        if (canonical.Length == contentEnd - packetStart)
        {
            // Already canonical: leave the bytes (and the cross-reference table) untouched. This keeps
            // the pass idempotent and avoids rewriting documents that do not need it.
            return data;
        }

        if (!TryReadClassicXref(data, out var xrefKeyword, out var entries, out var startxref))
        {
            return data;
        }

        if (!TryFindStreamLength(data, packetStart, out var lengthField))
        {
            return data;
        }

        var packetEdit = (start: packetStart, end: contentEnd, replacement: canonical);
        var lengthEdit = (start: lengthField.start, end: lengthField.end, replacement: AsciiDigits(canonical.Length));

        var shiftPacket = packetEdit.replacement.Length - (packetEdit.end - packetEdit.start);
        var shiftLength = lengthEdit.replacement.Length - (lengthEdit.end - lengthEdit.start);

        // Maps an original byte position (never inside an edited region) to its position after both the
        // packet and length edits are applied.
        int Shift(int position) =>
            position +
            (position >= packetEdit.end ? shiftPacket : 0) +
            (position >= lengthEdit.end ? shiftLength : 0);

        // startxref points at the cross-reference keyword, which trails both edits, so it is repointed to
        // the shifted position. This third edit sits after the table and so does not move it.
        var startxrefEdit = (start: startxref.start, end: startxref.end, replacement: AsciiDigits(Shift(xrefKeyword)));

        var edits = new List<(int start, int end, byte[] replacement)> {packetEdit, lengthEdit, startxrefEdit};
        edits.Sort((left, right) => left.start.CompareTo(right.start));
        var rebuilt = ApplyEdits(data, edits);

        // Repair each in-use entry with the shifted offset of its object. The entry field and the object
        // it points at are both original positions, mapped through the same shift.
        foreach (var entry in entries)
        {
            if (entry.inUse)
            {
                WriteOffset(rebuilt, Shift(entry.fieldOffset), Shift(entry.objectOffset));
            }
        }

        return rebuilt;
    }

    // Drops every run of whitespace that sits between a '>' and a '<' (ignorable inter-element whitespace
    // and packet padding). Whitespace inside text content or attribute values is preserved.
    static byte[] CollapseInterTagWhitespace(byte[] data, int start, int end)
    {
        var output = new List<byte>(end - start);
        var index = start;
        while (index < end)
        {
            var current = data[index];
            if (IsWhitespace(current) && output.Count > 0 && output[^1] == (byte) '>')
            {
                var runEnd = index;
                while (runEnd < end && IsWhitespace(data[runEnd]))
                {
                    runEnd++;
                }

                if (runEnd < end && data[runEnd] == (byte) '<')
                {
                    index = runEnd;
                    continue;
                }
            }

            output.Add(current);
            index++;
        }

        return output.ToArray();
    }

    // Reads the sole classic cross-reference table: the keyword position, every entry (the object offset
    // it records and where that offset field lives), and the digit span of the sole startxref value.
    // Returns false for anything else (a cross-reference stream, an incremental update, or a malformed
    // table) so the caller can leave the document untouched.
    static bool TryReadClassicXref(
        byte[] data,
        out int xrefKeyword,
        out List<(int objectOffset, int fieldOffset, bool inUse)> entries,
        out (int start, int end) startxref)
    {
        xrefKeyword = -1;
        entries = [];
        startxref = default;

        // A second startxref implies an incremental update, which this does not rewrite.
        var startxrefKeyword = IndexOf(data, "startxref"u8, 0);
        if (startxrefKeyword < 0 ||
            IndexOf(data, "startxref"u8, startxrefKeyword + 1) >= 0)
        {
            return false;
        }

        if (!TryFindXrefTable(data, out xrefKeyword))
        {
            return false;
        }

        var position = SkipEol(data, xrefKeyword + 4);

        // Cross-reference subsections until the trailer keyword.
        while (true)
        {
            position = SkipWhitespace(data, position);
            if (StartsWith(data, position, "trailer"u8))
            {
                break;
            }

            // A subsection header is two integers ("first count") separated by a space.
            if (!TryReadInt(data, ref position, out _))
            {
                return false;
            }

            position = SkipWhitespace(data, position);
            if (!TryReadInt(data, ref position, out var count))
            {
                return false;
            }

            position = SkipEol(data, position);
            for (var index = 0; index < count; index++)
            {
                // A cross-reference entry is exactly 20 bytes: a 10-digit offset, a space, a 5-digit
                // generation, a space, the in-use/free type byte, and a two-byte end-of-line.
                if (position + 20 > data.Length)
                {
                    return false;
                }

                var offset = ParseFixedInt(data, position, 10);
                var type = data[position + 17];
                entries.Add((offset, position, type == (byte) 'n'));
                position += 20;
            }
        }

        var digits = SkipWhitespace(data, startxrefKeyword + "startxref"u8.Length);
        var digitsEnd = digits;
        while (digitsEnd < data.Length && IsDigit(data[digitsEnd]))
        {
            digitsEnd++;
        }

        if (digitsEnd == digits)
        {
            return false;
        }

        startxref = (digits, digitsEnd);
        return true;
    }

    // Finds the cross-reference table keyword: an "xref" that begins a line (which excludes the "xref"
    // inside "startxref"). Fails when there is not exactly one, so cross-reference streams and
    // incremental updates are rejected.
    static bool TryFindXrefTable(byte[] data, out int position)
    {
        position = -1;
        var search = 0;
        while (true)
        {
            var hit = IndexOf(data, "xref"u8, search);
            if (hit < 0)
            {
                return position >= 0;
            }

            search = hit + 4;
            if (hit == 0 || data[hit - 1] != (byte) '\r' && data[hit - 1] != (byte) '\n')
            {
                continue;
            }

            if (position >= 0)
            {
                position = -1;
                return false;
            }

            position = hit;
        }
    }

    // Locates the metadata stream length value, either the direct "/Length n" in the dictionary or, for
    // the indirect "/Length g 0 R" form, the numeric value of object g.
    static bool TryFindStreamLength(byte[] data, int packetStart, out (int start, int end) lengthField)
    {
        lengthField = default;

        // The metadata dictionary sits immediately before the packet, so its /Length is the nearest one.
        var key = LastIndexOf(data, "/Length"u8, packetStart);
        if (key < 0)
        {
            return false;
        }

        var digitsStart = SkipWhitespace(data, key + "/Length"u8.Length);
        var cursor = digitsStart;
        if (!TryReadInt(data, ref cursor, out var first))
        {
            return false;
        }

        // Indirect form "/Length g 0 R": the value lives in object g.
        var probe = SkipWhitespace(data, cursor);
        if (TryReadInt(data, ref probe, out var generation))
        {
            probe = SkipWhitespace(data, probe);
            if (probe < data.Length && data[probe] == (byte) 'R')
            {
                return TryFindIndirectLength(data, first, generation, out lengthField);
            }
        }

        // Direct form "/Length n".
        lengthField = (digitsStart, cursor);
        return true;
    }

    // Finds "objectNumber generation obj" at the start of a line and returns the span of the integer that
    // follows (the stream length held in an indirect object).
    static bool TryFindIndirectLength(byte[] data, int objectNumber, int generation, out (int start, int end) lengthField)
    {
        lengthField = default;

        var header = AsciiBytes($"{objectNumber} {generation} obj");
        var search = 0;
        while (true)
        {
            var hit = IndexOf(data, header, search);
            if (hit < 0)
            {
                return false;
            }

            search = hit + header.Length;
            if (hit != 0 && data[hit - 1] != (byte) '\r' && data[hit - 1] != (byte) '\n')
            {
                continue;
            }

            var digitsStart = SkipWhitespace(data, hit + header.Length);
            var cursor = digitsStart;
            if (!TryReadInt(data, ref cursor, out _))
            {
                return false;
            }

            lengthField = (digitsStart, cursor);
            return true;
        }
    }

    // Builds a new buffer with each edit (sorted, non-overlapping) spliced in.
    static byte[] ApplyEdits(byte[] data, List<(int start, int end, byte[] replacement)> edits)
    {
        var length = data.Length;
        foreach (var edit in edits)
        {
            length += edit.replacement.Length - (edit.end - edit.start);
        }

        var output = new byte[length];
        var read = 0;
        var write = 0;
        foreach (var edit in edits)
        {
            var copy = edit.start - read;
            Array.Copy(data, read, output, write, copy);
            write += copy;
            Array.Copy(edit.replacement, 0, output, write, edit.replacement.Length);
            write += edit.replacement.Length;
            read = edit.end;
        }

        Array.Copy(data, read, output, write, data.Length - read);
        return output;
    }

    // Overwrites a cross-reference entry's fixed 10-digit, zero-padded offset field.
    static void WriteOffset(byte[] data, int position, int offset)
    {
        for (var index = 9; index >= 0; index--)
        {
            data[position + index] = (byte) ('0' + offset % 10);
            offset /= 10;
        }
    }

    static int IndexOf(byte[] data, ReadOnlySpan<byte> value, int start)
    {
        var hit = data.AsSpan(start).IndexOf(value);
        return hit < 0 ? -1 : hit + start;
    }

    static int LastIndexOf(byte[] data, ReadOnlySpan<byte> value, int end) =>
        data.AsSpan(0, end).LastIndexOf(value);

    static bool StartsWith(byte[] data, int position, ReadOnlySpan<byte> value) =>
        position + value.Length <= data.Length &&
        data.AsSpan(position, value.Length).SequenceEqual(value);

    static bool IsAllWhitespace(byte[] data, int start, int end)
    {
        for (var index = start; index < end; index++)
        {
            if (!IsWhitespace(data[index]))
            {
                return false;
            }
        }

        return true;
    }

    static int SkipEol(byte[] data, int position)
    {
        if (position < data.Length && data[position] == (byte) '\r')
        {
            position++;
        }

        if (position < data.Length && data[position] == (byte) '\n')
        {
            position++;
        }

        return position;
    }

    static bool TryReadInt(byte[] data, ref int position, out int value)
    {
        value = 0;
        var start = position;
        while (position < data.Length && IsDigit(data[position]))
        {
            value = value * 10 + (data[position] - '0');
            position++;
        }

        return position > start;
    }

    static int ParseFixedInt(byte[] data, int position, int width)
    {
        var value = 0;
        for (var index = 0; index < width; index++)
        {
            value = value * 10 + (data[position + index] - '0');
        }

        return value;
    }

    static byte[] AsciiDigits(int value)
    {
        if (value == 0)
        {
            return [(byte) '0'];
        }

        var length = 0;
        for (var remaining = value; remaining > 0; remaining /= 10)
        {
            length++;
        }

        var bytes = new byte[length];
        for (var index = length - 1; index >= 0; index--)
        {
            bytes[index] = (byte) ('0' + value % 10);
            value /= 10;
        }

        return bytes;
    }

    static byte[] AsciiBytes(string value)
    {
        var bytes = new byte[value.Length];
        for (var index = 0; index < value.Length; index++)
        {
            bytes[index] = (byte) value[index];
        }

        return bytes;
    }
}
