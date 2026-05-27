using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using BakimApp.Models;

namespace BakimApp.Services;

public class RegistryService
{
    private readonly string _backupFolder;

    public RegistryService()
    {
        _backupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BakimApp", "Backups");
        Directory.CreateDirectory(_backupFolder);
    }

    public List<RegistryCategory> GetCategories()
    {
        return new List<RegistryCategory>
        {
            new RegistryCategory
            {
                Type = RegistryCategoryType.StartupPrograms,
                Name = "Baslangic Programlari",
                Description = "Sistem acilisinda calisan programlar",
                Icon = "\uE8F1",
                RiskLevel = RegistryRiskLevel.Caution
            },
            new RegistryCategory
            {
                Type = RegistryCategoryType.UninstallEntries,
                Name = "Kaldirilmis Programlar",
                Description = "Silinmis ancak kaydi kalan programlar",
                Icon = "\uE74D",
                RiskLevel = RegistryRiskLevel.Dangerous
            },
            new RegistryCategory
            {
                Type = RegistryCategoryType.SharedDLLs,
                Name = "Paylasilan DLL'ler",
                Description = "Artik kullanilmayan paylasili DLL dosyalari",
                Icon = "\uE8F4",
                RiskLevel = RegistryRiskLevel.Safe
            },
            new RegistryCategory
            {
                Type = RegistryCategoryType.EmptyKeys,
                Name = "Bos Anahtarlar",
                Description = "Deger icermeyen kayit defteri anahtarlari",
                Icon = "\uE8B7",
                RiskLevel = RegistryRiskLevel.Caution
            },
            new RegistryCategory
            {
                Type = RegistryCategoryType.InvalidPaths,
                Name = "Gecersiz Yolcular",
                Description = "Olmayan dosyalara isaret eden kayitlar",
                Icon = "\uE783",
                RiskLevel = RegistryRiskLevel.Caution
            },
            new RegistryCategory
            {
                Type = RegistryCategoryType.FileExtensions,
                Name = "Dosya Uyantilari",
                Description = "Artik kullanilmayan dosya uzantisi kayitlari",
                Icon = "\uE8A5",
                RiskLevel = RegistryRiskLevel.Caution
            },
            new RegistryCategory
            {
                Type = RegistryCategoryType.HelpFiles,
                Name = "Yardim Dosyalari",
                Description = "Silinmis programlarin yardim dosyalari",
                Icon = "\uE897",
                RiskLevel = RegistryRiskLevel.Safe
            },
            new RegistryCategory
            {
                Type = RegistryCategoryType.ObsoleteKeys,
                Name = "Eski Kayitlar",
                Description = "Eski Windows surumlerinden kalan kayitlar",
                Icon = "\uE7C3",
                RiskLevel = RegistryRiskLevel.Dangerous
            }
        };
    }

    public void ScanCategory(RegistryCategory category, IProgress<string>? progress = null)
    {
        progress?.Report($"{category.Name} taraniyor...");

        switch (category.Type)
        {
            case RegistryCategoryType.StartupPrograms:
                ScanStartupPrograms(category, progress);
                break;
            case RegistryCategoryType.UninstallEntries:
                ScanUninstallEntries(category, progress);
                break;
            case RegistryCategoryType.SharedDLLs:
                ScanSharedDLLs(category, progress);
                break;
            case RegistryCategoryType.EmptyKeys:
                ScanEmptyKeys(category, progress);
                break;
            case RegistryCategoryType.InvalidPaths:
                ScanInvalidPaths(category, progress);
                break;
            case RegistryCategoryType.FileExtensions:
                ScanFileExtensions(category, progress);
                break;
            case RegistryCategoryType.HelpFiles:
                ScanHelpFiles(category, progress);
                break;
            case RegistryCategoryType.ObsoleteKeys:
                ScanObsoleteKeys(category, progress);
                break;
        }

        category.Status = $"{category.ItemCount} oge bulundu";
    }

    private void ScanStartupPrograms(RegistryCategory category, IProgress<string>? progress)
    {
        var items = new List<RegistryItem>();

        // HKCU Run
        ScanRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", items, category);
        ScanRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", items, category);

        // HKLM Run
        ScanRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", items, category, RegistryHive.LocalMachine);
        ScanRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", items, category, RegistryHive.LocalMachine);

        // Startup folder
        ScanStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), items, category);
        ScanStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), items, category);

        category.Items = items;
    }

    private void ScanUninstallEntries(RegistryCategory category, IProgress<string>? progress)
    {
        var items = new List<RegistryItem>();

        // HKEY_LOCAL_MACHINE Uninstall
        ScanUninstallRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", items, category, RegistryHive.LocalMachine);

        // HKEY_CURRENT_USER Uninstall
        ScanUninstallRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", items, category, RegistryHive.CurrentUser);

        category.Items = items;
    }

    private void ScanSharedDLLs(RegistryCategory category, IProgress<string>? progress)
    {
        var items = new List<RegistryItem>();

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs");
            if (key != null)
            {
                foreach (var valueName in key.GetValueNames())
                {
                    try
                    {
                        var value = key.GetValue(valueName);
                        var count = value is int ? (int)value : 0;

                        if (count == 0 && File.Exists(valueName))
                        {
                            items.Add(new RegistryItem
                            {
                                KeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs",
                                ValueName = valueName,
                                ValueData = count.ToString(),
                                Category = category.Name,
                                RiskLevel = RegistryRiskLevel.Safe
                            });
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        category.Items = items;
    }

    private void ScanEmptyKeys(RegistryCategory category, IProgress<string>? progress)
    {
        var items = new List<RegistryItem>();

        // Check common empty key locations
        string[] emptyKeyPaths = {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var path in emptyKeyPaths)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(path);
                if (key != null && key.GetValueNames().Length == 0)
                {
                    items.Add(new RegistryItem
                    {
                        KeyPath = @"HKEY_CURRENT_USER\" + path,
                        ValueName = "(Bos Anahtar)",
                        Category = category.Name,
                        RiskLevel = RegistryRiskLevel.Caution
                    });
                }
            }
            catch { }
        }

        category.Items = items;
    }

    private void ScanInvalidPaths(RegistryCategory category, IProgress<string>? progress)
    {
        var items = new List<RegistryItem>();

        // Check uninstall entries for invalid paths
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    try
                    {
                        using var subKey = key.OpenSubKey(subKeyName);
                        var installLocation = subKey?.GetValue("InstallLocation") as string;

                        if (!string.IsNullOrEmpty(installLocation) && !Directory.Exists(installLocation))
                        {
                            items.Add(new RegistryItem
                            {
                                KeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + subKeyName,
                                ValueName = "InstallLocation",
                                ValueData = installLocation,
                                Category = category.Name,
                                RiskLevel = RegistryRiskLevel.Caution,
                                IsInvalid = true
                            });
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        category.Items = items;
    }

    private void ScanFileExtensions(RegistryCategory category, IProgress<string>? progress)
    {
        var items = new List<RegistryItem>();

        // Check for orphaned extension handlers
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey(@".*");
            if (key != null)
            {
                foreach (var ext in key.GetSubKeyNames())
                {
                    try
                    {
                        using var extKey = key.OpenSubKey(ext);
                        var handler = extKey?.GetValue("") as string;

                        if (!string.IsNullOrEmpty(handler))
                        {
                            using var handlerKey = Registry.ClassesRoot.OpenSubKey(handler);
                            if (handlerKey == null)
                            {
                                items.Add(new RegistryItem
                                {
                                    KeyPath = @"HKEY_CLASSES_ROOT\" + ext,
                                    ValueName = "(Varsayilan)",
                                    ValueData = handler,
                                    Category = category.Name,
                                    RiskLevel = RegistryRiskLevel.Caution,
                                    IsOrphaned = true
                                });
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        category.Items = items;
    }

    private void ScanHelpFiles(RegistryCategory category, IProgress<string>? progress)
    {
        var items = new List<RegistryItem>();

        // Check for orphaned help file references
        string[] helpKeyPaths = {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Help"
        };

        foreach (var path in helpKeyPaths)
        {
            ScanRegistryKey(path, items, category, RegistryHive.LocalMachine);
        }

        category.Items = items;
    }

    private void ScanObsoleteKeys(RegistryCategory category, IProgress<string>? progress)
    {
        var items = new List<RegistryItem>();

        // Check for old Windows version keys
        string[] obsoletePaths = {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Setup",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\ServicePack",
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\BitBucket"
        };

        foreach (var path in obsoletePaths)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(path);
                if (key != null)
                {
                    var subKeys = key.GetSubKeyNames();
                    if (subKeys.Length == 0)
                    {
                        items.Add(new RegistryItem
                        {
                            KeyPath = @"HKEY_LOCAL_MACHINE\" + path,
                            ValueName = "(Bos veya eski anahtar)",
                            Category = category.Name,
                            RiskLevel = RegistryRiskLevel.Dangerous
                        });
                    }
                }
            }
            catch { }
        }

        category.Items = items;
    }

    private void ScanRegistryKey(string path, List<RegistryItem> items, RegistryCategory category, RegistryHive hive = RegistryHive.CurrentUser)
    {
        try
        {
            using var key = hive == RegistryHive.LocalMachine
                ? Registry.LocalMachine.OpenSubKey(path)
                : Registry.CurrentUser.OpenSubKey(path);

            if (key != null)
            {
                foreach (var valueName in key.GetValueNames())
                {
                    try
                    {
                        var value = key.GetValue(valueName);
                        if (value != null)
                        {
                            var riskLevel = RegistryRiskLevel.Caution;
                            if (value.ToString()?.Contains("explorer") == true)
                                riskLevel = RegistryRiskLevel.Safe;

                            items.Add(new RegistryItem
                            {
                                KeyPath = (hive == RegistryHive.LocalMachine ? @"HKEY_LOCAL_MACHINE\" : @"HKEY_CURRENT_USER\") + path,
                                ValueName = string.IsNullOrEmpty(valueName) ? "(Varsayilan)" : valueName,
                                ValueData = value.ToString() ?? "",
                                Category = category.Name,
                                RiskLevel = riskLevel
                            });
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private void ScanUninstallRegistryKey(string path, List<RegistryItem> items, RegistryCategory category, RegistryHive hive)
    {
        try
        {
            using var key = hive == RegistryHive.LocalMachine
                ? Registry.LocalMachine.OpenSubKey(path)
                : Registry.CurrentUser.OpenSubKey(path);

            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    try
                    {
                        using var subKey = key.OpenSubKey(subKeyName);
                        if (subKey != null)
                        {
                            var displayName = subKey.GetValue("DisplayName") as string;
                            var installLocation = subKey.GetValue("InstallLocation") as string;
                            var uninstallString = subKey.GetValue("UninstallString") as string;

                            // Check if the program actually exists
                            bool isOrphaned = false;
                            if (!string.IsNullOrEmpty(installLocation) && !Directory.Exists(installLocation))
                            {
                                isOrphaned = true;
                            }
                            else if (string.IsNullOrEmpty(displayName))
                            {
                                isOrphaned = true;
                            }

                            items.Add(new RegistryItem
                            {
                                KeyPath = (hive == RegistryHive.LocalMachine ? @"HKEY_LOCAL_MACHINE\" : @"HKEY_CURRENT_USER\") + path + @"\" + subKeyName,
                                ValueName = "DisplayName",
                                ValueData = displayName ?? "(Adsiz)",
                                Category = category.Name,
                                RiskLevel = isOrphaned ? RegistryRiskLevel.Dangerous : RegistryRiskLevel.Safe,
                                IsOrphaned = isOrphaned
                            });
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private void ScanStartupFolder(string folderPath, List<RegistryItem> items, RegistryCategory category)
    {
        try
        {
            if (Directory.Exists(folderPath))
            {
                foreach (var file in Directory.GetFiles(folderPath, "*.lnk"))
                {
                    var fileInfo = new FileInfo(file);
                    var riskLevel = RegistryRiskLevel.Safe;

                    if (fileInfo.Name.Contains("unins"))
                        riskLevel = RegistryRiskLevel.Caution;

                    items.Add(new RegistryItem
                    {
                        KeyPath = folderPath,
                        ValueName = fileInfo.Name,
                        ValueData = fileInfo.FullName,
                        Category = category.Name,
                        RiskLevel = riskLevel
                    });
                }
            }
        }
        catch { }
    }

    public bool DeleteRegistryItem(RegistryItem item, IProgress<string>? progress = null)
    {
        try
        {
            progress?.Report($"Siliniyor: {item.DisplayPath}");

            // Parse the key path
            var parts = item.KeyPath.Split('\\', 2);
            if (parts.Length < 2) return false;

            var hiveName = parts[0].Replace("HKEY_LOCAL_MACHINE\\", "").Replace("HKEY_CURRENT_USER\\", "");
            var subPath = parts[1];

            RegistryHive hive;
            if (hiveName.StartsWith("HKEY_LOCAL_MACHINE") || hiveName == "SOFTWARE")
                hive = RegistryHive.LocalMachine;
            else
                hive = RegistryHive.CurrentUser;

            // Delete the key or value
            if (item.IsOrphaned || item.IsInvalid)
            {
                // Delete the subkey
                return DeleteRegistryKey(hive, subPath);
            }
            else
            {
                // Delete the value
                return DeleteRegistryValue(hive, subPath, item.ValueName);
            }
        }
        catch (Exception ex)
        {
            progress?.Report($"Hata: {ex.Message}");
            return false;
        }
    }

    private bool DeleteRegistryKey(RegistryHive hive, string keyPath)
    {
        try
        {
            var rootKey = hive == RegistryHive.LocalMachine ? Registry.LocalMachine : Registry.CurrentUser;

            // Find parent key and subkey name
            var lastSlash = keyPath.LastIndexOf('\\');
            if (lastSlash < 0) return false;

            var parentPath = keyPath.Substring(0, lastSlash);
            var subKeyName = keyPath.Substring(lastSlash + 1);

            using var parentKey = rootKey.OpenSubKey(parentPath, true);
            parentKey?.DeleteSubKeyTree(subKeyName, false);
            return true;
        }
        catch { return false; }
    }

    private bool DeleteRegistryValue(RegistryHive hive, string keyPath, string valueName)
    {
        try
        {
            var rootKey = hive == RegistryHive.LocalMachine ? Registry.LocalMachine : Registry.CurrentUser;
            using var key = rootKey.OpenSubKey(keyPath, true);
            key?.DeleteValue(valueName, false);
            return true;
        }
        catch { return false; }
    }

    public RegistryBackup CreateBackup(string description, List<RegistryItem> items)
    {
        var backup = new RegistryBackup
        {
            CreatedAt = DateTime.Now,
            Description = description,
            KeysBackedUp = items.Count
        };

        try
        {
            var fileName = $"registry_backup_{DateTime.Now:yyyyMMdd_HHmmss}.reg";
            var filePath = Path.Combine(_backupFolder, fileName);

            var content = "Windows Registry Editor Version 5.00\n\n";

            foreach (var item in items)
            {
                content += $"\n[{item.KeyPath}]\n";
                if (!string.IsNullOrEmpty(item.ValueName))
                {
                    content += $"\"{item.ValueName}\"=\"{item.ValueData}\"\n";
                }
            }

            File.WriteAllText(filePath, content);
            backup.FilePath = filePath;
            backup.FileSize = new FileInfo(filePath).Length;
        }
        catch { }

        return backup;
    }

    public List<RegistryBackup> GetBackups()
    {
        var backups = new List<RegistryBackup>();

        try
        {
            if (Directory.Exists(_backupFolder))
            {
                foreach (var file in Directory.GetFiles(_backupFolder, "*.reg"))
                {
                    var info = new FileInfo(file);
                    backups.Add(new RegistryBackup
                    {
                        CreatedAt = info.CreationTime,
                        FilePath = file,
                        FileName = info.Name,
                        FileSize = info.Length,
                        Description = "Registry Yedegi"
                    });
                }
            }
        }
        catch { }

        return backups.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public bool RestoreBackup(RegistryBackup backup)
    {
        try
        {
            if (!File.Exists(backup.FilePath)) return false;

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "regedit.exe",
                Arguments = $"/s \"{backup.FilePath}\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            System.Diagnostics.Process.Start(psi);
            return true;
        }
        catch { return false; }
    }
}
