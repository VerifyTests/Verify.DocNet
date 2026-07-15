public class FopTests
{
    // Renders sample.fo to a PDF using a locally installed Apache FOP, then verifies the result.
    // FOP writes a volatile render time into the PDF (dc:date etc); this exercises PdfNormalizer
    // end-to-end and proves a FOP-produced PDF verifies deterministically across runs.
    // Returns inconclusive when FOP is not installed so the suite still passes on machines without it.
    [Test]
    public async Task VerifyFopRenderedPdf()
    {
        var fop = FindFop();
        if (fop == null)
        {
            Skip.Test("Apache FOP not found on PATH. Set the FOP_HOME environment variable or add fop to PATH.");
            return;
        }

        var output = Path.Combine(Path.GetTempPath(), $"verify-docnet-fop-{Guid.NewGuid():N}.pdf");
        try
        {
            RunFop(fop!, "sample.fo", output);
            await VerifyFile(output);
        }
        finally
        {
            File.Delete(output);
        }
    }

    static void RunFop(string fop, string input, string output)
    {
        var startInfo = new ProcessStartInfo(fop)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };
        startInfo.ArgumentList.Add("-fo");
        startInfo.ArgumentList.Add(input);
        startInfo.ArgumentList.Add("-pdf");
        startInfo.ArgumentList.Add(output);

        using var process = Process.Start(startInfo)!;
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new($"fop exited with code {process.ExitCode}:{Environment.NewLine}{error}");
        }
    }

    // Resolves the fop launcher: FOP_HOME first, then the platform specific names on PATH.
    static string? FindFop()
    {
        var names = OperatingSystem.IsWindows()
            ? ["fop.bat", "fop.cmd", "fop"]
            : new[] { "fop" };

        var home = Environment.GetEnvironmentVariable("FOP_HOME");
        if (home != null)
        {
            foreach (var name in names)
            {
                var candidate = Path.Combine(home, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        if (path == null)
        {
            return null;
        }

        foreach (var directory in path.Split(Path.PathSeparator))
        {
            if (directory.Length == 0)
            {
                continue;
            }

            foreach (var name in names)
            {
                var candidate = Path.Combine(directory, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
