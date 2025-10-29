using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.App.Services;
using IvanovItog.App.Views;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Application = System.Windows.Application;

namespace IvanovItog.App.ViewModels;

public partial class UserManagementViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly DialogService _dialogService;
    private readonly SessionContext _sessionContext;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<User> Users { get; } = new();

    [ObservableProperty]
    private User? _selectedUser;

    [ObservableProperty]
    private bool _isBusy;

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand CreateUserCommand { get; }
    public IAsyncRelayCommand EditUserCommand { get; }
    public IAsyncRelayCommand DeleteUserCommand { get; }

    public UserManagementViewModel(IAuthService authService, DialogService dialogService, SessionContext sessionContext, IServiceProvider serviceProvider)
    {
        _authService = authService;
        _dialogService = dialogService;
        _sessionContext = sessionContext;
        _serviceProvider = serviceProvider;

        LoadCommand = new AsyncRelayCommand(LoadUsersAsync, () => !IsBusy);
        CreateUserCommand = new AsyncRelayCommand(CreateUserAsync, () => !IsBusy);
        EditUserCommand = new AsyncRelayCommand(EditUserAsync, CanEditUser);
        DeleteUserCommand = new AsyncRelayCommand(DeleteUserAsync, CanEditUser);
    }

    partial void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        CreateUserCommand.NotifyCanExecuteChanged();
        EditUserCommand.NotifyCanExecuteChanged();
        DeleteUserCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedUserChanged(User? value)
    {
        EditUserCommand.NotifyCanExecuteChanged();
        DeleteUserCommand.NotifyCanExecuteChanged();
    }

    private bool CanEditUser() => SelectedUser is not null && !IsBusy;

    private async Task LoadUsersAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            Users.Clear();
            var users = await _authService.GetUsersAsync();
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateUserAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var editor = _serviceProvider.GetRequiredService<UserEditorView>();
        var viewModel = _serviceProvider.GetRequiredService<UserEditorViewModel>();
        viewModel.InitializeForCreate();
        editor.DataContext = viewModel;
        editor.Owner = Application.Current?.MainWindow;
        if (editor.ShowDialog() == true)
        {
            await LoadUsersAsync();
        }
    }

    private async Task EditUserAsync()
    {
        if (SelectedUser is null || IsBusy)
        {
            return;
        }

        var editor = _serviceProvider.GetRequiredService<UserEditorView>();
        var viewModel = _serviceProvider.GetRequiredService<UserEditorViewModel>();
        viewModel.InitializeForEdit(SelectedUser);
        editor.DataContext = viewModel;
        editor.Owner = Application.Current?.MainWindow;
        if (editor.ShowDialog() == true)
        {
            await LoadUsersAsync();
        }
    }

    private async Task DeleteUserAsync()
    {
        if (SelectedUser is null || IsBusy)
        {
            return;
        }

        if (_sessionContext.CurrentUser?.Id == SelectedUser.Id)
        {
            _dialogService.ShowError("Нельзя удалить свою учётную запись во время работы");
            return;
        }

        if (SelectedUser.Role == Role.Admin && Users.Count(user => user.Role == Role.Admin) <= 1)
        {
            _dialogService.ShowError("Нельзя удалить последнего администратора");
            return;
        }

        if (!_dialogService.Confirm($"Удалить пользователя {SelectedUser.DisplayName}?"))
        {
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _authService.DeleteUserAsync(SelectedUser.Id);
            if (!result.IsSuccess)
            {
                var message = result.Error switch
                {
                    "UserHasRequests" => "Нельзя удалить пользователя, связанного с заявками",
                    _ => result.Error ?? "Не удалось удалить пользователя"
                };
                _dialogService.ShowError(message);
                return;
            }

            await LoadUsersAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
