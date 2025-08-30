# <img src="/src/icon.png" height="30px"> Verify.DocNet

[![Discussions](https://img.shields.io/badge/Verify-Discussions-yellow?svg=true&label=)](https://github.com/orgs/VerifyTests/discussions)
[![Build status](https://ci.appveyor.com/api/projects/status/41y3vomprwgnsheq?svg=true)](https://ci.appveyor.com/project/SimonCropp/Verify-DocNet)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.DocNet.svg)](https://www.nuget.org/packages/Verify.DocNet/)

Extends [Verify](https://github.com/VerifyTests/Verify) to allow verification of documents via [DocNet](https://github.com/GowenGit/docnet).<!-- singleLineInclude: intro. path: /docs/intro.include.md -->


**See [Milestones](../../milestones?state=closed) for release notes.**
Converts pdf documents to png for verification.

This library uses [SixLabors ImageSharp](https://github.com/SixLabors/ImageSharp) for png generation. For commercial application support visit [SixLabors/Pricing](https://sixlabors.com/pricing/).


## Sponsors


### Entity Framework Extensions<!-- include: zzz. path: /docs/zzz.include.md -->

[Entity Framework Extensions](https://entityframework-extensions.net/?utm_source=simoncropp&utm_medium=Verify.DocNet) is a major sponsor and is proud to contribute to the development this project.

[![Entity Framework Extensions](https://raw.githubusercontent.com/VerifyTests/Verify.DocNet/refs/heads/main/docs/zzz.png)](https://entityframework-extensions.net/?utm_source=simoncropp&utm_medium=Verify.DocNet)<!-- endInclude -->


## NuGet

 * https://nuget.org/packages/Verify.DocNet


## Usage


### Enable Verify.DocNet

<!-- snippet: enable -->
<a id='snippet-enable'></a>
```cs
[ModuleInitializer]
public static void Initialize()
{
    VerifyDocNet.Initialize();
    VerifyImageMagick.RegisterComparers(
        threshold: 0.13,
        ImageMagick.ErrorMetric.PerceptualHash);
}
```
<sup><a href='/src/Tests/ModuleInitializer.cs#L3-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-enable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`VerifyImageMagick.RegisterComparers` (provided by https://github.com/VerifyTests/Verify.ImageMagick) allows minor image changes to be ignored.


### Verify a file

<!-- snippet: VerifyPdf -->
<a id='snippet-VerifyPdf'></a>
```cs
[Test]
public Task VerifyPdf() =>
    VerifyFile("sample.pdf");
```
<sup><a href='/src/Tests/Samples.cs#L4-L10' title='Snippet source file'>snippet source</a> | <a href='#snippet-VerifyPdf' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Verify a Stream

<!-- snippet: VerifyPdfStream -->
<a id='snippet-VerifyPdfStream'></a>
```cs
[Test]
public Task VerifyPdfStream()
{
    var stream = File.OpenRead("sample.pdf");
    return Verify(stream, "pdf");
}
```
<sup><a href='/src/Tests/Samples.cs#L30-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-VerifyPdfStream' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Result

[Samples.VerifyPdf#01.verified.png](/src/Tests/Samples.VerifyPdf%2300.verified.png):

<img src="/src/Tests/Samples.VerifyPdf%2300.verified.png" width="200px">


## PreserveTransparency

<!-- snippet: PreserveTransparency -->
<a id='snippet-PreserveTransparency'></a>
```cs
[Test]
public Task VerifyPreserveTransparency() =>
    VerifyFile("sample.pdf")
        .PreserveTransparency();
```
<sup><a href='/src/Tests/Samples.cs#L12-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-PreserveTransparency' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## PageDimensions

<!-- snippet: PageDimensions -->
<a id='snippet-PageDimensions'></a>
```cs
[Test]
public Task VerifyPageDimensions() =>
    VerifyFile("sample.pdf")
        .PageDimensions(new(1080, 1920));
```
<sup><a href='/src/Tests/Samples.cs#L21-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-PageDimensions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## VerifySinglePage

<!-- snippet: VerifySinglePage -->
<a id='snippet-VerifySinglePage'></a>
```cs
[Test]
public Task VerifyFirstPage()
{
    var stream = File.OpenRead("sample.pdf");
    return Verify(stream, "pdf").SinglePage(0);
}

[Test]
public Task VerifySecondPage()
{
    var stream = File.OpenRead("sample.pdf");
    return Verify(stream, "pdf").SinglePage(1);
}
```
<sup><a href='/src/Tests/Samples.cs#L41-L57' title='Snippet source file'>snippet source</a> | <a href='#snippet-VerifySinglePage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## File Samples

http://file-examples.com/


## Icon

[Pdf](https://thenounproject.com/term/pdf/533502/) designed by [Alfredo](https://thenounproject.com/AlfredoCreates) from [The Noun Project](https://thenounproject.com/).
