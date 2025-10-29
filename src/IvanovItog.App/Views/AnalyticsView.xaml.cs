using System.Windows;

namespace IvanovItog.App.Views;

public partial class AnalyticsView : Window
{
    public AnalyticsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.AnalyticsViewModel viewModel)
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
        }
    }
}
