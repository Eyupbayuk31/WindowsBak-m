using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using BakimApp.Models;
using BakimApp.Services;

namespace BakimApp.ViewModels;

public class RegistryViewModel : BaseViewModel
{
    private readonly RegistryService _registryService;
    private ObservableCollection<RegistryCategory> _categories = new();
    private ObservableCollection<RegistryItem> _selectedItems = new();
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
        CleanSelectedCommand = new RelayCommand(CleanSelected, () => SelectedItems.Count > 0 && !IsCleaning);
        BackupCommand = new RelayCommand(CreateBackup, () => !IsBackingUp);
        RestoreCommand = new RelayCommand(RestoreBackup, () => SelectedBackup != null);
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
        CancelCommand = new RelayCommand(Cancel);

        InitializeCategories();
        LoadBackups();
    }

    public ObservableCollection<RegistryCategory> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public ObservableCollection<RegistryItem> SelectedItems
    {
        get => _selectedItems;
        set => SetProperty(ref _selectedItems, value);
    }

    public RegistryCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                OnPropertyChanged(nameof(CategoryItems));
                OnPropertyChanged(nameof(CategoryItemCount));
            }
        }
    }

    public ObservableCollection<RegistryItem> CategoryItems
    {
        get
        {
            if (SelectedCategory == null)
                return new ObservableCollection<RegistryItem>();

            return new ObservableCollection<RegistryItem>(SelectedCategory.Items);
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
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand CancelCommand { get; }

    private void InitializeCategories()
    {
        foreach (var category in _registryService.GetCategories())
        {
            category.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(RegistryCategory.IsSelected))
                {
                    UpdateSelectedItems();
                    OnPropertyChanged(nameof(TotalSelectedCount));
                    OnPropertyChanged(nameof(TotalOrphanedCount));
                    OnPropertyChanged(nameof(TotalInvalidCount));
                }
            };
            Categories.Add(category);
        }
    }

    private void UpdateSelectedItems()
    {
        SelectedItems = new ObservableCollection<RegistryItem>(
            Categories.Where(c => c.IsSelected).SelectMany(c => c.Items.Where(i => i.IsSelected)));
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

        Task.Run(async () =>
        {
            int count = 0;
            foreach (var category in Categories)
            {
                if (token.IsCancellationRequested) break;

                category.IsScanning = true;
                var progress = new Progress<string>(msg => StatusMessage = msg);

                await Task.Run(() => _registryService.ScanCategory(category, progress), token);

                category.IsScanning = false;
                count++;
                Progress = (double)count / Categories.Count * 100;
            }

            IsScanning = false;
            StatusMessage = $"Tarama tamamlandi. {Categories.Sum(c => c.ItemCount)} oge bulundu.";
        }, token);
    }

    private void CleanSelected()
    {
        IsCleaning = true;
        StatusMessage = "Temizleniyor...";
        _cancellationTokenSource = new CancellationTokenSource();
        Progress = 0;

        var allItems = Categories.SelectMany(c => c.Items.Where(i => i.IsSelected)).ToList();
        if (allItems.Count == 0)
        {
            IsCleaning = false;
            StatusMessage = "Temizlenecek oge yok.";
            return;
        }

        var token = _cancellationTokenSource.Token;

        Task.Run(async () =>
        {
            int count = 0;
            int deleted = 0;
            int failed = 0;

            // First create backup
            StatusMessage = "Yedek olusturuluyor...";
            var backup = _registryService.CreateBackup("Otomatik yedek", allItems);

            foreach (var item in allItems)
            {
                if (token.IsCancellationRequested) break;

                var progress = new Progress<string>(msg => StatusMessage = msg);

                var success = await Task.Run(() => _registryService.DeleteRegistryItem(item, progress), token);

                if (success)
                    deleted++;
                else
                    failed++;

                count++;
                Progress = (double)count / allItems.Count * 100;
            }

            // Refresh the list
            await Task.Run(() =>
            {
                foreach (var category in Categories)
                {
                    _registryService.ScanCategory(category, null);
                }
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
        StatusMessage = $"Yedek olusturuldu: {backup.FilePath}";
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
        foreach (var category in Categories)
        {
            foreach (var item in category.Items)
            {
                item.IsSelected = true;
            }
            category.IsSelected = true;
        }
        UpdateSelectedItems();
    }

    private void DeselectAll()
    {
        foreach (var category in Categories)
        {
            foreach (var item in category.Items)
            {
                item.IsSelected = false;
            }
            category.IsSelected = false;
        }
        UpdateSelectedItems();
    }

    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        IsScanning = false;
        IsCleaning = false;
        StatusMessage = "Islem iptal edildi.";
    }
}
