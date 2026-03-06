using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CombasLauncherApp.Models;

/// <summary>
/// Represents a single map entry.
/// </summary>
public partial class MapEntry : ObservableObject
{

    [JsonPropertyName("paramRowId")]
    public int paramRowId { get; init; }

    [JsonPropertyName("enabled")] 
    [ObservableProperty]
    private bool _enabled;

    [JsonPropertyName("mapFolderPart1")]
    [ObservableProperty]
    private int _mapFolderNumPart1;


    [JsonPropertyName("mapFolderPart1")]
    [ObservableProperty]
    private int _mapFolderNumPart2;

    [JsonPropertyName("mapName")]
    public string? MapName { get; set; }

    [JsonPropertyName("timeOfDayHour")]
    public int TimeOfDayHour { get; set; }

    [JsonPropertyName("timeOfDayMinute")]
    public int TimeOfDayMinute { get; set; }

    public string? FreeBattleMapName { get; set; }

    public bool FreeBattleAvailable { get; set; }

    [JsonPropertyName("mapSizeX")]
    public int? MapSizeX { get; init; }

    [JsonPropertyName("mapSizeY")]
    public int? MapSizeY { get; init; }

    /// <summary>
    /// Returns the map size as "X x Y" or "N/A" if not available.
    /// </summary>
    [JsonIgnore]
    public string MapSizeDisplay =>
        MapSizeX.HasValue && MapSizeY.HasValue
            ? $"{MapSizeX.Value} x {MapSizeY.Value}"
            : (MapSizeX.HasValue ? $"{MapSizeX.Value}" : (MapSizeY.HasValue ? $"{MapSizeY.Value}" : "N/A"));

    /// <summary>
    /// Gets or sets the name of the folder that contains the map data.
    /// </summary>
    [JsonIgnore]
    public string MapFolderName => $"m{MapFolderNumPart1:D2}_{MapFolderNumPart2:D3}"; 
    
    [JsonIgnore]
    public string? TimeOfDay => $"{TimeOfDayHour:D2}:{TimeOfDayMinute:D2}";

    [JsonIgnore]
    public byte[]? MapBytes;
}