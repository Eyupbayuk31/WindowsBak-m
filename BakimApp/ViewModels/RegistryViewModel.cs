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
        CleanSelectedCommand = new RelayCommand(CleanSelected, () => Categories.Any(c => c.Items.Any(i => i.IsSelected)) && !IsCleaning);
        BackupCommand = new RelayCommand(CreateBackup, () => !IsBackingUp);
        RestoreCommand = new RelayCommand(RestoreBackup, () => SelectedBackup != null);
        CancelCommand = new RelayCommand(Cancel);

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
            }
        }
    }

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
    public ICommand CancelCommand { get; }

    // SelectCategoryCommand - can accept a category or nothing (to select first)
    public RelayCommand? SelectCategoryCommand { get; private set; }

    private void InitializeCategories()
    {
        foreach (var category in _registryService.GetCategories())
        {
            category.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(TotalSelectedCount));
                OnPropertyChanged(nameof(TotalOrphanedCount));
                OnPropertyChanged(nameof(TotalInvalidCount));
            };
            Categories.Add(category);
        }

        // Initialize SelectCategoryCommand
        SelectCategoryCommand = new RelayCommand(SelectCategory);
    }

    private void SelectCategory()
    {
        // Select first non-empty category
        var firstNonEmpty = Categories.FirstOrDefault(c => c.ItemCount > 0);
        if (firstNonEmpty != null)
            SelectedCategory = firstNonEmpty;
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

    private void LoadBackups()
    {
        Backups = new ObservableCollection<RegistryBackup>(_registryService.GetBackups());
    }

    private void ScanAll()
    {
        IsScanning = true;
        StatusMessage = "Taraniyor...";
        Progress = 0;
        _cancellationTokenSource = new CancellationTokenSource();

        var token = _cancellationTokenSource.Token;

        Task.Run(async () =>
        {
            int count = 0;
            int totalItems = 0;

            foreach (var category in Categories)
            {
                if (token.IsCancellationRequested) break;

                category.IsScanning = true;

                await Task.Run(() => _registryService.ScanCategory(category, null), token);

                category.IsScanning = false;
                count++;
                totalItems += category.ItemCount;
                Progress = (double)count / Categories.Count * 100;

                // Update status on UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"Tarandi: {category.Name} ({category.ItemCount} oge)";
                });
            }

            IsScanning = false;
            StatusMessage = $"Tarama tamamlandi. {totalItems} toplam oge bulundu.";

            // Select first non-empty category
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var firstNonEmpty = Categories.FirstOrDefault(c => c.ItemCount > 0);
                if (firstNonEmpty != null && SelectedCategory == null)
                {
                    SelectedCategory = firstNonEmpty;
                }
                else if (SelectedCategory != null)
                {
                    // Refresh selected category items
                    UpdateCategoryItems();
                }
            });
        }, token);
    }

    private void CleanSelected()
    {
        var selectedItems = Categories.SelectMany(c => c.Items.Where(i => i.IsSelected)).ToList();
        if (selectedItems.Count == 0)
        {
            StatusMessage = "Temizlenecek oge secilmedi.";
            return;
        }

        IsCleaning = true;
        StatusMessage = "Temizleniyor...";
        Progress = 0;
        _cancellationTokenSource = new CancellationTokenSource();

        var token = _cancellationTokenSource.Token;

        Task.Run(async () =>
        {
            int deleted = 0;
            int failed = 0;

            // Create backup first
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = "Yedek olusturuluyor...";
            });
            var backup = _registryService.CreateBackup("Otomatik Yedek", selectedItems);

            // Delete items
            foreach (var item in selectedItems)
            {
                if (token.IsCancellationRequested) break;

                var success = await Task.Run(() => _registryService.DeleteRegistryItem(item, null), token);

                if (success) deleted++;
                else failed++;

                Progress = (double)(deleted + failed) / selectedItems.Count * 100;
            }

            // Refresh all categories
            foreach (var category in Categories)
            {
                _registryService.ScanCategory(category, null);
            }

            IsCleaning = false;
            StatusMessage = $"Tamamlandi. {deleted} silindi, {failed} basarisiz.";

            // Refresh selected category
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateCategoryItems();
                OnPropertyChanged(nameof(TotalSelectedCount));
                OnPropertyChanged(nameof(TotalOrphanedCount));
                OnPropertyChanged(nameof(TotalInvalidCount));
            });

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

        var backup = _registryService.CreateBackup("Manuel Yedek", allItems);

        IsBackingUp = false;
        StatusMessage = $"Yedek olusturuldu!";
        LoadBackups();
    }

    private void RestoreBackup()
    {
        if (SelectedBackup == null) return;

        StatusMessage = "Yedek geri yukleniyor...";
        _registryService.RestoreBackup(SelectedBackup);
        StatusMessage = "Yedek geri yuklendi. Bilgisayari yeniden baslatin.";
    }

    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        IsScanning = false;
        IsCleaning = false;
        StatusMessage = "Islem iptal edildi.";
    }
}
