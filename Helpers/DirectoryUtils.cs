namespace Markerator.Helpers;

public static class DirectoryUtils
{
    private static void DeleteOutputDirectorsIfExists()
    {
        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output")))
        {
            Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "output"), true);
        }
    }

    public static void CreateOutputDirectories()
    {
        // TODO: Spit out a message about what the actual proper directory structure for input should look like.
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output"));

        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "input", "images")))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", "images"));
            CopyDirectory(
                sourceDirectory: Path.Combine(Directory.GetCurrentDirectory(), "input", "images"),
                targetDirectory: Path.Combine(Directory.GetCurrentDirectory(), "output", "images")
            );
        }

        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "input", "fonts")))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", "fonts"));
            CopyDirectory(
                sourceDirectory: Path.Combine(Directory.GetCurrentDirectory(), "input", "fonts"),
                targetDirectory: Path.Combine(Directory.GetCurrentDirectory(), "output", "fonts")
            );
        }
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        var diSource = new DirectoryInfo(sourceDirectory);
        var diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget);
    }

    private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        foreach (FileInfo fi in source.GetFiles())
        {
            Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }
}