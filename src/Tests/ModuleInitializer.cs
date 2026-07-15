public static class ModuleInitializer
{
    #region enable

    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDocNet.Initialize();
        // 0.95 tolerates cross-OS pdfium PNG rendering (default is 0.98).
        VerifierSettings.UseSsimForPng(0.95);
    }

    #endregion

    [ModuleInitializer]
    public static void InitializeOther() =>
        VerifierSettings.InitializePlugins();
}
