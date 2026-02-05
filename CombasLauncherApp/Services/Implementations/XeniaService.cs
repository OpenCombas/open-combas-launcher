using CombasLauncherApp.Enums;
using CombasLauncherApp.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace CombasLauncherApp.Services.Implementations
{
    public class XeniaService : IXeniaService
    {
        private readonly ILoggingService _loggingService = ServiceProvider.GetService<ILoggingService>();


        public bool XeniaFound { get; private set; }

        private Process? _xeniaProcess;


        public string XeniaPath { get; private set; }


        public void Initialise()
        {
            XeniaFound = GetXeniaPath();
        }

        private bool GetXeniaPath()
        {
            try
            {
                var xeniaFolder = Path.Combine(AppService.LocalAppData, "xenia");
                if (!Directory.Exists(xeniaFolder))
                {
                    throw new DirectoryNotFoundException($"The xenia folder was not found at: {xeniaFolder}");
                }

                var exeFiles = Directory.GetFiles(xeniaFolder, "*.exe", SearchOption.TopDirectoryOnly)
                    .Where(f => Path.GetFileName(f).ToLower().Contains("xenia"))
                    .ToList();

                if (exeFiles.Count > 1)
                {
                    throw new TargetParameterCountException($"There is more than one .exe that has xenia in the name. ");
                }

                if (exeFiles.Count == 0)
                {
                    XeniaPath = "Not Found";
                    return false;
                }

                XeniaPath = exeFiles.First();
                return true;
            }
            catch (Exception ex)
            {
                XeniaPath = "Not Found";
                _loggingService.LogError(ex.Message);
                return false;
            }
        }

        public void UpdateXeniaPath()
        {
            try
            {
                var xeniaFolder = Path.Combine(AppService.LocalAppData, "xenia");
                if (!Directory.Exists(xeniaFolder))
                {
                    throw new DirectoryNotFoundException($"The xenia folder was not found at: {xeniaFolder}");
                }

                var exeFiles = Directory.GetFiles(xeniaFolder, "*.exe", SearchOption.TopDirectoryOnly)
                    .Where(f => Path.GetFileName(f).ToLower().Contains("xenia"))
                    .ToList();

                if (exeFiles.Count > 1)
                {
                    throw new TargetParameterCountException($"There is more than one .exe that has xenia in the name. ");
                }

                if (exeFiles.Count == 0)
                {
                    XeniaPath = "Not Found";
                    XeniaFound = false;
                    return;
                }

                XeniaPath = exeFiles.First();
                XeniaFound = true;
            }
            catch (Exception ex)
            {
                XeniaPath = "Not Found";
                XeniaFound = false;
                _loggingService.LogError(ex.Message);
            }
        }

        public bool UpdateApiAddress(string newAddress)
        {
            try
            {
                var xeniaFolder = Path.GetDirectoryName(XeniaPath);

                if (xeniaFolder == null)
                {
                    return false;
                }

                var tomlPath = Path.Combine(xeniaFolder, "xenia-canary-netplay.config.toml");

                if (!File.Exists(tomlPath))
                {
                    _loggingService.LogError($"TOML file not found: {tomlPath}");
                    return false;
                }

                var lines = File.ReadAllLines(tomlPath);
                var updated = false;

                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var trimmed = line.TrimStart();

                    if (!trimmed.StartsWith("api_address ="))
                    {
                        continue;
                    }

                    // Find the start of the value
                    var hashIdx = line.IndexOf('#');
                    var comment = hashIdx >= 0 ? line[hashIdx..] : "";
                    var newLine = $"api_address = \"https://{newAddress}/\"";

                    if (!string.IsNullOrWhiteSpace(comment))
                    {
                        newLine += " \t" + comment;
                    }

                    lines[i] = newLine;
                    updated = true;
                    break;
                }

                if (updated)
                {
                    File.WriteAllLines(tomlPath, lines);
                    _loggingService.LogInformation($"api_address updated to: {newAddress}");
                    return true;
                }

                _loggingService.LogError("api_address parameter not found in TOML file.");

                return false;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error updating api_address: {ex.Message}");
                return false;
            }
        }
        
        public void LunchXeniaProcess(string chromeHoundsDirectory)
        {
            var xeniaFolder = Path.GetDirectoryName(XeniaPath);
            var xexPath = Path.Combine(chromeHoundsDirectory, "default.xex");
            if (!string.IsNullOrWhiteSpace(XeniaPath))
            {
                try
                {
                    _xeniaProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = XeniaPath,
                        Arguments = $"\"{xexPath}\"",
                        WorkingDirectory = xeniaFolder,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Failed to launch Xenia, {ex.Message}");
                }
            }
            else
            {
                _loggingService.LogError($"Xenia path is null or whitespace please check....");
            }
        }

      

        public ImportGameDataResult ImportGameData(string gameDataFolderPath)
        {
            if (!Directory.Exists(gameDataFolderPath))
            {
                _loggingService.LogError($"The selected game data folder: {gameDataFolderPath}, does not exist.");
                return ImportGameDataResult.GameDataFolderNotFound;
            }

            // Define the folder names to import
            var folderNames = new List<string> { "xstorage", "content" };

            foreach (var folderName in folderNames)
            {
                var sourceFolder = Path.Combine(gameDataFolderPath, folderName);
                if (!Directory.Exists(sourceFolder))
                {
                    _loggingService.LogError($"The selected source folder: {sourceFolder}, does not exist in the directory: {gameDataFolderPath}.");
                    return ImportGameDataResult.SourceFolderNotFound;
                }

                var xeniaDir = Directory.GetParent(XeniaPath)?.FullName;
                if (string.IsNullOrWhiteSpace(xeniaDir) || !Directory.Exists(xeniaDir))
                {
                    _loggingService.LogError($"The Xenia directory: {xeniaDir}, provided is missing or invalid.");
                    return ImportGameDataResult.XeniaPathInvalid;
                }

                var destinationFolder = Path.Combine(xeniaDir, folderName);

                try
                {
                    if (Directory.Exists(destinationFolder))
                    {
                        var backupFolder = destinationFolder + "_backup_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                        Directory.Move(destinationFolder, backupFolder);
                    }
                    else
                    {
                        Directory.CreateDirectory(destinationFolder);
                    }

                    foreach (var filePath in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = filePath[(sourceFolder.Length + 1)..];
                        var destinationFilePath = Path.Combine(destinationFolder, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);
                        File.Copy(filePath, destinationFilePath, overwrite: true);
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Exception occurred: {ex.Message}");
                    return ImportGameDataResult.ExceptionThrown;
                }
            }

            _loggingService.LogInformation("Importing game date was successful.");
            return ImportGameDataResult.Success;
        }
    }
}
