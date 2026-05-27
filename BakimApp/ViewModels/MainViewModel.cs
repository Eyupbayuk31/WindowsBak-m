using System.Windows.Input;

namespace BakimApp.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentViewModel = null!;
    private string _currentPage = "Dashboard";
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly CleaningViewModel _cleaningViewModel;
    private readonly RegistryViewModel _registryViewModel;

    public MainViewModel()
    {
        _dashboardViewModel = new DashboardViewModel();
        _cleaningViewModel = new CleaningViewModel();
        _registryViewModel = new RegistryViewModel();

        _currentViewModel = _dashboardViewModel;

        NavigateToDashboardCommand = new RelayCommand(() => NavigateTo("Dashboard"));
        NavigateToCleaningCommand = new RelayCommand(() => NavigateTo("Cleaning"));
        NavigateToRegistryCommand = new RelayCommand(() => NavigateTo("Registry"));
    }

    public BaseViewModel CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public string CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(IsDashboardSelected));
                OnPropertyChanged(nameof(IsCleaningSelected));
                OnPropertyChanged(nameof(IsRegistrySelected));
            }
        }
    }

    public bool IsDashboardSelected => CurrentPage == "Dashboard";
    public bool IsCleaningSelected => CurrentPage == "Cleaning";
    public bool IsRegistrySelected => CurrentPage == "Registry";

    public DashboardViewModel DashboardViewModel => _dashboardViewModel;
    public CleaningViewModel CleaningViewModel => _cleaningViewModel;
    public RegistryViewModel RegistryViewModel => _registryViewModel;

    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToCleaningCommand { get; }
    public ICommand NavigateToRegistryCommand { get; }

    private void NavigateTo(string page)
    {
        CurrentPage = page;
        CurrentViewModel = page switch
        {
            "Dashboard" => _dashboardViewModel,
            "Cleaning" => _cleaningViewModel,
            "Registry" => _registryViewModel,
            _ => _currentViewModel
        };
    }

    public void Cleanup()
    {
        _dashboardViewModel.StopTimer();
    }
}