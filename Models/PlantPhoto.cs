namespace HydroGrow.Models;

public class PlantPhoto
{
    public int Id { get; set; }
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();
    public int PlantId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string TakenAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string Caption { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
