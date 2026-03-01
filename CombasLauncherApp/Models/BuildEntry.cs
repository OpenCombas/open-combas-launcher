using System.Windows.Media.Imaging;

namespace CombasLauncherApp.Models;

public class BuildEntry(string name, string directoryPath, BitmapSource image, DateTime buildDate)
{
    public string Name { get; } = name;

    public string DirectoryPath { get; } = directoryPath;

    public BitmapSource Image { get; } = image;

    public DateTime BuildDate { get; } = buildDate;
}