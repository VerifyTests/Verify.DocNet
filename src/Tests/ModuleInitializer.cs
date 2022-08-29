public static class ModuleInitializer
{
    #region enable

    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDocNet.Initialize();
        VerifyImageMagick.RegisterComparers(
            threshold: 0.13,
            ImageMagick.ErrorMetric.PerceptualHash);

        #endregion

        VerifyDiffPlex.Initialize();
    }
}