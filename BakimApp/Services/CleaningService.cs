using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using BakimApp.Models;

namespace BakimApp.Services;

public class CleaningService : ICleaningService
{
    private readonly string _tempPath;
    private readonly string _windowsTempPath;
    private readonly string _prefetchPath;
    private readonly string _windowsUpdatePath;
    private readonly string _thumbnailCachePath;
    private readonly string _recentPath;
    private readonly string _localAppData;
    private readonly string _appData;

    public CleaningService()
    {
        _tempPath = Path.GetTempPath();
        _windowsTempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
        _prefetchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
        _windowsUpdatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
        _thumbnailCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Explorer");
        _recentPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
        _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }

    public List<CleaningCategory> GetAllCategories()
    {
        return new List<CleaningCategory>
        {
            CreateCategory(CleaningCategoryType.TempFiles, "\uE8B7", "Ge\u00e7ici Dosyalar", "%TEMP% ve Windows\\Temp", RiskLevel.Safe),
            CreateCategory(CleaningCategoryType.Prefetch, "\uE8F1", "Prefetch Dosyalar\u0131", "Windows\\Prefetch", RiskLevel.Caution),
            CreateCategory(CleaningCategoryType.RecycleBin, "\uE74D", "Geri D\u00f6n\u00fc\u015f\u00fcm Kutusu", "$Recycle.Bin", RiskLevel.Caution),
            CreateCategory(CleaningCategoryType.WindowsUpdateCache, "\uE777", "Windows Update Cache", "SoftwareDistribution", RiskLevel.Caution),
            CreateCategory(CleaningCategoryType.ThumbnailCache, "\uEB9F", "Thumbnail \u00d6nbellek", "Explorer \u00f6nbellek", RiskLevel.Safe),
            CreateCategory(CleaningCategoryType.DnsCache, "\uE968", "DNS \u00d6nbellek", "Sistem \u00f6nbellek", RiskLevel.Safe),
            CreateCategory(CleaningCategoryType.RecentFiles, "\uE823", "Son Dosyalar", "Recent klas\u00f6r\u00fc", RiskLevel.Safe),
            CreateCategory(CleaningCategoryType.BrowserCache, "\uE774", "Taray\u0131c\u0131 \u00d6nbellek", "Chrome, Firefox, Edge", RiskLevel.Safe)
        };
    }

    private CleaningCategory CreateCategory(CleaningCategoryType type, string icon, string name, string description, RiskLevel risk)
    {
        return new CleaningCategory
        {
            Type = type,
            Icon = icon,
            Name = name,
            Description = description,
            RiskLevel = risk,
            Status = CleaningStatus.Ready
        };
    }

    public async Task<CleaningCategory> ScanCategoryAsync(CleaningCategoryType categoryType, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var category = CreateCategoryFromType(categoryType);
        category.Status = CleaningStatus.Scanning;
        category.IsScanning = true;

        await Task.Run(() =>
        {
            try
            {
                switch (categoryType)
                {
                    case CleaningCategoryType.TempFiles:
                        ScanTempFiles(category, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.Prefetch:
                        ScanPrefetch(category, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.RecycleBin:
                        ScanRecycleBin(category, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.WindowsUpdateCache:
                        ScanWindowsUpdateCache(category, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.ThumbnailCache:
                        ScanThumbnailCache(category, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.DnsCache:
                        ScanDnsCache(category, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.RecentFiles:
                        ScanRecentFiles(category, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.BrowserCache:
                        ScanBrowserCache(category, progress, cancellationToken);
                        break;
                }
                category.Status = CleaningStatus.Selected;
            }
            catch (Exception ex)
            {
                category.Status = CleaningStatus.Error;
                category.StatusMessage = ex.Message;
            }
        }, cancellationToken);

        category.IsScanning = false;
        return category;
    }

    private void ScanTempFiles(CleaningCategory category, IProgress<string>? progress, CancellationToken ct)
    {
        long totalSize = 0;
        int totalFiles = 0;

        progress?.Report("Ge\u00e7ici dosyalar taran\u0131yor...");

        var (size1, count1) = GetDirectorySize(_tempPath, ct);
        var (size2, count2) = GetDirectorySize(_windowsTempPath, ct);
        totalSize = size1 + size2;
        totalFiles = count1 + count2;

        category.Size = totalSize;
        category.FileCount = totalFiles;
        category.StatusMessage = $"{totalFiles} dosya bulundu";
    }

    private void ScanPrefetch(CleaningCategory category, IProgress<string>? progress, CancellationToken ct)
    {
        progress?.Report("Prefetch dosyalar\u0131 taran\u0131yor...");
        var (totalSize, totalFiles) = GetDirectorySize(_prefetchPath, ct);
        category.Size = totalSize;
        category.FileCount = totalFiles;
        category.StatusMessage = $"{totalFiles} prefetch dosyas\u0131 bulundu";
    }

    private void ScanRecycleBin(CleaningCategory category, IProgress<string>? progress, CancellationToken ct)
    {
        progress?.Report("Geri d\u00f6n\u00fc\u015f\u00fcm kutusu taran\u0131yor...");

        long totalSize = 0;
        int totalFiles = 0;

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            var recyclePath = Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin");
            if (Directory.Exists(recyclePath))
            {
                var (size, count) = GetDirectorySize(recyclePath, ct);
                totalSize += size;
                totalFiles += count;
            }
        }

        category.Size = totalSize;
        category.FileCount = totalFiles;
        category.StatusMessage = $"{totalFiles} dosya bulundu";
    }

    private void ScanWindowsUpdateCache(CleaningCategory category, IProgress<string>? progress, CancellationToken ct)
    {
        progress?.Report("Windows Update cache taran\u0131yor...");
        var (totalSize, totalFiles) = GetDirectorySize(_windowsUpdatePath, ct);
        category.Size = totalSize;
        category.FileCount = totalFiles;
        category.StatusMessage = $"{totalFiles} g\u00fcncelleme dosyas\u0131 bulundu";
    }

    private void ScanThumbnailCache(CleaningCategory category, IProgress<string>? progress, CancellationToken ct)
    {
        progress?.Report("Thumbnail \u00f6nbellek taran\u0131yor...");

        long totalSize = 0;
        int totalFiles = 0;

        if (Directory.Exists(_thumbnailCachePath))
        {
            // Only scan thumbcache files, not the entire Explorer folder
            var thumbcacheFiles = Directory.GetFiles(_thumbnailCachePath, "thumbcache_*.db", SearchOption.TopDirectoryOnly);
            foreach (var file in thumbcacheFiles)
            {
                try
                {
                    var info = new FileInfo(file);
                    totalSize += info.Length;
                    totalFiles++;
                }
                catch { }
            }
        }

        category.Size = totalSize;
        category.FileCount = totalFiles;
        category.StatusMessage = $"{totalFiles} thumbnail dosyas\u0131 bulundu";
    }

    private void ScanDnsCache(CleaningCategory category, IProgress<string>? progress, CancellationToken ct)
    {
        progress?.Report("DNS cache kontrol ediliyor...");

        var dnsCacheSize = GetDnsCacheSize();
        category.Size = dnsCacheSize;
        category.FileCount = 1;
        category.StatusMessage = "DNS cache boyutu hesapland\u0131";
    }

    private void ScanRecentFiles(CleaningCategory category, IProgress<string>? progress, CancellationToken ct)
    {
        progress?.Report("Son dosyalar taran\u0131yor...");

        long totalSize = 0;
        int totalFiles = 0;

        if (Directory.Exists(_recentPath))
        {
            // Only get .lnk files (shortcuts), not all files
            var shortcutFiles = Directory.GetFiles(_recentPath, "*.lnk", SearchOption.TopDirectoryOnly);
            foreach (var file in shortcutFiles)
            {
                try
                {
                    var info = new FileInfo(file);
                    totalSize += info.Length;
                    totalFiles++;
                }
                catch { }
            }
        }

        category.Size = totalSize;
        category.FileCount = totalFiles;
        category.StatusMessage = $"{totalFiles} son dosya bulundu";
    }

    private void ScanBrowserCache(CleaningCategory category, IProgress<string>? progress, CancellationToken ct)
    {
        progress?.Report("Taray\u0131c\u0131 \u00f6nbellekleri taran\u0131yor...");

        long totalSize = 0;
        int totalFiles = 0;

        // Chrome - scan all profile caches
        ScanChromeCache(ct, ref totalSize, ref totalFiles);

        // Edge - scan all profile caches
        ScanEdgeCache(ct, ref totalSize, ref totalFiles);

        // Firefox - scan all profiles and cache folders
        ScanFirefoxCache(ct, ref totalSize, ref totalFiles);

        category.Size = totalSize;
        category.FileCount = totalFiles;
        category.StatusMessage = $"{totalFiles} taray\u0131c\u0131 dosyas\u0131 bulundu";
    }

    private void ScanChromeCache(CancellationToken ct, ref long totalSize, ref int totalFiles)
    {
        var chromeBase = Path.Combine(_localAppData, "Google", "Chrome", "User Data");

        if (Directory.Exists(chromeBase))
        {
            foreach (var profileDir in Directory.GetDirectories(chromeBase, "Profile*"))
            {
                var cachePath = Path.Combine(profileDir, "Cache");
                var codeCachePath = Path.Combine(profileDir, "Code Cache");

                if (Directory.Exists(cachePath))
                {
                    var (size, count) = GetDirectorySize(cachePath, ct);
                    totalSize += size;
                    totalFiles += count;
                }
                if (Directory.Exists(codeCachePath))
                {
                    var (size, count) = GetDirectorySize(codeCachePath, ct);
                    totalSize += size;
                    totalFiles += count;
                }
            }

            // Default profile
            var defaultCache = Path.Combine(chromeBase, "Default", "Cache");
            if (Directory.Exists(defaultCache))
            {
                var (size, count) = GetDirectorySize(defaultCache, ct);
                totalSize += size;
                totalFiles += count;
            }
        }
    }

    private void ScanEdgeCache(CancellationToken ct, ref long totalSize, ref int totalFiles)
    {
        var edgeBase = Path.Combine(_localAppData, "Microsoft", "Edge", "User Data");

        if (Directory.Exists(edgeBase))
        {
            foreach (var profileDir in Directory.GetDirectories(edgeBase, "Profile*"))
            {
                var cachePath = Path.Combine(profileDir, "Cache");
                var codeCachePath = Path.Combine(profileDir, "Code Cache");

                if (Directory.Exists(cachePath))
                {
                    var (size, count) = GetDirectorySize(cachePath, ct);
                    totalSize += size;
                    totalFiles += count;
                }
                if (Directory.Exists(codeCachePath))
                {
                    var (size, count) = GetDirectorySize(codeCachePath, ct);
                    totalSize += size;
                    totalFiles += count;
                }
            }

            // Default profile
            var defaultCache = Path.Combine(edgeBase, "Default", "Cache");
            if (Directory.Exists(defaultCache))
            {
                var (size, count) = GetDirectorySize(defaultCache, ct);
                totalSize += size;
                totalFiles += count;
            }
        }
    }

    private void ScanFirefoxCache(CancellationToken ct, ref long totalSize, ref int totalFiles)
    {
        var firefoxBase = Path.Combine(_appData, "Mozilla", "Firefox", "Profiles");

        if (Directory.Exists(firefoxBase))
        {
            foreach (var profileDir in Directory.GetDirectories(firefoxBase))
            {
                // cache2 folder
                var cache2Path = Path.Combine(profileDir, "cache2");
                if (Directory.Exists(cache2Path))
                {
                    var (size, count) = GetDirectorySize(cache2Path, ct);
                    totalSize += size;
                    totalFiles += count;
                }

                // startupCache folder
                var startupCachePath = Path.Combine(profileDir, "startupCache");
                if (Directory.Exists(startupCachePath))
                {
                    var (size, count) = GetDirectorySize(startupCachePath, ct);
                    totalSize += size;
                    totalFiles += count;
                }
            }
        }
    }

    public async Task<CleaningResult> CleanCategoryAsync(CleaningCategoryType categoryType, IProgress<(int percent, string message)>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = new CleaningResult { Category = categoryType };
        var sw = Stopwatch.StartNew();

        await Task.Run(() =>
        {
            try
            {
                switch (categoryType)
                {
                    case CleaningCategoryType.TempFiles:
                        CleanTempFiles(result, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.Prefetch:
                        CleanPrefetch(result, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.RecycleBin:
                        CleanRecycleBin(result, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.WindowsUpdateCache:
                        CleanWindowsUpdateCache(result, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.ThumbnailCache:
                        CleanThumbnailCache(result, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.DnsCache:
                        CleanDnsCache(result, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.RecentFiles:
                        CleanRecentFiles(result, progress, cancellationToken);
                        break;
                    case CleaningCategoryType.BrowserCache:
                        CleanBrowserCache(result, progress, cancellationToken);
                        break;
                }
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }
        }, cancellationToken);

        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }

    private void CleanTempFiles(CleaningResult result, IProgress<(int percent, string message)>? progress, CancellationToken ct)
    {
        progress?.Report((0, "Ge\u00e7ici dosyalar temizleniyor..."));

        // Clean user temp folder
        CleanDirectoryWithSubdirs(_tempPath, result, ct, 0, 50);

        // Clean Windows temp folder
        CleanDirectoryWithSubdirs(_windowsTempPath, result, ct, 50, 100);

        progress?.Report((100, "Tamamland\u0131!"));
    }

    private void CleanPrefetch(CleaningResult result, IProgress<(int percent, string message)>? progress, CancellationToken ct)
    {
        progress?.Report((0, "Prefetch dosyalar\u0131 temizleniyor..."));

        if (IsRunAsAdmin())
        {
            CleanDirectoryWithSubdirs(_prefetchPath, result, ct, 0, 100);
        }
        else
        {
            result.FilesFailed = -1;
            result.ErrorMessage = "Y\u00f6netici olarak \u00e7al\u0131\u015ft\u0131r\u0131lmas\u0131 gerekiyor";
        }

        progress?.Report((100, "Tamamland\u0131!"));
    }

    private void CleanRecycleBin(CleaningResult result, IProgress<(int percent, string message)>? progress, CancellationToken ct)
    {
        progress?.Report((0, "Geri d\u00f6n\u00fc\u015f\u00fcm kutusu bo\u015falt\u0131l\u0131yor..."));

        try
        {
            SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
            result.FilesDeleted = 1;
            result.FreedBytes = 0;
        }
        catch (Exception ex)
        {
            result.FilesFailed = 1;
            result.ErrorMessage = ex.Message;
        }

        progress?.Report((100, "Tamamland\u0131!"));
    }

    private void CleanWindowsUpdateCache(CleaningResult result, IProgress<(int percent, string message)>? progress, CancellationToken ct)
    {
        progress?.Report((0, "Windows Update cache temizleniyor..."));

        if (IsRunAsAdmin())
        {
            // Stop the Windows Update service
            StopWindowsUpdateService();

            CleanDirectoryWithSubdirs(_windowsUpdatePath, result, ct, 0, 100);
        }
        else
        {
            result.FilesFailed = -1;
            result.ErrorMessage = "Y\u00f6netici olarak \u00e7al\u0131\u015ft\u0131r\u0131lmas\u0131 gerekiyor";
        }

        progress?.Report((100, "Tamamland\u0131!"));
    }

    private void StopWindowsUpdateService()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "net",
                Arguments = "stop wuauserv",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch { }

        // Also try sc command
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc",
                Arguments = "stop wuauserv",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch { }
    }

    private void CleanThumbnailCache(CleaningResult result, IProgress<(int percent, string message)>? progress, CancellationToken ct)
    {
        progress?.Report((0, "Thumbnail \u00f6nbellek temizleniyor..."));

        if (Directory.Exists(_thumbnailCachePath))
        {
            // Only delete thumbcache files
            var thumbcacheFiles = Directory.GetFiles(_thumbnailCachePath, "thumbcache_*.db", SearchOption.TopDirectoryOnly);
            int current = 0;
            foreach (var file in thumbcacheFiles)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    result.FreedBytes += info.Length;
                    File.Delete(file);
                    result.FilesDeleted++;
                }
                catch
                {
                    result.FilesFailed++;
                }
                current++;
                int fileProgress = (current * 100) / thumbcacheFiles.Length;
                progress?.Report((fileProgress, $"{Path.GetFileName(file)} siliniyor..."));
            }
        }

        progress?.Report((100, "Tamamland\u0131!"));
    }

    private void CleanDnsCache(CleaningResult result, IProgress<(int percent, string message)>? progress, CancellationToken ct)
    {
        progress?.Report((0, "DNS cache temizleniyor..."));

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = "/flushdns",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit();
            result.FilesDeleted = 1;
        }
        catch (Exception ex)
        {
            result.FilesFailed = 1;
            result.ErrorMessage = ex.Message;
        }

        progress?.Report((100, "Tamamland\u0131!"));
    }

    private void CleanRecentFiles(CleaningResult result, IProgress<(int percent, string message)>? progress, CancellationToken ct)
    {
        progress?.Report((0, "Son dosyalar temizleniyor..."));

        if (Directory.Exists(_recentPath))
        {
            var shortcutFiles = Directory.GetFiles(_recentPath, "*.lnk", SearchOption.TopDirectoryOnly);
            int current = 0;
            foreach (var file in shortcutFiles)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    result.FreedBytes += info.Length;
                    File.Delete(file);
                    result.FilesDeleted++;
                }
                catch
                {
                    result.FilesFailed++;
                }
                current++;
                int fileProgress = (current * 100) / shortcutFiles.Length;
                progress?.Report((fileProgress, $"{Path.GetFileName(file)} siliniyor..."));
            }

            // Also clear JumpList by deleting the automatic destinations
            var automaticDestinations = Path.Combine(_recentPath, "AutomaticDestinations");
            if (Directory.Exists(automaticDestinations))
            {
                var destFiles = Directory.GetFiles(automaticDestinations, "*.automaticDestinations-ms");
                foreach (var file in destFiles)
                {
                    try
                    {
                        File.Delete(file);
                        result.FilesDeleted++;
                    }
                    catch { }
                }
            }
        }

        progress?.Report((100, "Tamamland\u0131!"));
    }

    private void CleanBrowserCache(CleaningResult result, IProgress<(int percent, string message)>? progress, CancellationToken ct)
    {
        progress?.Report((0, "Taray\u0131c\u0131 \u00f6nbellekleri temizleniyor..."));

        int totalSteps = 6;
        int currentStep = 0;

        // Close browsers first
        CloseBrowsers();

        // Chrome
        currentStep++;
        progress?.Report(((currentStep * 100) / totalSteps, "Chrome \u00f6nbelle\u011fi temizleniyor..."));
        CleanChromeCache(result, ct);

        // Edge
        currentStep++;
        progress?.Report(((currentStep * 100) / totalSteps, "Edge \u00f6nbelle\u011fi temizleniyor..."));
        CleanEdgeCache(result, ct);

        // Firefox
        currentStep++;
        progress?.Report(((currentStep * 100) / totalSteps, "Firefox \u00f6nbelle\u011fi temiziyor..."));
        CleanFirefoxCache(result, ct);

        progress?.Report((100, "Tamamland\u0131!"));
    }

    private void CloseBrowsers()
    {
        string[] browserProcesses = { "chrome", "msedge", "firefox", "iexplore" };

        foreach (var process in browserProcesses)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(process))
                {
                    try { p.Kill(); } catch { }
                }
            }
            catch { }
        }

        // Wait a bit for processes to close
        Thread.Sleep(1000);
    }

    private void CleanChromeCache(CleaningResult result, CancellationToken ct)
    {
        var chromeBase = Path.Combine(_localAppData, "Google", "Chrome", "User Data");

        if (Directory.Exists(chromeBase))
        {
            foreach (var profileDir in Directory.GetDirectories(chromeBase, "Profile*"))
            {
                var cachePath = Path.Combine(profileDir, "Cache");
                var codeCachePath = Path.Combine(profileDir, "Code Cache");
                var gpuCachePath = Path.Combine(profileDir, "GPUCache");

                if (Directory.Exists(cachePath))
                    CleanDirectoryWithSubdirs(cachePath, result, ct, 0, 0);
                if (Directory.Exists(codeCachePath))
                    CleanDirectoryWithSubdirs(codeCachePath, result, ct, 0, 0);
                if (Directory.Exists(gpuCachePath))
                    CleanDirectoryWithSubdirs(gpuCachePath, result, ct, 0, 0);
            }

            // Default profile
            CleanBrowserProfile(Path.Combine(chromeBase, "Default"), result, ct);
        }
    }

    private void CleanEdgeCache(CleaningResult result, CancellationToken ct)
    {
        var edgeBase = Path.Combine(_localAppData, "Microsoft", "Edge", "User Data");

        if (Directory.Exists(edgeBase))
        {
            foreach (var profileDir in Directory.GetDirectories(edgeBase, "Profile*"))
            {
                CleanBrowserProfile(profileDir, result, ct);
            }

            // Default profile
            CleanBrowserProfile(Path.Combine(edgeBase, "Default"), result, ct);
        }
    }

    private void CleanBrowserProfile(string profilePath, CleaningResult result, CancellationToken ct)
    {
        string[] cacheFolders = { "Cache", "Code Cache", "GPUCache", "databases", "IndexedDB", "Local Storage", "Session Storage" };

        foreach (var folder in cacheFolders)
        {
            var folderPath = Path.Combine(profilePath, folder);
            if (Directory.Exists(folderPath))
            {
                CleanDirectoryWithSubdirs(folderPath, result, ct, 0, 0);
            }
        }
    }

    private void CleanFirefoxCache(CleaningResult result, CancellationToken ct)
    {
        var firefoxBase = Path.Combine(_appData, "Mozilla", "Firefox", "Profiles");

        if (Directory.Exists(firefoxBase))
        {
            foreach (var profileDir in Directory.GetDirectories(firefoxBase))
            {
                string[] cacheFolders = { "cache2", "startupCache", "thumbnails", "webapps-store" };

                foreach (var folder in cacheFolders)
                {
                    var folderPath = Path.Combine(profileDir, folder);
                    if (Directory.Exists(folderPath))
                    {
                        CleanDirectoryWithSubdirs(folderPath, result, ct, 0, 0);
                    }
                }
            }
        }
    }

    private (long size, int fileCount) GetDirectorySize(string path, CancellationToken ct)
    {
        if (!Directory.Exists(path))
            return (0, 0);

        long size = 0;
        int files = 0;

        try
        {
            var filesEnum = Directory.EnumerateFiles(path, "*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            });

            foreach (var file in filesEnum)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    size += info.Length;
                    files++;
                }
                catch { }
            }
        }
        catch { }

        return (size, files);
    }

    private void CleanDirectoryWithSubdirs(string path, CleaningResult result, CancellationToken ct, int startProgress, int endProgress)
    {
        if (!Directory.Exists(path))
            return;

        try
        {
            // First delete all files
            var allFiles = Directory.GetFiles(path, "*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            });

            foreach (var file in allFiles)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    result.FreedBytes += info.Length;
                    File.Delete(file);
                    result.FilesDeleted++;
                }
                catch
                {
                    result.FilesFailed++;
                }
            }

            // Then delete empty subdirectories (bottom-up)
            var dirs = Directory.GetDirectories(path, "*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            }).OrderByDescending(d => d.Count(c => c == Path.DirectorySeparatorChar));

            foreach (var dir in dirs)
            {
                try
                {
                    if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                    }
                }
                catch { }
            }

            // Finally delete the main directory if empty
            try
            {
                if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                {
                    Directory.Delete(path);
                }
            }
            catch { }
        }
        catch (Exception ex)
        {
            result.FilesFailed++;
            if (string.IsNullOrEmpty(result.ErrorMessage))
                result.ErrorMessage = ex.Message;
        }
    }

    private long GetDnsCacheSize()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = "/displaydns",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit();
            return output.Length * 2;
        }
        catch
        {
            return 0;
        }
    }

    private bool IsRunAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private CleaningCategory CreateCategoryFromType(CleaningCategoryType type)
    {
        return type switch
        {
            CleaningCategoryType.TempFiles => CreateCategory(type, "\uE8B7", "Ge\u00e7ici Dosyalar", "%TEMP%", RiskLevel.Safe),
            CleaningCategoryType.Prefetch => CreateCategory(type, "\uE8F1", "Prefetch Dosyalar\u0131", "Prefetch", RiskLevel.Caution),
            CleaningCategoryType.RecycleBin => CreateCategory(type, "\uE74D", "Geri D\u00f6n\u00fc\u015f\u00fcm Kutusu", "Recycle Bin", RiskLevel.Caution),
            CleaningCategoryType.WindowsUpdateCache => CreateCategory(type, "\uE777", "Windows Update Cache", "Updates", RiskLevel.Caution),
            CleaningCategoryType.ThumbnailCache => CreateCategory(type, "\uEB9F", "Thumbnail \u00d6nbellek", "Thumbnails", RiskLevel.Safe),
            CleaningCategoryType.DnsCache => CreateCategory(type, "\uE968", "DNS \u00d6nbellek", "DNS", RiskLevel.Safe),
            CleaningCategoryType.RecentFiles => CreateCategory(type, "\uE823", "Son Dosyalar", "Recent", RiskLevel.Safe),
            CleaningCategoryType.BrowserCache => CreateCategory(type, "\uE774", "Taray\u0131c\u0131 \u00d6nbellek", "Browsers", RiskLevel.Safe),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    // P/Invoke for Recycle Bin
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    private const uint SHERB_NOCONFIRMATION = 0x00000001;
    private const uint SHERB_NOPROGRESSUI = 0x00000002;
    private const uint SHERB_NOSOUND = 0x00000004;
}
