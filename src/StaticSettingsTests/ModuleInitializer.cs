public static class ModuleInitializer
{
    #region InitializeOutputs

    [ModuleInitializer]
    public static void Initialize() =>
        VerifyDocNet.Initialize(DocNetOutputs.Text);

    #endregion
}
