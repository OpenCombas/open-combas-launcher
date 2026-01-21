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

    [JsonPropertyName("mapName")]
    public string? MapName { get; set; }

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


    [JsonIgnore]
    public byte[]? MapBytes;
}