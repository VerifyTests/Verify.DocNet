namespace VerifyTests;

/// <summary>
/// What <c>Initialize</c> used to be given to choose the outputs a pdf is split into. Settings
/// of Verify choose that now, and nothing reads this: it is here so that code still naming it is
/// told what to use in its place.
/// </summary>
[Obsolete(
    "DocNetOutputs and the outputs argument of Initialize are replaced by settings of Verify: VerifierSettings.ExcludeDerivedTargets(\"png\") to leave out the page images and VerifierSettings.PageText(PageTextPlacement.None) to leave out the text. See https://github.com/VerifyTests/Verify.DocNet#migrating-from-4x",
    true)]
[Flags]
public enum DocNetOutputs
{
    /// <summary>No outputs. Only the source document and info are emitted.</summary>
    None = 0,

    /// <summary>Render each page to a png. When omitted, pages are not rendered.</summary>
    Png = 1,

    /// <summary>Extract the text of each page into the info. When omitted, text is not extracted.</summary>
    Text = 2,

    /// <summary>All outputs.</summary>
    All = Png | Text
}
