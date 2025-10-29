using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.App.Services;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Interfaces;

namespace IvanovItog.App.ViewModels;

public partial class RegistrationViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly DialogService _dialogService;

    public event EventHandler<bool>? CloseRequested;

    public User? RegisteredUser { get; private set; }

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public IAsyncRelayCommand RegisterCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public RegistrationViewModel(IAuthService authService, DialogService dialogService)
    {
        _authService = authService;
        _dialogService = dialogService;
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, () => !IsBusy);
        CancelCommand = new RelayCommand(Cancel);
    }

    partial void OnIsBusyChanged(bool value)
    {
        RegisterCommand.NotifyCanExecuteChanged();
    }

    private async Task RegisterAsync()
    {
        if (IsBusy)
        {
            return;
        }

        RegisteredUser = null;

        if (string.IsNullOrWhiteSpace(Login))
        {
            _dialogService.ShowError("Логин обязателен");
            return;
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            _dialogService.ShowError("Имя пользователя обязательно");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            _dialogService.ShowError("Пароль обязателен");
            return;
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            _dialogService.ShowError("Пароли не совпадают");
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _authService.RegisterUserAsync(Login, DisplayName, Password);
            if (!result.IsSuccess)
            {
                var message = result.Error switch
                {
                    "UserAlreadyExists" => "Пользователь с таким логином уже существует",
                    "PasswordRequired" => "Пароль обязателен",
                    _ => result.Error ?? "Не удалось создать пользователя"
                };
                _dialogService.ShowError(message);
                return;
            }

            if (result.Value is not null)
            {
                RegisteredUser = result.Value;
            }

            _dialogService.ShowInfo("Аккаунт успешно создан");
            CloseRequested?.Invoke(this, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Cancel()
    {
        RegisteredUser = null;
        CloseRequested?.Invoke(this, false);
    }
}
