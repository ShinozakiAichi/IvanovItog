using System.Windows;
using System.Windows.Controls;
using IvanovItog.App.ViewModels;

namespace IvanovItog.App.Views;

public partial class SettingsView : Window
{
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SettingsViewModel oldVm)
        {
            oldVm.RequestPasswordClear -= OnRequestPasswordClear;
        }

        if (e.NewValue is SettingsViewModel newVm)
        {
            newVm.RequestPasswordClear += OnRequestPasswordClear;
        }
    }

    private void OnRequestPasswordClear(object? sender, EventArgs e)
    {
        CurrentPasswordBox.Password = string.Empty;
        NewPasswordBox.Password = string.Empty;
        ConfirmPasswordBox.Password = string.Empty;
    }

    private void OnCurrentPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.CurrentPassword = passwordBox.Password;
        }
    }

    private void OnNewPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.NewPassword = passwordBox.Password;
        }
    }

    private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.ConfirmPassword = passwordBox.Password;
        }
    }
}
