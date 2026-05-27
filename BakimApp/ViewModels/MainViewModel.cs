using System.Windows.Input;

namespace BakimApp.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentViewModel = null!;
    private string _currentPage = "Dashboard";
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly CleaningViewModel _cleaningViewModel;

    public MainViewModel()
    {
        _dashboardViewModel = new DashboardViewModel();
        _cleaningViewModel = new CleaningViewModel();

        _currentViewModel = _dashboardViewModel;

        NavigateToDashboardCommand = new RelayCommand(() => NavigateTo("Dashboard"));
        NavigateToCleaningCommand = new RelayCommand(() => NavigateTo("Cleaning"));
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
            }
        }
    }

    public bool IsDashboardSelected => CurrentPage == "Dashboard";
    public bool IsCleaningSelected => CurrentPage == "Cleaning";

    public DashboardViewModel DashboardViewModel => _dashboardViewModel;
    public CleaningViewModel CleaningViewModel => _cleaningViewModel;

    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToCleaningCommand { get; }

    private void NavigateTo(string page)
    {
        CurrentPage = page;
        CurrentViewModel = page switch
        {
            "Dashboard" => _dashboardViewModel,
            "Cleaning" => _cleaningViewModel,
            _ => _currentViewModel
        };
    }

    public void Cleanup()
    {
        _dashboardViewModel.StopTimer();
    }
}
