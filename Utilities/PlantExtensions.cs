namespace HydroGrow.Utilities;

public static class PlantExtensions
{
    public static bool IsNullOrNew(this Plant? plant) =>
        plant is null || plant.Id == 0;

    public static string GetMediumDisplayName(this Plant plant)
    {
        if (Enum.TryParse<MediumType>(plant.MediumType, out var type))
            return type.ToDisplayString();
        return plant.MediumType;
    }

    public static Color GetMediumColor(this Plant plant) =>
        plant.MediumType switch
        {
            nameof(MediumType.Hydro) => Color.FromArgb("#17A2B8"),
            nameof(MediumType.SemiHydro) => Color.FromArgb("#2ECC71"),
            nameof(MediumType.LECA) => Color.FromArgb("#F0A500"),
            nameof(MediumType.Perlite) => Color.FromArgb("#9B59B6"),
            nameof(MediumType.Water) => Color.FromArgb("#3498DB"),
            nameof(MediumType.Soil) => Color.FromArgb("#8B4513"),
            _ => Color.FromArgb("#95A5A6")
        };
}
