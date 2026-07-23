namespace VerifyTests;

public static partial class VerifyDocNet
{
    public static bool Initialized { get; private set; }

    public static void Initialize()
    {
        if (Initialized)
        {
            throw new("Already Initialized");
        }

        Initialized = true;

        InnerVerifier.ThrowIfVerifyHasBeenRun();
        VerifierSettings.RegisterStreamConverter("pdf", Convert);
        VerifierSettings.RegisterFileConverter<IDocReader>((target, context) => Convert(null, target, context));
    }

    public static void PagesToInclude(this VerifySettings settings, int count)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "PagesToInclude count must be greater than or equal to 1.");
        }

        settings.Context["VerifyDocNetPagesToInclude"] = count;
    }

    public static SettingsTask PagesToInclude(this SettingsTask settings, int count)
    {
        settings.CurrentSettings.PagesToInclude(count);
        return settings;
    }

    static int GetPagesToInclude(this IReadOnlyDictionary<string, object> settings, int count)
    {
        if (settings.TryGetValue("VerifyDocNetPagesToInclude", out var value))
        {
            return Math.Min(count, (int)value);
        }

        return count;
    }

    public static void SinglePage(this VerifySettings settings, int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "SinglePage index must be greater than or equal to 0.");
        }

        settings.Context["VerifyDocNetSinglePage"] = index;
    }

    /// <summary>
    /// Zero based index of single page to include (overrules PagesToInclude when in range)
    /// </summary>
    public static SettingsTask SinglePage(this SettingsTask settings, int index)
    {
        settings.CurrentSettings.SinglePage(index);
        return settings;
    }

    static bool TryGetSinglePage(this IReadOnlyDictionary<string, object> settings, out int singlePage)
    {
        if (settings.TryGetValue("VerifyDocNetSinglePage", out var value))
        {
            singlePage = (int)value;
            return true;
        }

        singlePage = 0;
        return false;
    }

    public static void PageDimensions(this VerifySettings settings, PageDimensions pageDimensions) =>
        settings.Context["VerifyDocNetPageDimensions"] = pageDimensions;

    public static SettingsTask PageDimensions(this SettingsTask settings, PageDimensions pageDimensions)
    {
        settings.CurrentSettings.PageDimensions(pageDimensions);
        return settings;
    }

    static PageDimensions GetPageDimensions(this IReadOnlyDictionary<string, object> settings, PageDimensions pageDimensions)
    {
        if (settings.TryGetValue("VerifyDocNetPageDimensions", out var value))
        {
            return (PageDimensions)value;
        }

        return pageDimensions;
    }

    public static void PreserveTransparency(this VerifySettings settings) =>
        settings.Context["VerifyDocNetPreserveTransparency"] = true;

    public static SettingsTask PreserveTransparency(this SettingsTask settings)
    {
        settings.CurrentSettings.PreserveTransparency();
        return settings;
    }

    static bool GetPreserveTransparency(this IReadOnlyDictionary<string, object> settings)
    {
        if (settings.TryGetValue("VerifyDocNetPreserveTransparency", out var value))
        {
            return (bool)value;
        }

        return false;
    }

    /// <summary>
    /// Snapshots the pdf bytes exactly as produced, skipping the normalization that neutralizes the
    /// trailer <c>/ID</c>, the <c>/CreationDate</c> and <c>/ModDate</c>, and the XMP dates and
    /// identifiers. Use it when the producer already emits byte-deterministic documents, since
    /// normalizing them again copies the whole buffer, rescans it, and — when the XMP packet is
    /// canonicalized — rebuilds it and repairs the cross-reference table, all to change nothing.
    /// </summary>
    /// <remarks>
    /// Only skip this when the producer is genuinely deterministic. Without it a freshly generated
    /// pdf carries a wall-clock <c>/CreationDate</c> and a fresh <c>/ID</c>, so the snapshot differs
    /// on every run.
    /// <para>
    /// The XMP canonicalization is worth calling out because it is the pass that changes bytes for
    /// an already-deterministic producer: it collapses the packet's whitespace, so enabling or
    /// disabling this setting on an existing suite shifts the stored <c>.verified.pdf</c> even
    /// though nothing about the document changed. Expect to re-accept those snapshots once.
    /// </para>
    /// </remarks>
    public static void SkipPdfNormalization(this VerifySettings settings) =>
        settings.Context["VerifyDocNetSkipNormalization"] = true;

    /// <inheritdoc cref="SkipPdfNormalization(VerifySettings)"/>
    public static SettingsTask SkipPdfNormalization(this SettingsTask settings)
    {
        settings.CurrentSettings.SkipPdfNormalization();
        return settings;
    }

    static bool GetNormalize(this IReadOnlyDictionary<string, object> settings) =>
        !settings.TryGetValue("VerifyDocNetSkipNormalization", out var value) ||
        value is not true;
}