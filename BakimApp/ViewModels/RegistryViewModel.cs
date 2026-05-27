using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using BakimApp.Models;
using BakimApp.Services;

namespace BakimApp.ViewModels;

public class RegistryViewModel : BaseViewModel
{
    private readonly RegistryService _registryService;
    private ObservableCollection<RegistryCategory> _categories = new();
    private ObservableCollection<RegistryItem> _categoryItems = new();
    private RegistryCategory? _selectedCategory;
    private bool _isScanning;
    private bool _isCleaning;
    private bool _isBackingUp;
    private string _statusMessage = "Hazir";
    private double _progress;
    private ObservableCollection<RegistryBackup> _backups = new();
    private CancellationTokenSource? _cancellationTokenSource;

    public RegistryViewModel()
    {
        _registryService = new RegistryService();

        ScanAllCommand = new RelayCommand(ScanAll, () => !IsScanning && !IsCleaning);
        CleanSelectedCommand = new RelayCommand(CleanSelected, () => CategoryItems.Any(i => i.IsSelected) && !IsCleaning);
        BackupCommand = new RelayCommand(CreateBackup, () => !IsBackingUp);
        RestoreCommand = new RelayCommand(RestoreBackup, () => SelectedBackup != null);
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
        CancelCommand = new RelayCommand(Cancel);
        SelectCategoryCommand = new RelayCommand(SelectCategory);

        InitializeCategories();
        LoadBackups();
    }

    public ObservableCollection<RegistryCategory> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public ObservableCollection<RegistryItem> CategoryItems
    {
        get => _categoryItems;
        set => SetProperty(ref _categoryItems, value);
    }

    public RegistryCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                UpdateCategoryItems();
                OnPropertyChanged(nameof(CategoryItemCount));
                OnPropertyChanged(nameof(HasSelectedCategory));
            }
        }
    }

    public bool HasSelectedCategory => SelectedCategory != null;

    public int CategoryItemCount => SelectedCategory?.ItemCount ?? 0;

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

    public bool IsBackingUp
    {
        get => _isBackingUp;
        set => SetProperty(ref _isBackingUp, value);
    }

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

    public ObservableCollection<RegistryBackup> Backups
    {
        get => _backups;
        set => SetProperty(ref _backups, value);
    }

    public RegistryBackup? SelectedBackup { get; set; }

    public int TotalSelectedCount => Categories.Sum(c => c.Items.Count(i => i.IsSelected));
    public int TotalOrphanedCount => Categories.Sum(c => c.Items.Count(i => i.IsOrphaned));
    public int TotalInvalidCount => Categories.Sum(c => c.Items.Count(i => i.IsInvalid));

    public ICommand ScanAllCommand { get; }
    public ICommand CleanSelectedCommand { get; }
    public ICommand BackupCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SelectCategoryCommand { get; }

    private void InitializeCategories()
    {
        foreach (var category in _registryService.GetCategories())
        {
            category.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(RegistryCategory.ItemCount) ||
                    e.PropertyName == nameof(RegistryCategory.Status) ||
                    e.PropertyName == nameof(RegistryCategory.IsScanning))
                {
                    OnPropertyChanged(nameof(TotalSelectedCount));
                    OnPropertyChanged(nameof(TotalOrphanedCount));
                    OnPropertyChanged(nameof(TotalInvalidCount));
                }
            };
            Categories.Add(category);
        }
    }

    private void UpdateCategoryItems()
    {
        CategoryItems.Clear();
        if (SelectedCategory != null)
        {
            foreach (var item in SelectedCategory.Items)
            {
                CategoryItems.Add(item);
            }
        }
    }

    private void SelectCategory(object? parameter)
    {
        if (parameter is RegistryCategory category)
        {
            SelectedCategory = category;
        }
    }

    private void LoadBackups()
    {
        Backups = new ObservableCollection<RegistryBackup>(_registryService.GetBackups());
    }

    private void ScanAll()
    {
        IsScanning = true;
        StatusMessage = "Taraniyor...";
        _cancellationTokenSource = new CancellationTokenSource();
        Progress = 0;

        var token = _cancellationTokenSource.Token;
        var selectedCategorySnapshot = SelectedCategory;

        Task.Run(async () =>
        {
            int count = 0;
            foreach (var category in Categories)
            {
                if (token.IsCancellationRequested) break;

                category.IsScanning = true;
                var progress = new Progress<string>(msg =>
                {
                    category.Status = msg;
                });

                await Task.Run(() => _registryService.ScanCategory(category, progress), token);

                category.IsScanning = false;
                count++;
                Progress = (double)count / Categories.Count * 100;
            }

            IsScanning = false;
            var totalItems = Categories.Sum(c => c.ItemCount);
            StatusMessage = $"Tarama tamamlandi. {totalItems} oge bulundu.";

            // Update UI - show selected category items
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // If no category selected, select the first one with items
                if (selectedCategorySnapshot == null || selectedCategorySnapshot.ItemCount == 0)
                {
                    selectedCategorySnapshot = Categories.FirstOrDefault(c => c.ItemCount > 0);
                }

                if (selectedCategorySnapshot != null)
                {
                    SelectedCategory = selectedCategorySnapshot;
                }
            });
        }, token);
    }

    private void CleanSelected()
    {
        var selectedItems = CategoryItems.Where(i => i.IsSelected).ToList();
        if (selectedItems.Count == 0)
        {
            StatusMessage = "Temizlenecek oge yok.";
            return;
        }

        IsCleaning = true;
        StatusMessage = "Temizleniyor...";
        _cancellationTokenSource = new CancellationTokenSource();
        Progress = 0;

        var token = _cancellationTokenSource.Token;

        Task.Run(async () =>
        {
            int deleted = 0;
            int failed = 0;

            // First create backup
            StatusMessage = "Yedek olusturuluyor...";
            var backup = _registryService.CreateBackup("Otomatik yedek", selectedItems);

            foreach (var item in selectedItems)
            {
                if (token.IsCancellationRequested) break;

                var progress = new Progress<string>(msg => StatusMessage = msg);

                var success = await Task.Run(() => _registryService.DeleteRegistryItem(item, progress), token);

                if (success)
                    deleted++;
                else
                    failed++;

                Progress = (double)(deleted + failed) / selectedItems.Count * 100;
            }

            // Refresh all categories
            foreach (var category in Categories)
            {
                _registryService.ScanCategory(category, null);
            }

            // Update UI
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateCategoryItems();
            });

            IsCleaning = false;
            StatusMessage = $"Tamamlandi. {deleted} silindi, {failed} basarisiz.";
            LoadBackups();
        }, token);
    }

    private void CreateBackup()
    {
        var allItems = Categories.SelectMany(c => c.Items).ToList();
        if (allItems.Count == 0)
        {
            StatusMessage = "Yedeklenecek oge yok.";
            return;
        }

        IsBackingUp = true;
        StatusMessage = "Yedek olusturuluyor...";

        var backup = _registryService.CreateBackup("Manuel yedek", allItems);

        IsBackingUp = false;
        StatusMessage = $"Yedek olusturuldu!";
        LoadBackups();
    }

    private void RestoreBackup()
    {
        if (SelectedBackup == null) return;

        StatusMessage = "Yedek geri yukleniyor...";
        _registryService.RestoreBackup(SelectedBackup);
        StatusMessage = "Yedek geri yuklendi. Degisiklikler etkin olmasi icin bilgisayari yeniden baslatin.";
    }

    private void SelectAll()
    {
        foreach (var item in CategoryItems)
        {
            item.IsSelected = true;
        }
        OnPropertyChanged(nameof(TotalSelectedCount));
        OnPropertyChanged(nameof(CategoryItemCount));
    }

    private void DeselectAll()
    {
        foreach (var item in CategoryItems)
        {
            item.IsSelected = false;
        }
        OnPropertyChanged(nameof(TotalSelectedCount));
        OnPropertyChanged(nameof(CategoryItemCount));
    }

    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        IsScanning = false;
        IsCleaning = false;
        StatusMessage = "Islem iptal edildi.";
    }
}
