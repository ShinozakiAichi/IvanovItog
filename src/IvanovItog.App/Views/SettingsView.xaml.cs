using System.Windows;

namespace IvanovItog.App.Views;

public partial class SettingsView : Window
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel viewModel)
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
        }
    }
}
