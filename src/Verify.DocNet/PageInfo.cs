class PageInfo
{
    /// <summary>Zero based index of the page within the source document.</summary>
    public int Index { get; init; }

    /// <summary>Text extracted from the page, or null when the page has no text.</summary>
    public string? Text { get; init; }
}
