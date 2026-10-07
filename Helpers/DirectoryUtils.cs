namespace Markerator.Helpers;

/// <summary>
/// This class contains helper utility functions to help deal with the file/directory based operations. 
/// </summary>
public static class DirectoryUtils
{
    private static void DeleteOutputDirectorsIfExists()
    {
        Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output"))
            .IfTrue(() => Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "output"), true));
    }

    /// <summary>
    /// This function creates the directories/subfolders that end up in the output/ folder that will eventually
    /// contain the generated files.
    /// </summary>
    public static void CreateOutputDirectories()
    {
        // TODO: Spit out a message about what the actual proper directory structure for input should look like.
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output"));

        Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "input", "images"))
            .IfTrue(() =>
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", "images"));
                CopyDirectory(
                    sourceDirectory: Path.Combine(Directory.GetCurrentDirectory(), "input", "images"),
                    targetDirectory: Path.Combine(Directory.GetCurrentDirectory(), "output", "images")
                );
            });
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
