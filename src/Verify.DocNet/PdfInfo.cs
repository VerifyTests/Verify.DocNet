class PdfInfo
{
    public string Version { get; init; } = "";

    /// <summary>
    /// Total number of pages in the source document. This is the full count regardless of any
    /// <c>PagesToInclude</c>/<c>SinglePage</c> filter; the pages actually rendered are listed in
    /// <see cref="Pages"/>, each carrying its <see cref="PageInfo.Index"/> within the document.
    /// </summary>
    public int PageCount { get; init; }

    public IReadOnlyList<PageInfo> Pages { get; init; } = [];
}
