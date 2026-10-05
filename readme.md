# <img src="/src/icon.png" height="30px"> Verify.DocNet

[![Discussions](https://img.shields.io/badge/Verify-Discussions-yellow?svg=true&label=)](https://github.com/orgs/VerifyTests/discussions)
[![Build status](https://github.com/VerifyTests/Verify.DocNet/actions/workflows/build.yml/badge.svg)](https://github.com/VerifyTests/Verify.DocNet/actions/workflows/build.yml)
[![NuGet Status](https://img.shields.io/nuget/v/Verify.DocNet.svg)](https://www.nuget.org/packages/Verify.DocNet/)

Extends [Verify](https://github.com/VerifyTests/Verify) to allow verification of documents via [DocNet](https://github.com/GowenGit/docnet).<!-- singleLineInclude: intro. path: /docs/intro.include.md -->

Verifying a `pdf` produces:

 * A `.verified.txt` with the pdf version, the page count, and the extracted text of each page.
 * The pdf itself as `.verified.pdf`. This can be omitted with [`ExcludeTargets`](#choosing-what-is-verified).
 * A png render of every page as `#page_0001.verified.png`, `#page_0002.verified.png`, etc.

The page files are named, and the text placed, by Verify's [paged documents](https://github.com/VerifyTests/Verify/blob/main/docs/paged-documents.md) support, which every Verify plugin that splits a document into pages shares. So do the settings that [choose what is verified](#choosing-what-is-verified).

**See [Milestones](../../milestones?state=closed) for release notes.**


## Sponsors


### Entity Framework Extensions<!-- include: sponsors. path: /docs/sponsors.include.md -->

[Entity Framework Extensions](https://entityframework-extensions.net/?utm_source=simoncropp&utm_medium=Verify.DocNet) is a major sponsor and is proud to contribute to the development this project.

[![Entity Framework Extensions](https://raw.githubusercontent.com/VerifyTests/Verify.DocNet/refs/heads/main/docs/zzz.png)](https://entityframework-extensions.net/?utm_source=simoncropp&utm_medium=Verify.DocNet)

### Developed using JetBrains IDEs

[![JetBrains logo.](https://raw.githubusercontent.com/VerifyTests/Verify.DocNet/main/docs/jetbrains.png)](https://jb.gg/OpenSourceSupport)<!-- endInclude -->


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
    // 0.95 tolerates cross-OS pdfium PNG rendering (default is 0.98).
    VerifierSettings.UseSsimForPng(0.95);
}
```
<sup><a href='/src/Tests/ModuleInitializer.cs#L3-L13' title='Snippet source file'>snippet source</a> | <a href='#snippet-enable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`VerifyImageMagick.RegisterComparers` (provided by https://github.com/VerifyTests/Verify.ImageMagick) allows minor image changes to be ignored.


### Choosing what is verified

What a pdf is split into is controlled by Verify's settings for [paged documents](https://github.com/VerifyTests/Verify/blob/main/docs/paged-documents.md). Anything left out is not produced at all (pages are not rendered, text is not extracted), so these also save work.

`PagesToInclude` limits the pages that are rendered and read, to the first pages of a document or to those a delegate accepts. Page numbers are 1 based. The pdf itself is still verified whole:

<!-- snippet: PagesToInclude -->
<a id='snippet-PagesToInclude'></a>
```cs
[Test]
public Task VerifyFirstPage()
{
    var stream = File.OpenRead("sample.pdf");
    return Verify(stream, "pdf")
        .PagesToInclude(1);
}

[Test]
public Task VerifySecondPage()
{
    var stream = File.OpenRead("sample.pdf");
    return Verify(stream, "pdf")
        .PagesToInclude(_ => _ == 2);
}
```
<sup><a href='/src/Tests/Samples.cs#L49-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-PagesToInclude' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The text of each page is in the info file by default. `PageText` moves it to a `#page_0001.verified.txt` per page, or leaves it out with `PageTextPlacement.None`. `ExcludeDerivedTargets("png")` leaves out the rendered pages:

<!-- snippet: PageTextPerPage -->
<a id='snippet-PageTextPerPage'></a>
```cs
[Test]
public Task PageTextPerPage() =>
    VerifyFile("sample.pdf")
        .PageText(PageTextPlacement.PerPage)
        .ExcludeDerivedTargets("png");
```
<sup><a href='/src/Tests/Samples.cs#L69-L77' title='Snippet source file'>snippet source</a> | <a href='#snippet-PageTextPerPage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ExcludeTargets("pdf")` leaves out the pdf itself, for a document that is large or whose bytes cannot be made deterministic. With the rendered pages left out as well, only the info file is verified:

<!-- snippet: InfoOnly -->
<a id='snippet-InfoOnly'></a>
```cs
[Test]
public Task InfoOnly() =>
    VerifyFile("sample.pdf")
        .ExcludeTargets("pdf")
        .ExcludeDerivedTargets("png");
```
<sup><a href='/src/Tests/Samples.cs#L79-L87' title='Snippet source file'>snippet source</a> | <a href='#snippet-InfoOnly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Each can also be set for every test, on `VerifierSettings`:

<!-- snippet: InitializeOutputs -->
<a id='snippet-InitializeOutputs'></a>
```cs
[ModuleInitializer]
public static void Initialize()
{
    VerifyDocNet.Initialize();

    // For every test: no page images, so only the pdf and its text are verified
    VerifierSettings.ExcludeDerivedTargets("png");
}
```
<sup><a href='/src/StaticSettingsTests/ModuleInitializer.cs#L3-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-InitializeOutputs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Verify a file

<!-- snippet: VerifyPdf -->
<a id='snippet-VerifyPdf'></a>
```cs
[Test]
public Task VerifyPdf() =>
    VerifyFile("sample.pdf");
```
<sup><a href='/src/Tests/Samples.cs#L3-L9' title='Snippet source file'>snippet source</a> | <a href='#snippet-VerifyPdf' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Tests/Samples.cs#L38-L47' title='Snippet source file'>snippet source</a> | <a href='#snippet-VerifyPdfStream' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Result

[Samples.VerifyPdf#page_0001.verified.png](/src/Tests/Samples.VerifyPdf%23page_0001.verified.png):

<img src="/src/Tests/Samples.VerifyPdf%23page_0001.verified.png" width="200px">


## PreserveTransparency

<!-- snippet: PreserveTransparency -->
<a id='snippet-PreserveTransparency'></a>
```cs
[Test]
public Task VerifyPreserveTransparency() =>
    VerifyFile("sample.pdf")
        .PreserveTransparency();
```
<sup><a href='/src/Tests/Samples.cs#L20-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-PreserveTransparency' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Tests/Samples.cs#L29-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-PageDimensions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Reviewing changes

A change to a pdf is a change to several files: the pdf, its info file, and every page. Verify tells the diff tool that the pages and the info file were derived from the pdf, and [DiffEngineViewer](https://github.com/VerifyTests/DiffEngine/blob/main/docs/viewer.md#files-derived-from-a-document), which draws a pdf's pages itself, shows them as one row and accepts them together. Other diff tools are given each file, as before.

When the pdf has changed, its pages are compared exactly, skipping any comparer registered for png, such as the one `UseSsimForPng` registers. A comparer exists to tolerate differences, and a changed pdf is the one case where its pages should not be given the benefit of the doubt.


## Migrating from 4.x

Version 5 moves to the paged document support in Verify 33.3. The settings and the file names this package had of its own are replaced by the ones every paged document shares.

| 4.x | 5.x |
| --- | --- |
| `Initialize(DocNetOutputs.Png)` | `VerifierSettings.PageText(PageTextPlacement.None)` |
| `Initialize(DocNetOutputs.Text)` | `VerifierSettings.ExcludeDerivedTargets("png")` |
| `Initialize(DocNetOutputs.None)` | Both of the above |
| `.PagesToInclude(2)` | `.PagesToInclude(2)`. The same call, now a setting of Verify |
| `.SinglePage(0)` | `.PagesToInclude(_ => _ == 1)`. The index was 0 based, a page number is 1 based |

Each of the settings of Verify can be applied to one verification or, on `VerifierSettings`, to every test.

`SinglePage` threw for an index past the last page. A `PagesToInclude` that accepts no page is not an error: no page is rendered or read, and the pdf and the info file are still verified.

The snapshot files are renamed:

| 4.x | 5.x |
| --- | --- |
| `Tests.Report#pdf.verified.pdf` | `Tests.Report.verified.pdf` |
| `Tests.Report#00.verified.png` | `Tests.Report#page_0001.verified.png` |
| `Tests.Report#01.verified.png` | `Tests.Report#page_0002.verified.png` |
| `Tests.Report.verified.png`, where one page was verified | `Tests.Report#page_0001.verified.png`, by the number of that page |

The index was 0 based, and was missing where one page was verified. A page is now always named by its 1 based number, which it keeps when other pages are left out.

The content of the pdf and of the page images is unchanged, with one exception: under `PagesToInclude` or `SinglePage` the `.verified.pdf` held only the pages that were rendered, and it is now always the whole document.

Renamed snapshots show as a new file and a pending delete. Accepting both, or running once with [AutoVerify](https://github.com/VerifyTests/Verify/blob/main/docs/autoverify.md), moves a test over. An accepted page image is the one rendered on the machine doing the accepting. Where a comparer has been tolerating a difference in rendering, that image is not byte identical to the committed one, and renaming the files instead leaves them as they are.

The info file keeps its name and has the shape every paged document has: the pdf version under `Document`, and each page as its `Number` and its `Text`.

```
{                                    {
  Version: 1.4,                        Document: {
  PageCount: 2,                          Version: 1.4
  Pages: [                             },
    {                                  PageCount: 2,
      Text: The first page             Pages: [
    },                                   {
    {                                      Number: 1,
      Index: 1,                            Text: The first page
      Text: The second page              },
    }                                    {
  ]                                        Number: 2,
}                                          Text: The second page
                                         }
                                       ]
                                     }
```


## File Samples

http://file-examples.com/


## Icon

[Pdf](https://thenounproject.com/term/pdf/533502/) designed by [Alfredo](https://thenounproject.com/AlfredoCreates) from [The Noun Project](https://thenounproject.com/).
