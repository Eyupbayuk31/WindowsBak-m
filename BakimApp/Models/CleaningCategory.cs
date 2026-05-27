namespace BakimApp.Models;

public enum CleaningCategoryType
{
    TempFiles,
    Prefetch,
    RecycleBin,
    WindowsUpdateCache,
    ThumbnailCache,
    DnsCache,
    RecentFiles,
    BrowserCache
}

public enum CleaningStatus
{
    Ready,
    Scanning,
    Selected,
    Cleaning,
    Completed,
    Error
}

public enum RiskLevel
{
    Safe,
    Caution,
    Critical
}

public class CleaningCategory : BakimApp.ViewModels.BaseViewModel
{
    private CleaningCategoryType _type;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _icon = string.Empty;
    private long _size;
    private int _fileCount;
    private bool _isSelected;
    private CleaningStatus _status;
    private RiskLevel _riskLevel;
    private string _statusMessage = string.Empty;
    private double _progress;
    private bool _isScanning;

    public CleaningCategoryType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    public long Size
    {
        get => _size;
        set
        {
            if (SetProperty(ref _size, value))
            {
                OnPropertyChanged(nameof(FormattedSize));
            }
        }
    }

    public string FormattedSize => FormatSize(Size);

    public int FileCount
    {
        get => _fileCount;
        set => SetProperty(ref _fileCount, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public CleaningStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public RiskLevel RiskLevel
    {
        get => _riskLevel;
        set
        {
            if (SetProperty(ref _riskLevel, value))
            {
                OnPropertyChanged(nameof(RiskColor));
            }
        }
    }

    public string RiskColor => RiskLevel switch
    {
        RiskLevel.Safe => "#6CCB5F",
        RiskLevel.Caution => "#FCE100",
        RiskLevel.Critical => "#F85149",
        _ => "#9E9E9E"
    };

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    public static string FormatSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }
}
