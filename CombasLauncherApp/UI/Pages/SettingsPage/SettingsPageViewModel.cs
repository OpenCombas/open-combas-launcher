using CombasLauncherApp.Enums;
using CombasLauncherApp.Services;
using CombasLauncherApp.Services.Implementations;
using CombasLauncherApp.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CombasLauncherApp.Utilities;

namespace CombasLauncherApp.UI.Pages.SettingsPage
{
    public partial class SettingsPageViewModel :ObservableObject
    {
        private static readonly ILoggingService _loggingService = ServiceProvider.GetService<ILoggingService>();

        private static readonly IMessageBoxService _messageBoxService = ServiceProvider.GetService<IMessageBoxService>();

        private static readonly IXeniaService _xeniaService = ServiceProvider.GetService<IXeniaService>();

       
        [RelayCommand]
        private void ImportSaveData()
        {
            ImportGameSaveDataAsync();
        }

        [RelayCommand]
        private void ImportHoundPreBuilds()
        {
            ImportPrebuiltHounds();
        }

        [RelayCommand]
        private void ImportHoundBuilds()
        {
            ImportCustomHounds();
        }

        private void ImportGameSaveDataAsync()
        {
            string? selectedDir = null;
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the Xenia folder";
                dialog.SelectedPath = selectedDir;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedDir = dialog.SelectedPath;
                }
                else
                {
                    _loggingService.LogError("No folder selected.");
                    return;
                }
            }

            // Check if directory is named "xenia"
            var dirInfo = new DirectoryInfo(selectedDir);
            if (!dirInfo.Name.Equals("xenia", StringComparison.CurrentCultureIgnoreCase))
            {
                _messageBoxService.ShowError("The selected folder is not a valid Xenia folder.");
                return;
            }


            var result = _xeniaService.ImportGameData(selectedDir);

            switch (result)
            {
                case ImportGameDataResult.ExceptionThrown:
                    _messageBoxService.ShowError("Game Save Import Failed with an exception.");
                    break;
                case ImportGameDataResult.XeniaPathInvalid:
                    _messageBoxService.ShowError("Games Save Import Failed as the Destination Xenia Path is invalid.");
                    break;
                case ImportGameDataResult.SourceFolderNotFound:
                    _messageBoxService.ShowError("Game Save Import Failed as the Source Folder was not found.");
                    break;
                case ImportGameDataResult.GameDataFolderNotFound:
                    _messageBoxService.ShowError("Game Save Import Failed as the Game Data folder was not found.");
                    break;
                case ImportGameDataResult.Success:
                    _messageBoxService.ShowInformation("Game Save Imported Successfully");
                    break;
                default:
                    _messageBoxService.ShowError("Game Save Import Failed with an unknown error.");
                    break;
            }
        }

        private void ImportPrebuiltHounds()
        {
            try
            {
                // Set up source and destination paths relative to the application's base directory

                var destDir = Path.Combine(AppService.XeniaDir, "content", "B13EBABEBABEBABE", "534507D4", "00000001");

                _loggingService.LogInformation("Installing pre-built HOUNDs...");

                // Ensure the destination directory exists
                Directory.CreateDirectory(destDir);

                // Recursively copy all files and subdirectories
                FileUtils.CopyDirectory(AppService.HoundPreBuildsDir, destDir);

                _loggingService.LogInformation("Pre-built HOUNDs installation complete.");
                _messageBoxService.ShowInformation("Pre-built HOUNDs installation complete.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to install pre-built HOUNDs: {ex.Message}");
                _messageBoxService.ShowError("Failed to install pre-built HOUNDs.");
            }
        }

        private void ImportCustomHounds()
        {
            try
            {
                string? selectedDir = null;
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Select the folder containing your custom builds .mcd folders to import.";
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        selectedDir = dialog.SelectedPath;
                    }
                    else
                    {
                        _loggingService.LogError("No folder selected.");
                        return;
                    }
                }

                var destDir = Path.Combine(AppService.XeniaContentDir, "B13EBABEBABEBABE", "534507D4", "00000001");

                // Find all subdirectories ending with .mcd
                var mcdFolders = Directory.GetDirectories(selectedDir, "*.mcd", SearchOption.TopDirectoryOnly);

                if (mcdFolders.Length == 0)
                {
                    _loggingService.LogError("No .mcd folders found in the selected directory.");
                    _messageBoxService.ShowError("No .mcd folders found in the selected directory.");
                    return;
                }

                _loggingService.LogInformation("Importing custom HOUNDs (.mcd folders)...");

                // Ensure the destination directory exists
                Directory.CreateDirectory(destDir);

                // Copy each .mcd folder to the destination
                foreach (var mcdFolder in mcdFolders)
                {
                    var folderName = Path.GetFileName(mcdFolder);
                    var targetFolder = Path.Combine(destDir, folderName);
                    FileUtils.CopyDirectory(mcdFolder, targetFolder);
                    _loggingService.LogInformation($"Copied {folderName} to {targetFolder}");
                }

                _loggingService.LogInformation("Custom HOUNDs (.mcd folders) installation complete.");
                _messageBoxService.ShowInformation("Custom HOUNDs (.mcd folders) installation complete.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to install custom HOUNDs: {ex.Message}");
                _messageBoxService.ShowError("Failed to install custom HOUNDs.");
            }
        }
    }
}
