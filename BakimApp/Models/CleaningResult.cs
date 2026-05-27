namespace BakimApp.Models;

public class CleaningResult
{
    public CleaningCategoryType Category { get; set; }
    public long FreedBytes { get; set; }
    public int FilesDeleted { get; set; }
    public int FilesFailed { get; set; }
    public string FormattedFreed => CleaningCategory.FormatSize(FreedBytes);
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
}