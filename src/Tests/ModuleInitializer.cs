public static class ModuleInitializer
{
    #region enable

    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDocNet.Initialize();
        VerifierSettings.UseSsimForPng();
    }

    #endregion

    [ModuleInitializer]
    public static void InitializeOther() =>
        VerifyDiffPlex.Initialize();
}
