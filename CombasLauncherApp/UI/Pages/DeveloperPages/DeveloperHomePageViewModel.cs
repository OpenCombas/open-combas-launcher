using System.Diagnostics;
using System.IO;
using CombasLauncherApp.Models;
using CombasLauncherApp.Services;
using CombasLauncherApp.Services.Implementations;
using CombasLauncherApp.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CombasLauncherApp.UI.Pages.DeveloperPages
{
    public partial class DeveloperHomePageViewModel : ObservableObject
    {

        private readonly INavigationService _navigationService = ServiceProvider.GetService<INavigationService>();
        private readonly IMessageBoxService _messageBoxService = ServiceProvider.GetService<IMessageBoxService>();

        private const int MaxEnabledMaps = 31;

        [ObservableProperty]
        private int _enabledMapCount;

        [ObservableProperty]
        private bool _reOrder;

        [ObservableProperty]
        private List<MapEntry> _maps = [];

        [ObservableProperty]
        private string? _mapConsoleOutput;

        public DeveloperHomePageViewModel()
        {
            ReadCurrentMapPack();
        }

        [RelayCommand]
        private void ReadCurrentMapPack()
        {
            var service = new MapConfigurationService();

            // Patch to Chromehounds menu directory
            var menuDir = Path.Combine(AppService.ChromeHoundsDir, "menu");
            var paramDir = Path.Combine(Environment.CurrentDirectory, "ParamDefinitions");
            var maps = service.ReadMapConfiguration(Path.Combine(menuDir, "MenuMapInfo.bin"), Path.Combine(paramDir, "MenuMapInfo.xml"), Path.Combine(menuDir, "MenuText_Eng.fmg"));

            foreach (var map in maps)
            {
                var status = map.Enabled ? "ENABLED " : "DISABLED";
                var size = $"Size: {map.MapSizeDisplay}";
                Debug.WriteLine($"  [{status}] {map.MapName}{size}");
                MapConsoleOutput = string.Join(Environment.NewLine, maps.Select(m => $"{m.MapName,-30} Enabled: {m.Enabled,-5} Size: {m.MapSizeDisplay}"
                ));
            }

            Maps = maps;

            UpdateEnableMapCount();
        }

        [RelayCommand]
        private void WriteCurrentMapPack()
        {
            var service = new MapConfigurationService();
            // Patch to Chromehounds menu directory
            var menuDir = Path.Combine(AppService.ChromeHoundsDir, "menu");
            var paramDir = Path.Combine(Environment.CurrentDirectory, "ParamDefinitions");

            var selectedMaps = Maps.OrderByDescending(m => m.Enabled).ToList();

            // Write changes back
            service.WriteMapConfiguration(Path.Combine(menuDir, "MenuMapInfo.bin"), Path.Combine(paramDir, "MenuMapInfo.xml"), Path.Combine(menuDir, "MenuText_Eng.fmg"), selectedMaps, createBackup: true, ReOrder);

        }

        [RelayCommand]
        private void ImportCurrentMapPack()
        {
            // Patch to Chromehounds menu directory
            var menuDir = Path.Combine(AppService.ChromeHoundsDir, "menu");
            var menuMapInfoPath = Path.Combine(menuDir, "MenuMapInfo.bin");
            var menuTextPath = Path.Combine(menuDir, "MenuText_Eng.fmg");

            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select folder containing MenuMapInfo.bin and MenuText_Eng.fmg";
            dialog.UseDescriptionForTitle = true;

            var result = dialog.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                try
                {
                    var sourceMapInfo = Path.Combine(dialog.SelectedPath, "MenuMapInfo.bin");
                    var sourceMenuText = Path.Combine(dialog.SelectedPath, "MenuText_Eng.fmg");

                    if (!File.Exists(sourceMapInfo) || !File.Exists(sourceMenuText))
                    {
                        _messageBoxService.ShowError("Selected folder does not contain both MenuMapInfo.bin and MenuText_Eng.fmg.");
                        return;
                    }

                    File.Copy(sourceMapInfo, menuMapInfoPath, overwrite: true);
                    File.Copy(sourceMenuText, menuTextPath, overwrite: true);

                    _messageBoxService.ShowInformation("Import successful.");
                    ReadCurrentMapPack(); // Refresh view
                }
                catch (Exception ex)
                {
                    _messageBoxService.ShowError($"Import failed: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private void ExportCurrentMapPack()
        {
            // Patch to Chromehounds menu directory
            var menuDir = Path.Combine(AppService.ChromeHoundsDir, "menu");
            var menuMapInfoPath = Path.Combine(menuDir, "MenuMapInfo.bin");
            var menuTextPath = Path.Combine(menuDir, "MenuText_Eng.fmg");

            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select export destination folder";
            dialog.UseDescriptionForTitle = true;

            var result = dialog.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                try
                {
                    var destMapInfo = Path.Combine(dialog.SelectedPath, "MenuMapInfo.bin");
                    var destMenuText = Path.Combine(dialog.SelectedPath, "MenuText_Eng.fmg");

                    File.Copy(menuMapInfoPath, destMapInfo, overwrite: true);
                    File.Copy(menuTextPath, destMenuText, overwrite: true);

                    _messageBoxService.ShowInformation("Export successful.");
                }
                catch (Exception ex)
                {
                    _messageBoxService.ShowError($"Export failed: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private void NavigateHome()
        {
            _navigationService.NavigateHome();
        }






        public void TryToggleMapEnabled(MapEntry map)
        {
            if (map.Enabled)
            {
                // Trying to enable
                if (Maps.Count(m => m.Enabled) > MaxEnabledMaps)
                { 
                    map.Enabled = !map.Enabled;
                    _messageBoxService.ShowInformation(
                        $"Max map count cannot exceed {MaxEnabledMaps}, please disable a map and try again.");
                }
            }

            UpdateEnableMapCount();
        }

        private void UpdateEnableMapCount()
        {
            EnabledMapCount = Maps.Count(m => m.Enabled);
        }
    }
}
