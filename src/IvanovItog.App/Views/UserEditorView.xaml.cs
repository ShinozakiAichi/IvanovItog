using System.Windows;
using System.Windows.Controls;
using IvanovItog.App.ViewModels;

namespace IvanovItog.App.Views;

public partial class UserEditorView : Window
{
    public UserEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is UserEditorViewModel oldVm)
        {
            oldVm.CloseRequested -= OnCloseRequested;
        }

        if (e.NewValue is UserEditorViewModel newVm)
        {
            newVm.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, bool e)
    {
        DialogResult = e;
        Close();
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserEditorViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.Password = passwordBox.Password;
        }
    }

    private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserEditorViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.ConfirmPassword = passwordBox.Password;
        }
    }
}
