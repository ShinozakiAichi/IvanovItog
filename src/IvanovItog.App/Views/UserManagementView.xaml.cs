using System.Windows;
using IvanovItog.App.ViewModels;

namespace IvanovItog.App.Views;

public partial class UserManagementView : Window
{
    public UserManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel viewModel)
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
        }
    }
}
