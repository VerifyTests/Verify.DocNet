public static class ModuleInitializer
{
    #region InitializeOutputs

    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDocNet.Initialize();

        // For every test: no page images, so only the pdf and its text are verified
        VerifierSettings.ExcludeDerivedTargets("png");
    }

    #endregion
}
