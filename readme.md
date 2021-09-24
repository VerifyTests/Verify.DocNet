# <img src="/src/icon.png" height="30px"> Verify.DocNet

[![Build status](https://ci.appveyor.com/api/projects/status/41y3vomprwgnsheq?svg=true)](https://ci.appveyor.com/project/SimonCropp/Verify-DocNet)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.DocNet.svg)](https://www.nuget.org/packages/Verify.DocNet/)

Extends [Verify](https://github.com/VerifyTests/Verify) to allow verification of documents via [DocNet](https://github.com/GowenGit/docnet).

Converts documents pdf to png for verification.

<a href='https://dotnetfoundation.org' alt='Part of the .NET Foundation'><img src='https://raw.githubusercontent.com/VerifyTests/Verify/master/docs/dotNetFoundation.svg' height='30px'></a><br>
Part of the <a href='https://dotnetfoundation.org' alt=''>.NET Foundation</a>


## NuGet package

https://nuget.org/packages/Verify.DocNet/


## Usage


### Enable Verify.DocNet

<!-- snippet: ModuleInitializer.cs -->
<a id='snippet-ModuleInitializer.cs'></a>
```cs
using System.Runtime.CompilerServices;
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
```
<sup><a href='/src/Tests/ModuleInitializer.cs#L1-L12' title='Snippet source file'>snippet source</a> | <a href='#snippet-ModuleInitializer.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Verify a file

<!-- snippet: VerifyPdf -->
<a id='snippet-verifypdf'></a>
```cs
[Test]
public Task VerifyPdf()
{
    return Verifier.VerifyFile("sample.pdf")
        .PreserveTransparency()
        .PageDimensions(new(1080, 1920));
}
```
<sup><a href='/src/Tests/Samples.cs#L10-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-verifypdf' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Verify a Stream

<!-- snippet: VerifyPdfStream -->
<a id='snippet-verifypdfstream'></a>
```cs
[Test]
public Task VerifyPdfStream()
{
    return Verifier.Verify(File.OpenRead("sample.pdf"))
        .UseExtension("pdf")
        .PageDimensions(new(1080, 1920));
}
```
<sup><a href='/src/Tests/Samples.cs#L22-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-verifypdfstream' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Result

[Samples.VerifyPdf.01.verified.png](/src/Tests/Samples.VerifyPdf.00.verified.png):

<img src="/src/Tests/Samples.VerifyPdf.00.verified.png" width="200px">


## File Samples

http://file-examples.com/


## Icon

[Pdf](https://thenounproject.com/term/pdf/533502/) designed by [Alfredo](https://thenounproject.com/AlfredoCreates) from [The Noun Project](https://thenounproject.com/).
