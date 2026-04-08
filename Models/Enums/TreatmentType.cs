namespace HydroGrow.Models.Enums;

public enum TreatmentType
{
    WaterTopUp,
    NutrientChange,
    Fertilization,
    RootRinse,
    ContainerClean,
    Repot,
    Pruning,
    Other
}

public static class TreatmentTypeExtensions
{
    public static string ToDisplayString(this TreatmentType type) => type switch
    {
        TreatmentType.WaterTopUp => "Dolanie wody",
        TreatmentType.NutrientChange => "Wymiana pożywki",
        TreatmentType.Fertilization => "Nawożenie",
        TreatmentType.RootRinse => "Płukanie korzeni",
        TreatmentType.ContainerClean => "Czyszczenie pojemnika",
        TreatmentType.Repot => "Przesadzenie",
        TreatmentType.Pruning => "Przycinanie",
        TreatmentType.Other => "Inne",
        _ => type.ToString()
    };

    public static string ToIcon(this TreatmentType type) => type switch
    {
        TreatmentType.WaterTopUp => "\uf4d4",
        TreatmentType.NutrientChange => "\uf13e",
        TreatmentType.Fertilization => "\uf493",
        TreatmentType.RootRinse => "\uf4d3",
        TreatmentType.ContainerClean => "\uf18f",
        TreatmentType.Repot => "\uf493",
        TreatmentType.Pruning => "\uf178",
        TreatmentType.Other => "\uf4a3",
        _ => "\uf4a3"
    };

    public static IReadOnlyList<TreatmentType> All() =>
        Enum.GetValues<TreatmentType>().ToList();
}
