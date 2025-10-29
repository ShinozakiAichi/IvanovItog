using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.App.Services;
using IvanovItog.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Application = System.Windows.Application;

namespace IvanovItog.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly NavigationService _navigationService;
    private readonly DialogService _dialogService;
    private readonly SessionContext _sessionContext;
    private readonly IServiceProvider _serviceProvider;

    public event EventHandler? RequestPasswordClear;

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public IAsyncRelayCommand LoginCommand { get; }
    public IRelayCommand OpenRegistrationCommand { get; }

    public LoginViewModel(IAuthService authService, NavigationService navigationService, DialogService dialogService, SessionContext sessionContext, IServiceProvider serviceProvider)
    {
        _authService = authService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _sessionContext = sessionContext;
        _serviceProvider = serviceProvider;
        LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, () => !IsBusy);
        OpenRegistrationCommand = new RelayCommand(OpenRegistration);
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

    private void OpenRegistration()
    {
        var view = _serviceProvider.GetRequiredService<Views.RegistrationView>();
        var vm = _serviceProvider.GetRequiredService<RegistrationViewModel>();
        view.DataContext = vm;
        view.Owner = Application.Current?.MainWindow;
        var result = view.ShowDialog();
        if (result == true && vm.RegisteredUser is not null)
        {
            _sessionContext.CurrentUser = vm.RegisteredUser;
            _navigationService.Navigate<Views.RequestsView, RequestsViewModel>();
        }
    }
}
