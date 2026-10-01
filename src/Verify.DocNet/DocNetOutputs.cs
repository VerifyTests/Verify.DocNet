namespace VerifyTests;

/// <summary>
/// Controls which outputs a pdf is split into. Passed to <see cref="VerifyDocNet.Initialize"/>.
/// The source pdf snapshot is not controlled by this (use <c>VerifierSettings.ExcludeTargets("pdf")</c>).
/// </summary>
[Flags]
public enum DocNetOutputs
{
    /// <summary>Render each page to a png. When omitted, pages are not rendered.</summary>
    Png = 1,

    /// <summary>Extract the text of each page into the info. When omitted, text is not extracted.</summary>
    Text = 2,

    /// <summary>All outputs.</summary>
    All = Png | Text
}
