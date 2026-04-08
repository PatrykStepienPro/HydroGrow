namespace HydroGrow.Utilities;

public static class DateTimeExtensions
{
    public static string ToRelativeString(this DateTime utcDateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - utcDateTime;

        if (diff.TotalSeconds < 60)
            return "przed chwilą";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes} min temu";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours} godz. temu";
        if (diff.TotalDays < 2)
            return "wczoraj";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays} dni temu";
        if (diff.TotalDays < 30)
            return $"{(int)(diff.TotalDays / 7)} tyg. temu";
        if (diff.TotalDays < 365)
            return $"{(int)(diff.TotalDays / 30)} mies. temu";

        return utcDateTime.ToLocalTime().ToString("d MMM yyyy");
    }

    public static string ToRelativeFutureString(this DateTime utcDateTime)
    {
        var now = DateTime.UtcNow;
        var diff = utcDateTime - now;

        if (diff.TotalSeconds < 0)
        {
            var past = now - utcDateTime;
            if (past.TotalDays < 1) return "dziś (zaległe)";
            return $"zaległe {(int)past.TotalDays} dni";
        }

        if (diff.TotalHours < 24)
            return "dziś";
        if (diff.TotalDays < 2)
            return "jutro";
        if (diff.TotalDays < 7)
            return $"za {(int)diff.TotalDays} dni";
        if (diff.TotalDays < 30)
            return $"za {(int)(diff.TotalDays / 7)} tyg.";

        return utcDateTime.ToLocalTime().ToString("d MMM yyyy");
    }

    public static string ToShortLocalString(this DateTime utcDateTime) =>
        utcDateTime.ToLocalTime().ToString("d MMM yyyy, HH:mm");

    public static string ToDateOnlyLocalString(this DateTime utcDateTime) =>
        utcDateTime.ToLocalTime().ToString("d MMM yyyy");
}
