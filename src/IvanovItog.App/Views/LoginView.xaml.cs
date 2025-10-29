using System.Windows;
using System.Windows.Controls;
using IvanovItog.App.ViewModels;

namespace IvanovItog.App.Views;

public partial class LoginView : Window
{
    public LoginView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.LoginViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is LoginViewModel oldVm)
        {
            oldVm.RequestPasswordClear -= OnRequestPasswordClear;
        }

        if (e.NewValue is LoginViewModel newVm)
        {
            newVm.RequestPasswordClear += OnRequestPasswordClear;
        }
    }

    private void OnRequestPasswordClear(object? sender, EventArgs e)
    {
        PasswordBox.Password = string.Empty;
    }
}
