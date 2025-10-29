using System;
using System.Windows;
using IvanovItog.App.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace IvanovItog.App.Views;

public partial class AnalyticsView : Window
{
    private readonly AnalyticsViewModel _viewModel;

    public AnalyticsView(AnalyticsViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        try
        {
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось загрузить аналитику: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
