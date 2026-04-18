using System.Text.Json.Serialization;

namespace HydroGrow.Models;

public class PlantSpeciesEntry
{
    [JsonPropertyName("botanicalName")]
    public string BotanicalName { get; set; } = string.Empty;

    [JsonPropertyName("commonName")]
    public string CommonName { get; set; } = string.Empty;

    public string DisplayName => $"{BotanicalName} ({CommonName})";
}
