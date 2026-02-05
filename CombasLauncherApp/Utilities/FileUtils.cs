using System.IO;

namespace CombasLauncherApp.Utilities
{
    public static class FileUtils
    {
        // Utility to copy directories recursively
        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dir.Replace(sourceDir, targetDir));
            }
            foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(sourceDir, targetDir), true);
            }
        }
    }
}
