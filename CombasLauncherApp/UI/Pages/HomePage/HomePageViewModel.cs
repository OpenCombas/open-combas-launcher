using CombasLauncherApp.Services;
using CombasLauncherApp.Services.Implementations;
using CombasLauncherApp.Services.Interfaces;
using CombasLauncherApp.UI.Windows.AuthEntry;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CombasLauncherApp.Enums;
using CombasLauncherApp.Utilities;
using Application = System.Windows.Application;

namespace CombasLauncherApp.UI.Pages.HomePage
{
    public partial class HomePageViewModel : ObservableObject
    {

        private static readonly ILoggingService _loggingService = ServiceProvider.GetService<ILoggingService>();

        private static readonly IMessageBoxService _messageBoxService = ServiceProvider.GetService<IMessageBoxService>();

        private static readonly IXeniaService _xeniaService = ServiceProvider.GetService<IXeniaService>();

        private static readonly ITailScaleService _tailScaleService = ServiceProvider.GetService<ITailScaleService>();

        [ObservableProperty]
        private bool _isInstallComplete;

        [ObservableProperty]
        private bool _chromeHoundsExtracted;

        [ObservableProperty]
        private bool _isXeniaFound;


        [ObservableProperty] private Dictionary<string, string> _mapPackList = new()
        {
            { "Default", "n/a" },
            { "API 1", "api1.opencombas.org" },
            { "API 2", "api2.opencombas.org" },
            { "API 3", "api3.opencombas.org" },
            { "API 4", "api4.opencombas.org" }
        };


        [ObservableProperty] 
        private KeyValuePair<string, string>? _selectedMapPack;
        partial void OnSelectedMapPackChanged(KeyValuePair<string, string>? value)
        {
            if (_selectedMapPackSuppressed)
            {
                return;
            }

            _ = SwitchMapPackAsync(value?.Key, value?.Value);
        }

        private readonly bool _selectedMapPackSuppressed;

        public HomePageViewModel()
        {
            ChromeHoundsExtracted = AppService.Instance.ChromeHoundsExtracted;
            IsInstallComplete = AppService.Instance.IsInstallComplete;
            IsXeniaFound = _xeniaService.XeniaFound;

            var mapPackKey = AppService.Instance.CurrentMapPack; // This is the key, e.g., "API 1"
            if (mapPackKey != null && _mapPackList.TryGetValue(mapPackKey, out var mapPackValue))
            {
                SelectedMapPack = new KeyValuePair<string, string>(mapPackKey, mapPackValue);
            }
            else
            {
                SelectedMapPack = _mapPackList.FirstOrDefault();
            }

            _selectedMapPackSuppressed = false;
        }

        [RelayCommand]
        private void LaunchXenia()
        {
            if (!_tailScaleService.IsTailScaleRunning())
            {
                _messageBoxService.ShowWarning("No TailScale instance is currently running. Please start TailScale and try again.");
                return;
            }


            _xeniaService.LunchXeniaProcess(AppService.ChromeHoundsDir);
        }

        [RelayCommand]
        private async Task RunInitialSetup()
        {
            try
            {
                AppService.Instance.IsLoading = true;

                var isoImportResult = await ImportChromeHoundsIso();
                if (isoImportResult != ImportChromeHoundsIsoResult.Success && isoImportResult != ImportChromeHoundsIsoResult.Skipped)
                {
                    return;
                }

                if (!ImportDefaultMapPack())
                {
                    return;
                }

                if (!MoveXeniaFilesToLocalAppData())
                {
                    return;
                }

                if (_tailScaleService.IsTailScaleRunning())
                {
                    _messageBoxService.ShowWarning("We have detected you are already using Tailscale, please use this .....[TODO ADD INSTRUCTION] "); //TODO: Add instructions for users who already have TailScale running, as we can skip the auth key step and the TailScale service.
                }
                else
                {

                    // Input auth key
                    var authKey = PromptForTailScaleAuthenticationKey();
                    if (string.IsNullOrWhiteSpace(authKey))
                    {
                        _loggingService.LogError("No auth key entered.");
                        _messageBoxService.ShowError("You must enter a Tailscale auth key to continue.");
                        return;
                    }

                    var tailScaleResult = await _tailScaleService.InstallTailScaleAsync(authKey);

                    switch (tailScaleResult)
                    {
                        case TailScaleInstallResult.Success:
                            break;

                        case TailScaleInstallResult.AuthenticationFailed:
                            _messageBoxService.ShowError("TailScale authenticate could not be run.");
                            return;

                        case TailScaleInstallResult.XeniaStartupFailed:
                            _messageBoxService.ShowError(
                                "TailScale installation failed as Xenia did not start correctly. Please make sure Xenia is able to launch and try again.");
                            return;

                        case TailScaleInstallResult.ExceptionThrown:
                            _messageBoxService.ShowError(
                                "TailScale installation failed with an exception. Please check the logs for more details.");
                            return;

                        default:
                            _messageBoxService.ShowError(
                                "TailScale installation failed with an unknown result. Please check the logs for more details.");
                            return;
                    }
                }

                IsInstallComplete = true;
                AppService.Instance.IsInstallComplete = true;
            }
            finally
            {
                AppService.Instance.IsLoading = false;
            }
        }

        private async Task SwitchMapPackAsync(string? mapPackName, string? mapPackUrl)
        {
            try
            {
                AppService.Instance.IsLoading = true;

                // Delete old api map pack directory
                var isDefault = false;
                if (string.IsNullOrEmpty(mapPackName) || string.IsNullOrEmpty(mapPackUrl))
                {
                    _messageBoxService.ShowError("Could not find MapPack name or Url");
                    return;
                }

                if (mapPackName == "Default")
                {
                    mapPackName = Path.Combine(mapPackName, "menu");
                    isDefault = true;
                }

                var apiMapPackDir = Path.Combine(AppService.MapPacksDir, mapPackName);

                //Delete existing pack and redownload only if not default.
                if (Directory.Exists(apiMapPackDir) && !isDefault)
                {
                    Directory.Delete(apiMapPackDir, true);
                }

                Directory.CreateDirectory(apiMapPackDir);

                // Download and copy files
                var files = new[] { "MenuMapInfo.bin", "MenuText_Eng.fmg" }; // File List

                foreach (var file in files)
                {
                    var destPath = Path.Combine(apiMapPackDir, file);

                    if (!isDefault)
                    {
                        // Download from map pack and save to apiMapPackDir
                        var url = $"https://{mapPackUrl}/xstorage/title/534507D4/{file}";

                        using var client = new System.Net.Http.HttpClient();
                        var data = await client.GetByteArrayAsync(url);
                        await File.WriteAllBytesAsync(destPath, data);
                    }

                    // Patch to Chromehounds menu directory
                    var menuDir = Path.Combine(AppService.ChromeHoundsDir, "menu");
                    var menuFilePath = Path.Combine(menuDir, file);

                    if (File.Exists(menuFilePath))
                    {
                        File.Delete(menuFilePath);
                    }

                    File.Copy(destPath, menuFilePath);
                }

                AppService.Instance.CurrentMapPack = mapPackName;


                if (!isDefault)
                {
                    if (!_xeniaService.UpdateApiAddress(mapPackUrl))
                    {
                        _loggingService.LogError($"Failed to switch api");
                        _messageBoxService.ShowError("Failed to switch api.");
                    }
                }
                else
                {
                    await Task.Delay(1000); // Add a short delay to keep the loading consistent as Default doesn't change the api
                    mapPackName = "Default";
                }

                _loggingService.LogInformation($"Map pack switched to {mapPackName}.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to switch map pack: {ex.Message}");
                _messageBoxService.ShowError("Failed to switch map pack.");
            }
            finally
            {
                AppService.Instance.IsLoading = false;

            }
        }



        private async Task<ImportChromeHoundsIsoResult> ImportChromeHoundsIso()
        {
            if (AppService.Instance.ChromeHoundsExtracted)
            {
                _loggingService.LogError("The ISO has already been imported. Are you sure you want to override this?");
                var reImport = _messageBoxService.ShowWarning(
                    "The ISO has already been imported. Are you sure you want to override this?",
                    MessageBoxButton.YesNo);
                if (reImport != MessageBoxResult.Yes)
                {
                    _loggingService.LogError("Iso was already extracted, skipping.");
                    return ImportChromeHoundsIsoResult.Skipped;
                }
            }

            string? selectedDir = null;
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description =
                    "Select the folder containing your Chromehounds ISO or extracted Chromehounds directory";
                dialog.SelectedPath = selectedDir;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedDir = dialog.SelectedPath;
                }
                else
                {
                    _loggingService.LogError("No folder selected.");
                    return ImportChromeHoundsIsoResult.IsoFolderNotFound;
                }
            }

            return await Task.Run(async () =>
            {
                try
                {
                    var isoFiles = Directory.GetFiles(selectedDir, "*.iso", SearchOption.AllDirectories);
                    var chromeHoundsDirs =
                        Directory.GetDirectories(selectedDir, "Chromehounds", SearchOption.AllDirectories);

                    int choice;
                    if (isoFiles.Length > 0 && chromeHoundsDirs.Length > 0)
                    {
                        // UI thread needed for message box
                        var result = await Application.Current.Dispatcher.InvokeAsync(() =>
                            _messageBoxService.Show(
                                "Both a Chromehounds folder and an ISO file were found. Use ISO file?", "Choose Source",
                                MessageBoxButton.YesNoCancel, MessageBoxImage.Question));
                        switch (result)
                        {
                            case MessageBoxResult.Yes:
                                choice = 0; // Use ISO
                                break;
                            case MessageBoxResult.No:
                                choice = 1; // Use folder
                                break;
                            default:
                                return ImportChromeHoundsIsoResult.Aborted;
                        }
                    }
                    else if (isoFiles.Length > 0)
                    {
                        choice = 0;
                    }
                    else if (chromeHoundsDirs.Length > 0)
                    {
                        choice = 1;
                    }
                    else
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _loggingService.LogError("No Chromehounds ISO or extracted folder found.");
                            _messageBoxService.ShowError("No Chromehounds ISO or extracted folder found in the selected directory.");
                        });
                        return ImportChromeHoundsIsoResult.IsoFolderNotFound;
                    }

                    var isoDestinationDir = Path.GetDirectoryName(AppService.ChromeHoundsDir);
                    var chromeHoundsDestDir = AppService.ChromeHoundsDir;

                    // Clean up any previous extraction
                    if (Directory.Exists(chromeHoundsDestDir))
                    {
                        Directory.Delete(chromeHoundsDestDir, true);
                    }

                    switch (choice)
                    {
                        case 0:
                        {
                            // Use ISO
                            Directory.CreateDirectory(isoDestinationDir);
                            var isoFile = isoFiles[0];
                            var destIsoPath = Path.Combine(isoDestinationDir, Path.GetFileName(isoFile));
                            File.Copy(isoFile, destIsoPath, true);
                            _loggingService.LogInformation($"Copied ISO to: \"{destIsoPath}\"");

                            // Extract ISO
                            _loggingService.LogInformation($"Extracting ISO: \"{destIsoPath}\"");
                            var extractProcess = Process.Start(new ProcessStartInfo
                            {
                                FileName = Path.Combine(AppService.ToolsDir, "extract-xiso.exe"),
                                Arguments = $"\"{destIsoPath}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                WorkingDirectory = AppService.BaseDir
                            });
                            if (extractProcess == null)
                            {
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    _loggingService.LogError(
                                        "Extraction of the ISO could not be run as the process returned null");
                                    _messageBoxService.ShowError("Extraction of the ISO Failed");
                                });
                                return ImportChromeHoundsIsoResult.IsoExtractionFailed;
                            }

                            await extractProcess.WaitForExitAsync();

                            // Move extracted folder to Chromehounds
                            var isoName = Path.GetFileNameWithoutExtension(destIsoPath);
                            var extractedDir = Path.Combine(AppService.BaseDir, isoName);
                            if (Directory.Exists(extractedDir))
                            {
                                Directory.Move(extractedDir, chromeHoundsDestDir);
                            }
                            else
                            {
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    _loggingService.LogError($"Extraction failed or folder not found: \"{isoName}\"");
                                    _messageBoxService.ShowError(
                                        $"Extraction failed or folder not found: \"{isoName}\"");
                                });
                                return ImportChromeHoundsIsoResult.IsoExtractionFailed;
                            }

                            break;
                        }
                        case 1:
                        {
                            // Use extracted Chromehounds folder
                            var sourceDir = chromeHoundsDirs[0];
                            Directory.CreateDirectory(isoDestinationDir);
                            FileUtils.CopyDirectory(sourceDir, chromeHoundsDestDir);
                            _loggingService.LogInformation($"Copied Chromehounds folder to: \"{chromeHoundsDestDir}\"");
                            break;
                        }
                    }

                    //Verify extraction (back to UI thread for property update)
                    var extracted =
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                            AppService.Instance.ChromeHoundsExtracted);

                    if (extracted) return ImportChromeHoundsIsoResult.Success;

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _loggingService.LogError("The ISO was either not correct or was not extracted correctly");
                        _messageBoxService.ShowWarning(
                            "The ISO was either not correct or was not extracted correctly, please make sure its a ChromeHounds Iso and try again.");
                    });
                    return ImportChromeHoundsIsoResult.IsoExtractionFailed;

                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _loggingService.LogError($"Exception during import: {ex.Message}");
                        _messageBoxService.ShowError("An error occurred during import.");
                    });
                    return ImportChromeHoundsIsoResult.ExceptionThrown;
                }
            });
        }

        private bool ImportDefaultMapPack()
        {
            try
            {
                // Copy folders from map_pack into Chromehounds directory
                var mapPackDir = Path.Combine(AppService.InternalMapPackPatchDir, "default_map_pack");
                _loggingService.LogInformation("Installing Map Pack");
                FileUtils.CopyDirectory(mapPackDir, AppService.ChromeHoundsDir);


                //Copy default map pack to MapPacks directory for reference
                var destDir = Path.Combine(AppService.MapPacksDir, "Default");
                Directory.CreateDirectory(destDir);
                FileUtils.CopyDirectory(mapPackDir, destDir);


                _loggingService.LogInformation("Default map-packs installation complete.");

                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to import default map-packs: {ex.Message}");
                _messageBoxService.ShowError("Failed to install default map-packs.");
                return false;
            }
        }

        private bool MoveXeniaFilesToLocalAppData()
        {
            // Check if Xenia is already in LocalAppData
            if (_xeniaService.XeniaFound)
            {
                _loggingService.LogInformation("Xenia is already set up in LocalAppData.");
                return true;
            }

            try
            {
                _loggingService.LogInformation("Moving Xenia files to LocalAppData...");
                // Ensure the destination directory exists
                Directory.CreateDirectory(AppService.XeniaDir);
                // Recursively copy all files and subdirectories
                FileUtils.CopyDirectory(AppService.InternalXeniaFilesDir, AppService.XeniaDir);

                _loggingService.LogInformation("Xenia files moved to LocalAppData successfully.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to move Xenia files to LocalAppData: {ex.Message}");
                _messageBoxService.ShowError("Failed to move Xenia files to LocalAppData.");
                return false;
            }

            _xeniaService.UpdateXeniaPath();

            return true;
        }

        private string? PromptForTailScaleAuthenticationKey()
        {
            var window = new AuthKeyWindow
            {
                Owner = Application.Current.MainWindow
            };
            return window.ShowDialog() == true ? window.AuthKey : null;
        }

    }
}
