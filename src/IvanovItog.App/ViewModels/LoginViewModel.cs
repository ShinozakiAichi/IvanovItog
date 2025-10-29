using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.App.Services;
using IvanovItog.Domain.Interfaces;

namespace IvanovItog.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly NavigationService _navigationService;
    private readonly DialogService _dialogService;
    private readonly SessionContext _sessionContext;

    public event EventHandler? RequestPasswordClear;

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public IAsyncRelayCommand LoginCommand { get; }

    public LoginViewModel(IAuthService authService, NavigationService navigationService, DialogService dialogService, SessionContext sessionContext)
    {
        _authService = authService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _sessionContext = sessionContext;
        LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, () => !IsBusy);
    }

    partial void OnIsBusyChanged(bool value)
    {
        LoginCommand.NotifyCanExecuteChanged();
    }

    private async Task ExecuteLoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _authService.AuthenticateAsync(Login, Password);
            if (!result.IsSuccess || result.Value is null)
            {
                _dialogService.ShowError("Неверный логин или пароль");
                return;
            }

            _sessionContext.CurrentUser = result.Value;
            _navigationService.Navigate<Views.RequestsView, RequestsViewModel>();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Ошибка входа: {ex.Message}");
        }
        finally
        {
            Password = string.Empty;
            RequestPasswordClear?.Invoke(this, EventArgs.Empty);
            IsBusy = false;
        }
    }
}
