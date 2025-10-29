using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.App.Services;
using IvanovItog.Domain.Interfaces;
using Application = System.Windows.Application;

namespace IvanovItog.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly LocalSettingsService _settingsService;
    private readonly IAuthService _authService;
    private readonly DialogService _dialogService;
    private readonly SessionContext _sessionContext;

    public ObservableCollection<string> Themes { get; } = new(["Light", "Dark"]);

    [ObservableProperty]
    private string _selectedTheme = "Light";

    [ObservableProperty]
    private string _attachmentsPath = AppUserSettings.Default.AttachmentsPath;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand ChangePasswordCommand { get; }

    public event EventHandler? RequestPasswordClear;

    public SettingsViewModel(LocalSettingsService settingsService, IAuthService authService, DialogService dialogService, SessionContext sessionContext)
    {
        _settingsService = settingsService;
        _authService = authService;
        _dialogService = dialogService;
        _sessionContext = sessionContext;
        LoadCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync, () => !IsBusy);
    }

    partial void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        ChangePasswordCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var settings = await _settingsService.LoadAsync();
            SelectedTheme = settings.Theme;
            AttachmentsPath = settings.AttachmentsPath;
            NotificationsEnabled = settings.NotificationsEnabled;
            ApplyTheme(settings.Theme);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            Directory.CreateDirectory(AttachmentsPath);
            var settings = new AppUserSettings(SelectedTheme, AttachmentsPath, NotificationsEnabled);
            await _settingsService.SaveAsync(settings);
            ApplyTheme(SelectedTheme);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ChangePasswordAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_sessionContext.CurrentUser is null)
        {
            _dialogService.ShowError("Пользователь не найден в сессии");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            _dialogService.ShowError("Новый пароль не может быть пустым");
            return;
        }

        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            _dialogService.ShowError("Пароли не совпадают");
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _authService.ChangePasswordAsync(_sessionContext.CurrentUser.Id, CurrentPassword, NewPassword);
            if (!result.IsSuccess)
            {
                var error = result.Error switch
                {
                    "InvalidCredentials" => "Неверный текущий пароль",
                    _ => result.Error ?? "Не удалось изменить пароль"
                };
                _dialogService.ShowError(error);
                return;
            }

            _dialogService.ShowInfo("Пароль успешно изменён");
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            RequestPasswordClear?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedThemeChanged(string value)
    {
        ApplyTheme(value);
    }

    private static void ApplyTheme(string theme)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var existingTheme = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.Contains("Light.xaml") == true || d.Source?.OriginalString.Contains("Dark.xaml") == true);
        if (existingTheme is not null)
        {
            dictionaries.Remove(existingTheme);
        }

        var resource = new ResourceDictionary { Source = new Uri($"Resources/{theme}.xaml", UriKind.Relative) };
        dictionaries.Insert(0, resource);
    }
}
