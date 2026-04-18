using System.Text.Json;
using HydroGrow.Data;
using HydroGrow.Models;

namespace HydroGrow.Services;

public class PlantSpeciesService
{
    private readonly Task<IReadOnlyList<PlantSpeciesEntry>> _loadTask;

    public PlantSpeciesService()
    {
        _loadTask = LoadAsync();
    }

    public async Task<IReadOnlyList<PlantSpeciesEntry>> SearchAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return [];

        var all = await _loadTask;
        var words = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        return all
            .Where(e => words.All(w =>
                e.BotanicalName.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                e.CommonName.Contains(w, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(e => e.BotanicalName.StartsWith(words[0], StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(e => e.BotanicalName)
            .Take(8)
            .ToList();
    }

    private static async Task<IReadOnlyList<PlantSpeciesEntry>> LoadAsync()
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync("PlantSpecies.json");
        var list = await JsonSerializer.DeserializeAsync(stream,
            JsonContext.Default.ListPlantSpeciesEntry);
        return list ?? [];
    }
}
