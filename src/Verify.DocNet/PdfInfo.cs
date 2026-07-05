class PdfInfo
{
    public string Version { get; init; } = "";
    public int PageCount { get; init; }
    public IReadOnlyList<PageInfo> Pages { get; init; } = [];
}
