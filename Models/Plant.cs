namespace HydroGrow.Models;

public class Plant
{
    public int Id { get; set; }
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string MediumType { get; set; } = string.Empty;
    public string AcquiredDate { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int? ThumbnailPhotoId { get; set; }
    public string? ThumbnailPath { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public int IsArchived { get; set; } = 0;

    public bool IsNew => Id == 0;
    public override string ToString() => Name;
}
