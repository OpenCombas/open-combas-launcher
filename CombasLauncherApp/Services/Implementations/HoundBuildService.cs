using System.IO;
using System.Windows.Media.Imaging;

namespace CombasLauncherApp.Services.Implementations
{
    public class BuildEntry(string name, string directoryPath, DateTime buildDate)
    {
        public string Name { get; set; } = name;

        public string DirectoryPath { get; set; } = directoryPath;

        public DateTime BuildDate { get; set; } = buildDate;
    }

    public interface IHoundBuildService
    {
        public BitmapImage GetFileImage(string filePath);

        public List<BuildEntry> GetLoadedBuildEntries(string mcdHeaderFolderPath, string mcdFolderPath);

        public List<BuildEntry> GetAllBuildEntries();
    }

    public class HoundBuildService
    {
        public List<BuildEntry> GetLoadedBuildEntries(string mcdHeaderFolderPath, string mcdFolderPath)
        {
            //Get the loaded build entries from the specified MCD folder path

            var buildEntries = new List<BuildEntry>();

            if (!Directory.Exists(mcdFolderPath))
            {
                return buildEntries;
            }

            var mcdDirs = Directory.GetDirectories(mcdFolderPath, "*.mcd", SearchOption.TopDirectoryOnly);

            foreach (var dir in mcdDirs)
            {
                var thumbnailPath = Path.Combine(dir, "__thumbnail.png");
                var fromSoftwarePath = Path.Combine(dir, "fromsoftware.txt");

                if (File.Exists(thumbnailPath) && File.Exists(fromSoftwarePath))
                {
                    var folderName = Path.GetFileName(dir);

                    // Example: Use folderName to look for header info in mcdHeaderFolderPath
                    // var headerPath = Path.Combine(mcdHeaderFolderPath, folderName, "header.txt");
                    // (You can add logic here to read header info if needed)

                    buildEntries.Add(new BuildEntry(
                        folderName,
                        dir,
                        Directory.GetLastWriteTime(dir)
                    ));
                }
            }

            return buildEntries;
        }
    
      
    }


    
}
