﻿using System.Runtime.CompilerServices;
using VerifyTests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDocNet.Initialize();
        VerifyImageMagick.RegisterComparers(threshold: 0.05, ImageMagick.ErrorMetric.PerceptualHash);
    }
}