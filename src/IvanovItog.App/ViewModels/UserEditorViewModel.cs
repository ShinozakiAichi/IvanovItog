using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.App.Services;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;

namespace IvanovItog.App.ViewModels;

public partial class UserEditorViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly DialogService _dialogService;

    public event EventHandler<bool>? CloseRequested;

    [ObservableProperty]
    private int? _userId;

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private Role _selectedRole = Role.User;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isCreateMode;

    [ObservableProperty]
    private bool _isBusy;

    public IReadOnlyList<Role> Roles { get; } = Enum.GetValues<Role>();

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public UserEditorViewModel(IAuthService authService, DialogService dialogService)
    {
        _authService = authService;
        _dialogService = dialogService;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        CancelCommand = new RelayCommand(Cancel);
    }

    partial void OnIsBusyChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
    }

    public void InitializeForCreate()
    {
        Title = "Новый пользователь";
        UserId = null;
        Login = string.Empty;
        DisplayName = string.Empty;
        SelectedRole = Role.User;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        IsCreateMode = true;
    }

    public void InitializeForEdit(User user)
    {
        Title = "Редактирование пользователя";
        UserId = user.Id;
        Login = user.Login;
        DisplayName = user.DisplayName;
        SelectedRole = user.Role;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        IsCreateMode = false;
    }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

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

        try
        {
            IsBusy = true;

            if (IsCreateMode)
            {
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

                var user = new User
                {
                    Login = Login,
                    DisplayName = DisplayName,
                    Role = SelectedRole,
                    PasswordHash = string.Empty
                };

                var result = await _authService.CreateUserAsync(user, Password);
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

                _dialogService.ShowInfo("Пользователь успешно создан");
                CloseRequested?.Invoke(this, true);
                return;
            }

            if (UserId is null)
            {
                _dialogService.ShowError("Пользователь не найден");
                return;
            }

            var updateResult = await _authService.UpdateUserAsync(UserId.Value, Login, DisplayName, SelectedRole);
            if (!updateResult.IsSuccess)
            {
                var message = updateResult.Error switch
                {
                    "UserAlreadyExists" => "Пользователь с таким логином уже существует",
                    _ => updateResult.Error ?? "Не удалось обновить пользователя"
                };
                _dialogService.ShowError(message);
                return;
            }

            if (!string.IsNullOrWhiteSpace(Password) || !string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
                {
                    _dialogService.ShowError("Пароли не совпадают");
                    return;
                }

                var resetResult = await _authService.ResetPasswordAsync(UserId.Value, Password);
                if (!resetResult.IsSuccess)
                {
                    var message = resetResult.Error switch
                    {
                        "PasswordRequired" => "Пароль обязателен",
                        _ => resetResult.Error ?? "Не удалось обновить пароль"
                    };
                    _dialogService.ShowError(message);
                    return;
                }
            }

            _dialogService.ShowInfo("Данные пользователя обновлены");
            CloseRequested?.Invoke(this, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }
}
