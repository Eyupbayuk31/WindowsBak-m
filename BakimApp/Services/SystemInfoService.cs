using System.Diagnostics;
using System.IO;
using System.Management;
using BakimApp.Models;

namespace BakimApp.Services;

public class SystemInfoService
{
    public SystemInfo GetSystemInfo()
    {
        var info = new SystemInfo
        {
            ComputerName = Environment.MachineName,
            OsVersion = Environment.OSVersion.VersionString,
            ProcessorName = GetProcessorName(),
            ProcessorCores = Environment.ProcessorCount,
        };

        // Memory info
        var (totalRam, availableRam) = GetMemoryInfo();
        info.TotalRam = totalRam;
        info.AvailableRam = availableRam;

        // Disk info
        var (totalDisk, freeDisk) = GetDiskInfo();
        info.TotalDiskSpace = totalDisk;
        info.AvailableDiskSpace = freeDisk;

        // CPU usage
        info.CpuUsage = GetCpuUsage();

        return info;
    }

    private string GetProcessorName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                return obj["Name"]?.ToString() ?? "Unknown";
            }
        }
        catch { }
        return "Unknown";
    }

    private (long total, long available) GetMemoryInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                var total = Convert.ToInt64(obj["TotalVisibleMemorySize"]) * 1024;
                var free = Convert.ToInt64(obj["FreePhysicalMemory"]) * 1024;
                return (total, free);
            }
        }
        catch { }
        return (0, 0);
    }

    private (long total, long free) GetDiskInfo()
    {
        try
        {
            var drive = DriveInfo.GetDrives()
                .FirstOrDefault(d => d.IsReady && d.RootDirectory.Root.FullName == "C:\\");

            if (drive != null)
            {
                return (drive.TotalSize, drive.AvailableFreeSpace);
            }
        }
        catch { }
        return (0, 0);
    }

    private double GetCpuUsage()
    {
        try
        {
            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            System.Threading.Thread.Sleep(100);
            return Math.Round(cpuCounter.NextValue(), 1);
        }
        catch
        {
            return 0;
        }
    }

    public List<DiskUsage> GetDiskUsages()
    {
        var disks = new List<DiskUsage>();

        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                disks.Add(new DiskUsage
                {
                    DriveLetter = drive.RootDirectory.FullName,
                    VolumeLabel = drive.VolumeLabel,
                    TotalSize = drive.TotalSize,
                    UsedSpace = drive.TotalSize - drive.AvailableFreeSpace,
                    FreeSpace = drive.AvailableFreeSpace
                });
            }
        }
        catch { }

        return disks;
    }
}