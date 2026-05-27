using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BakimApp.Models;
using BakimApp.ViewModels;

namespace BakimApp.Views.Pages;

public partial class RegistryView : UserControl
{
    public RegistryView()
    {
        InitializeComponent();
    }

    private void CategoryCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is RegistryCategory category)
        {
            if (DataContext is RegistryViewModel vm)
            {
                vm.SelectCategoryCommand?.Execute(category);
            }
        }
    }
}
