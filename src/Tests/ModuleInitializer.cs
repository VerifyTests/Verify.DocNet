using VerifyTests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDocNet.Initialize();
        VerifyImageMagick.RegisterComparers(threshold: 0.13, ImageMagick.ErrorMetric.PerceptualHash);
    }
}