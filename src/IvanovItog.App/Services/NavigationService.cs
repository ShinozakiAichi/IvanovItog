using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Application = System.Windows.Application;

namespace IvanovItog.App.Services;

public class NavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _currentWindow;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Navigate<TView, TViewModel>()
        where TView : Window
        where TViewModel : notnull
    {
        var previousWindow = _currentWindow ?? Application.Current.MainWindow;
        if (previousWindow is not null)
        {
            previousWindow.Hide();
        }
        var view = _serviceProvider.GetRequiredService<TView>();
        view.DataContext = _serviceProvider.GetRequiredService<TViewModel>();
        view.Show();
        Application.Current.MainWindow = view;
        _currentWindow = view;
        if (previousWindow is not null && previousWindow != view)
        {
            previousWindow.Close();
        }
    }
}
