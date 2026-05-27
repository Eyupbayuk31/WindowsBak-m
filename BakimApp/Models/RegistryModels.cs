using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace BakimApp.Models;

public enum RegistryCategoryType
{
    StartupPrograms,
    UninstallEntries,
    SharedDLLs,
    EmptyKeys,
    InvalidPaths,
    FileExtensions,
    HelpFiles,
    ObsoleteKeys
}

public enum RegistryRiskLevel
{
    Safe,
    Caution,
    Dangerous
}

public class RegistryCategory : INotifyPropertyChanged
{
    private bool _isSelected;
    private string _status = "Hazir";
    private bool _isScanning;
    private bool _isCleaning;

    public RegistryCategoryType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public RegistryRiskLevel RiskLevel { get; set; }
    public List<RegistryItem> Items { get; set; } = new();
    public int ItemCount => Items.Count;

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public bool IsScanning
    {
        get => _isScanning;
        set { _isScanning = value; OnPropertyChanged(); }
    }

    public bool IsCleaning
    {
        get => _isCleaning;
        set { _isCleaning = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class RegistryItem : INotifyPropertyChanged
{
    private bool _isSelected = true;

    public string KeyPath { get; set; } = string.Empty;
    public string ValueName { get; set; } = string.Empty;
    public string ValueData { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public RegistryRiskLevel RiskLevel { get; set; }
    public bool IsOrphaned { get; set; }
    public bool IsInvalid { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public string DisplayPath => string.IsNullOrEmpty(ValueName) ? KeyPath : $"{KeyPath}\\{ValueName}";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class RegistryBackup
{
    public DateTime CreatedAt { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int KeysBackedUp { get; set; }
    public long FileSize { get; set; }
}
