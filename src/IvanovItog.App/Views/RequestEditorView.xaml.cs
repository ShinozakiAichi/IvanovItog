using System.Linq;
using System.Windows;

namespace IvanovItog.App.Views;

public partial class RequestEditorView : Window
{
    public RequestEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.RequestEditorViewModel viewModel)
        {
            viewModel.Saved += OnSaved;
            if (!viewModel.Categories.Any() || !viewModel.Statuses.Any())
            {
                await viewModel.InitializeAsync();
            }
        }
    }

    private void OnSaved(object? sender, EventArgs e)
    {
        if (DataContext is ViewModels.RequestEditorViewModel viewModel)
        {
            viewModel.Saved -= OnSaved;
        }
        DialogResult = true;
        Close();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is ViewModels.RequestEditorViewModel viewModel)
        {
            viewModel.Saved -= OnSaved;
        }
    }
}
