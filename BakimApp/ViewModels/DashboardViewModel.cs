using System.Windows.Input;
using System.Windows.Threading;
using BakimApp.Models;
using BakimApp.Services;

namespace BakimApp.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly SystemInfoService _systemInfoService;
    private SystemInfo _systemInfo = new();
    private List<DiskUsage> _diskUsages = new();
    private readonly DispatcherTimer _timer;

    public DashboardViewModel()
    {
        _systemInfoService = new SystemInfoService();

        RefreshCommand = new RelayCommand(RefreshData);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += (s, e) => RefreshData();
        _timer.Start();

        RefreshData();
    }

    public SystemInfo SystemInfo
    {
        get => _systemInfo;
        set => SetProperty(ref _systemInfo, value);
    }

    public List<DiskUsage> DiskUsages
    {
        get => _diskUsages;
        set => SetProperty(ref _diskUsages, value);
    }

    public double CpuUsage => SystemInfo.CpuUsage;
    public double RamUsagePercent => SystemInfo.RamUsagePercent;
    public double DiskUsagePercent => SystemInfo.DiskUsagePercent;

    public string FormattedCpuUsage => $"{CpuUsage:F1}%";
    public string FormattedRamUsage => $"{RamUsagePercent:F1}%";
    public string FormattedDiskUsage => $"{DiskUsagePercent:F1}%";

    public ICommand RefreshCommand { get; }

    private void RefreshData()
    {
        SystemInfo = _systemInfoService.GetSystemInfo();
        DiskUsages = _systemInfoService.GetDiskUsages();

        OnPropertyChanged(nameof(CpuUsage));
        OnPropertyChanged(nameof(RamUsagePercent));
        OnPropertyChanged(nameof(DiskUsagePercent));
        OnPropertyChanged(nameof(FormattedCpuUsage));
        OnPropertyChanged(nameof(FormattedRamUsage));
        OnPropertyChanged(nameof(FormattedDiskUsage));
    }

    public void StopTimer()
    {
        _timer.Stop();
    }
}
