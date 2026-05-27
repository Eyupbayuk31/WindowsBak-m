using System.Collections.ObjectModel;
using System.Windows.Input;
using BakimApp.Models;
using BakimApp.Services;

namespace BakimApp.ViewModels;

public class CleaningViewModel : BaseViewModel
{
    private readonly ICleaningService _cleaningService;
    private ObservableCollection<CleaningCategory> _categories = new();
    private CleaningCategory? _selectedCategory;
    private bool _isScanning;
    private bool _isCleaning;
    private double _overallProgress;
    private string _statusMessage = string.Empty;
    private long _totalSelectedSize;
    private int _totalSelectedFiles;
    private CancellationTokenSource? _cancellationTokenSource;

    public CleaningViewModel()
    {
        _cleaningService = new CleaningService();

        ScanAllCommand = new AsyncRelayCommand(ScanAllAsync);
        CleanSelectedCommand = new AsyncRelayCommand(CleanSelectedAsync, () => SelectedCategoriesCount > 0 && !IsCleaning);
        CancelCommand = new RelayCommand(Cancel, () => IsScanning || IsCleaning);
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
        SelectCategoryCommand = new RelayCommand(OnSelectCategory);

        InitializeCategories();
    }

    public ObservableCollection<CleaningCategory> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public CleaningCategory? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    public bool IsCleaning
    {
        get => _isCleaning;
        set => SetProperty(ref _isCleaning, value);
    }

    public double OverallProgress
    {
        get => _overallProgress;
        set => SetProperty(ref _overallProgress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public long TotalSelectedSize
    {
        get => _totalSelectedSize;
        set
        {
            if (SetProperty(ref _totalSelectedSize, value))
            {
                OnPropertyChanged(nameof(FormattedTotalSelectedSize));
            }
        }
    }

    public string FormattedTotalSelectedSize => CleaningCategory.FormatSize(TotalSelectedSize);

    public int TotalSelectedFiles
    {
        get => _totalSelectedFiles;
        set => SetProperty(ref _totalSelectedFiles, value);
    }

    public int SelectedCategoriesCount => Categories.Count(c => c.IsSelected);

    public ICommand ScanAllCommand { get; }
    public ICommand CleanSelectedCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand SelectCategoryCommand { get; }

    private void InitializeCategories()
    {
        foreach (var category in _cleaningService.GetAllCategories())
        {
            category.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CleaningCategory.IsSelected))
                {
                    UpdateTotalSelected();
                    OnPropertyChanged(nameof(SelectedCategoriesCount));
                }
            };
            Categories.Add(category);
        }
    }

    private void OnSelectCategory(object? parameter)
    {
        if (parameter is CleaningCategory category)
        {
            SelectedCategory = category;
        }
    }

    private void UpdateTotalSelected()
    {
        long totalSize = 0;
        int totalFiles = 0;

        foreach (var cat in Categories.Where(c => c.IsSelected))
        {
            totalSize += cat.Size;
            totalFiles += cat.FileCount;
        }

        TotalSelectedSize = totalSize;
        TotalSelectedFiles = totalFiles;
    }

    private async Task ScanAllAsync()
    {
        IsScanning = true;
        OverallProgress = 0;
        StatusMessage = "Taraniyor...";
        _cancellationTokenSource = new CancellationTokenSource();

        int count = 0;
        foreach (var category in Categories)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                break;

            var progress = new Progress<string>(msg => StatusMessage = msg);
            var scanned = await _cleaningService.ScanCategoryAsync(category.Type, progress, _cancellationTokenSource.Token);

            category.Size = scanned.Size;
            category.FileCount = scanned.FileCount;
            category.Status = scanned.Status;
            category.StatusMessage = scanned.StatusMessage;

            count++;
            OverallProgress = (double)count / Categories.Count * 100;
        }

        UpdateTotalSelected();
        StatusMessage = $"Tarama tamamlandi. {SelectedCategoriesCount} kategori secili.";
        IsScanning = false;
    }

    private async Task CleanSelectedAsync()
    {
        var selectedCategories = Categories.Where(c => c.IsSelected).ToList();
        if (!selectedCategories.Any())
            return;

        IsCleaning = true;
        OverallProgress = 0;
        StatusMessage = "Temizlik baslatiliyor...";
        _cancellationTokenSource = new CancellationTokenSource();

        int count = 0;
        foreach (var category in selectedCategories)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                break;

            category.Status = CleaningStatus.Cleaning;
            category.Progress = 0;

            var progress = new Progress<(int percent, string message)>(p =>
            {
                category.Progress = p.percent;
                StatusMessage = $"{category.Name}: {p.message}";
            });

            var result = await _cleaningService.CleanCategoryAsync(category.Type, progress, _cancellationTokenSource.Token);

            category.Status = result.IsSuccess ? CleaningStatus.Completed : CleaningStatus.Error;
            category.StatusMessage = result.IsSuccess ? $"{result.FilesDeleted} dosya silindi" : result.ErrorMessage ?? "Hata";
            category.Size = 0;
            category.FileCount = 0;
            category.IsSelected = false;

            count++;
            OverallProgress = (double)count / selectedCategories.Count * 100;
        }

        StatusMessage = "Temizlik tamamlandi!";
        IsCleaning = false;
        UpdateTotalSelected();
    }

    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Islem iptal edildi.";
        IsScanning = false;
        IsCleaning = false;
    }

    private void SelectAll()
    {
        foreach (var category in Categories.Where(c => c.Size > 0))
        {
            category.IsSelected = true;
        }
    }

    private void DeselectAll()
    {
        foreach (var category in Categories)
        {
            category.IsSelected = false;
        }
    }
}
