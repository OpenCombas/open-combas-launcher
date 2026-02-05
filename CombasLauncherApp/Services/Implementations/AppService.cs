using CombasLauncherApp.Services.Interfaces;
using System.IO;
using System.Text.Json;

namespace CombasLauncherApp.Services.Implementations;



public class AppService
{
    private readonly ILoggingService _loggingService = ServiceProvider.GetService<ILoggingService>();

    // Singleton implementation
    private static readonly Lazy<AppService> _instance = new(() => new AppService());
    public static AppService Instance => _instance.Value;

  
    
    public static string LocalAppData =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenCombasLauncher");

    private static string PersistentDataFile => Path.Combine(LocalAppData, "AppPersistenceData.json"); 
    
    public static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
    
    public static readonly string ChromeHoundsDir = Path.Combine(LocalAppData, "ISO", "Chromehounds");

    public static readonly string ChromeHoundsXex = Path.Combine(ChromeHoundsDir, "default.xex");

    public static readonly string InternalMapPackPatchDir = Path.Combine(BaseDir, "map_pack");

    public static readonly string MapPacksDir = Path.Combine(LocalAppData, "map_pack");

    public static readonly string ToolsDir = Path.Combine(BaseDir, "Tools");

    public static readonly string TailScaleExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tailscale", "tailscale.exe");

    public static readonly string InternalXeniaFilesDir = Path.Combine(BaseDir, "xenia");

    public static readonly string XeniaDir = Path.Combine(LocalAppData, "xenia");

    public static readonly string XeniaExe = Path.Combine(XeniaDir, "xenia_canary_netplay.exe");

    public static readonly string XeniaContentDir = Path.Combine(XeniaDir, "content");

    public static readonly string HoundPreBuildsDir = Path.Combine(BaseDir, "builds");

    public static readonly string HoundBuildsDir = Path.Combine(LocalAppData, "builds");


    public string CurrentVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

    public string? CurrentMapPack; 
    
    public bool ChromeHoundsExtracted => IsChromeHoundsExtracted();

    private bool _isInstallComplete;

    public bool IsInstallComplete
    {
        get=>_isInstallComplete;
        set
        {
            if (_isInstallComplete != value)
            {
                _isInstallComplete = value;
                OnIsInstallCompleteChanged?.Invoke(this, new IsInstallCompleteChangedEventArgs(_isInstallComplete));
            }
        }
    }
    
    // Event and event args for IsInstallComplete changes
    public event EventHandler<IsInstallCompleteChangedEventArgs>? OnIsInstallCompleteChanged;

    public class IsInstallCompleteChangedEventArgs(bool isInstallComplete) : EventArgs
    {
        public bool IsInstallComplete { get; } = isInstallComplete;
    }
    
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnIsLoadingChanged?.Invoke(this, new IsLoadingChangedEventArgs(_isLoading));
            }
        }
    }

    // Event and event args for IsLoading changes
    public event EventHandler<IsLoadingChangedEventArgs>? OnIsLoadingChanged;

    public class IsLoadingChangedEventArgs(bool isLoading) : EventArgs
    {
        public bool IsLoading { get; } = isLoading;
    }

   


    // Data model for persistent data
    private class PersistentAppData
    {
        public bool IsInstallComplete { get; set; }
        public string? CurrentMapPack { get; set; }
    }

    private PersistentAppData _persistentData = new();

    public int LoadPersistentAppData()
    {
        try
        {
            if (!Directory.Exists(LocalAppData))
            {
                Directory.CreateDirectory(LocalAppData);
                _persistentData = new PersistentAppData();
                if (SavePersistentAppData() != 0)
                {
                    return 1;
                }
            }
            else if (File.Exists(PersistentDataFile))
            {
                var json = File.ReadAllText(PersistentDataFile);
                _persistentData = JsonSerializer.Deserialize<PersistentAppData>(json) ?? new PersistentAppData();
                IsInstallComplete = _persistentData.IsInstallComplete;
                CurrentMapPack = _persistentData.CurrentMapPack;
            }
            else
            {
                _persistentData = new PersistentAppData();
                if (SavePersistentAppData() != 0)
                {
                    return 1;
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Failed to load or create persistent app data directory. {ex.Message}");
            return 1;
        }

    }

    public int SavePersistentAppData()
    {
        try
        {
            _persistentData.IsInstallComplete = IsInstallComplete;
            _persistentData.CurrentMapPack = CurrentMapPack;
            var json = JsonSerializer.Serialize(_persistentData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PersistentDataFile, json);
            return 0;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Failed to save persistent app data. {ex.Message}");
            return 1;
        }
    }
    

    private bool IsChromeHoundsExtracted()
    {
        if (!Directory.Exists(ChromeHoundsDir))
        {
            return false;
        }

        var xexPath = Path.Combine(ChromeHoundsDir, "default.xex");
        return File.Exists(xexPath);
    }
}