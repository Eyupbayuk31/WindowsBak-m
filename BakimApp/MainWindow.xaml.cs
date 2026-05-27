using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BakimApp.ViewModels;
using BakimApp.Views.Pages;
using BakimApp.Helpers;

namespace BakimApp;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        MicaBackdrop.ApplyMica(this);
        WindowState = WindowState.Maximized;
        UpdateContent();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _viewModel.Cleanup();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentViewModel))
        {
            UpdateContent();
        }
    }

    private void UpdateContent()
    {
        if (_viewModel.CurrentViewModel is DashboardViewModel)
        {
            ContentArea.Content = new DashboardView { DataContext = _viewModel.DashboardViewModel };
        }
        else if (_viewModel.CurrentViewModel is CleaningViewModel)
        {
            ContentArea.Content = new CleaningView { DataContext = _viewModel.CleaningViewModel };
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeRestore();
        }
        else
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        MaximizeRestore();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MaximizeRestore()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }
}
