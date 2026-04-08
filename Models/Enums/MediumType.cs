namespace HydroGrow.Models.Enums;

public enum MediumType
{
    Hydro,
    SemiHydro,
    LECA,
    Perlite,
    Water,
    Soil,
    Other
}

public static class MediumTypeExtensions
{
    public static string ToDisplayString(this MediumType type) => type switch
    {
        MediumType.Hydro => "Hydroponika",
        MediumType.SemiHydro => "Semi-hydro",
        MediumType.LECA => "LECA",
        MediumType.Perlite => "Perlit",
        MediumType.Water => "Woda",
        MediumType.Soil => "Ziemia",
        MediumType.Other => "Inne",
        _ => type.ToString()
    };

    public static IReadOnlyList<string> AllDisplayStrings() =>
        Enum.GetValues<MediumType>().Select(t => t.ToDisplayString()).ToList();

    public static MediumType FromDisplayString(string display) =>
        Enum.GetValues<MediumType>().FirstOrDefault(t => t.ToDisplayString() == display);
}
