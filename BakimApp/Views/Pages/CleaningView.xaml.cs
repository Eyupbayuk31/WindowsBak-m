using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BakimApp.Models;
using BakimApp.ViewModels;

namespace BakimApp.Views.Pages;

public partial class CleaningView : UserControl
{
    public CleaningView()
    {
        InitializeComponent();
    }

    private void Category_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is CleaningCategory category)
        {
            if (DataContext is CleaningViewModel vm)
            {
                vm.SelectCategoryCommand.Execute(category);
            }
        }
    }
}
