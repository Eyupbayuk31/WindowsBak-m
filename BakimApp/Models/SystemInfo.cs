namespace BakimApp.Models;

public class SystemInfo
{
    public string ComputerName { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string ProcessorName { get; set; } = string.Empty;
    public int ProcessorCores { get; set; }
    public long TotalRam { get; set; }
    public long AvailableRam { get; set; }
    public long TotalDiskSpace { get; set; }
    public long AvailableDiskSpace { get; set; }
    public DateTime LastCleanupDate { get; set; }

    public double CpuUsage { get; set; }
    public double RamUsagePercent => TotalRam > 0 ? (double)(TotalRam - AvailableRam) / TotalRam * 100 : 0;
    public double DiskUsagePercent => TotalDiskSpace > 0 ? (double)(TotalDiskSpace - AvailableDiskSpace) / TotalDiskSpace * 100 : 0;

    public string FormattedTotalRam => CleaningCategory.FormatSize(TotalRam);
    public string FormattedAvailableRam => CleaningCategory.FormatSize(AvailableRam);
    public string FormattedTotalDisk => CleaningCategory.FormatSize(TotalDiskSpace);
    public string FormattedAvailableDisk => CleaningCategory.FormatSize(AvailableDiskSpace);
}
