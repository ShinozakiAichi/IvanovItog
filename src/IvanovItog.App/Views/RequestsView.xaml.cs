using System.Windows;

namespace IvanovItog.App.Views;

public partial class RequestsView : Window
{
    public RequestsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.RequestsViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }
}
