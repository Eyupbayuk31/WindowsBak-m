namespace BakimApp.Models;

public class DiskUsage
{
    public string DriveLetter { get; set; } = string.Empty;
    public string VolumeLabel { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long UsedSpace { get; set; }
    public long FreeSpace { get; set; }
    public double UsagePercent => TotalSize > 0 ? (double)UsedSpace / TotalSize * 100 : 0;

    public string FormattedTotal => CleaningCategory.FormatSize(TotalSize);
    public string FormattedFree => CleaningCategory.FormatSize(FreeSpace);
    public string FormattedUsed => CleaningCategory.FormatSize(UsedSpace);
}
