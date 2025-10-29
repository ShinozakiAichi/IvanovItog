using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.App.Helpers;
using IvanovItog.App.Services;
using IvanovItog.App.Views;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Notification = IvanovItog.Domain.Entities.Notification;
using Application = System.Windows.Application;

namespace IvanovItog.App.ViewModels;

public partial class RequestsViewModel : ObservableObject
{
    private readonly IRequestService _requestService;
    private readonly ILookupService _lookupService;
    private readonly SessionContext _sessionContext;
    private readonly DialogService _dialogService;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notificationService;
    private readonly TrayNotificationService _trayNotificationService;

    public ObservableCollection<Request> Requests { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Status> Statuses { get; } = new();

    [ObservableProperty]
    private Request? _selectedRequest;

    [ObservableProperty]
    private int? _selectedCategoryId;

    [ObservableProperty]
    private int? _selectedStatusId;

    [ObservableProperty]
    private Priority? _selectedPriority;

    [ObservableProperty]
    private DateTime? _createdFrom;

    [ObservableProperty]
    private DateTime? _createdTo;

    [ObservableProperty]
    private string? _search;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _canManageUsers;

    public IReadOnlyCollection<Priority> Priorities { get; private set; } = Array.Empty<Priority>();

    public IAsyncRelayCommand InitializeCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand ClearFiltersCommand { get; }
    public IAsyncRelayCommand AssignToMeCommand { get; }
    public IAsyncRelayCommand CloseRequestCommand { get; }
    public IAsyncRelayCommand CreateRequestCommand { get; }
    public IAsyncRelayCommand EditRequestCommand { get; }
    public IAsyncRelayCommand DeleteRequestCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IRelayCommand LogoutCommand { get; }
    public IRelayCommand OpenRatingCommand { get; }
    public IRelayCommand OpenAnalyticsCommand { get; }
    public IRelayCommand OpenSettingsCommand { get; }
    public IRelayCommand OpenUserManagementCommand { get; }

    public RequestsViewModel(
        IRequestService requestService,
        ILookupService lookupService,
        SessionContext sessionContext,
        DialogService dialogService,
        IServiceProvider serviceProvider,
        INotificationService notificationService,
        TrayNotificationService trayNotificationService)
    {
        _requestService = requestService;
        _lookupService = lookupService;
        _sessionContext = sessionContext;
        _dialogService = dialogService;
        _serviceProvider = serviceProvider;
        _notificationService = notificationService;
        _trayNotificationService = trayNotificationService;

        InitializeCommand = new AsyncRelayCommand(InitializeAsync);
        RefreshCommand = new AsyncRelayCommand(LoadRequestsAsync, () => !IsBusy);
        ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync, () => !IsBusy);
        AssignToMeCommand = new AsyncRelayCommand(AssignToMeAsync, CanModifyRequest);
        CloseRequestCommand = new AsyncRelayCommand(CloseRequestAsync, CanModifyRequest);
        CreateRequestCommand = new AsyncRelayCommand(CreateRequestAsync, () => !IsBusy);
        EditRequestCommand = new AsyncRelayCommand(EditRequestAsync, () => SelectedRequest is not null && !IsBusy);
        DeleteRequestCommand = new AsyncRelayCommand(DeleteRequestAsync, () => SelectedRequest is not null && !IsBusy);
        ExportCommand = new AsyncRelayCommand(ExportAsync, () => Requests.Any());
        LogoutCommand = new RelayCommand(Logout);
        OpenRatingCommand = new RelayCommand(OpenRating);
        OpenAnalyticsCommand = new RelayCommand(OpenAnalytics);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenUserManagementCommand = new RelayCommand(OpenUserManagement, () => CanManageUsers);
    }

    partial void OnSelectedRequestChanged(Request? value)
    {
        AssignToMeCommand.NotifyCanExecuteChanged();
        CloseRequestCommand.NotifyCanExecuteChanged();
        EditRequestCommand.NotifyCanExecuteChanged();
        DeleteRequestCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        RefreshCommand.NotifyCanExecuteChanged();
        ClearFiltersCommand.NotifyCanExecuteChanged();
        AssignToMeCommand.NotifyCanExecuteChanged();
        CloseRequestCommand.NotifyCanExecuteChanged();
        CreateRequestCommand.NotifyCanExecuteChanged();
        EditRequestCommand.NotifyCanExecuteChanged();
        DeleteRequestCommand.NotifyCanExecuteChanged();
    }

    partial void OnCanManageUsersChanged(bool value)
    {
        OpenUserManagementCommand.NotifyCanExecuteChanged();
    }

    private bool CanModifyRequest() => SelectedRequest is not null && !IsBusy;

    private async Task InitializeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await LoadLookupsAsync();
            await LoadRequestsAsync();
            CanManageUsers = _sessionContext.CurrentUser?.Role == Role.Admin;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadLookupsAsync()
    {
        Categories.Clear();
        Statuses.Clear();

        var categories = await _lookupService.GetCategoriesAsync();
        foreach (var category in categories)
        {
            Categories.Add(category);
        }

        var statuses = await _lookupService.GetStatusesAsync();
        foreach (var status in statuses)
        {
            Statuses.Add(status);
        }

        Priorities = _lookupService.GetPriorities();
        OnPropertyChanged(nameof(Priorities));
    }

    private async Task LoadRequestsAsync()
    {
        try
        {
            IsBusy = true;
            Requests.Clear();
            var filter = new RequestFilter(
                SelectedCategoryId,
                SelectedStatusId,
                SelectedPriority,
                CreatedFrom,
                CreatedTo,
                Search);

            var requests = await _requestService.GetAsync(filter);
            foreach (var request in requests)
            {
                Requests.Add(request);
            }
            ExportCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Не удалось загрузить заявки: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ClearFiltersAsync()
    {
        SelectedCategoryId = null;
        SelectedStatusId = null;
        SelectedPriority = null;
        CreatedFrom = null;
        CreatedTo = null;
        Search = null;
        await LoadRequestsAsync();
    }

    private async Task AssignToMeAsync()
    {
        if (SelectedRequest is null)
        {
            return;
        }

        if (_sessionContext.CurrentUser is null)
        {
            _dialogService.ShowError("Не авторизован");
            return;
        }

        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _requestService.AssignAsync(SelectedRequest.Id, _sessionContext.CurrentUser.Id);
            if (!result.IsSuccess)
            {
                _dialogService.ShowError(result.Error ?? "Ошибка назначения");
                return;
            }

            await _notificationService.LogNotificationAsync(new Notification
            {
                Text = $"Вам назначена заявка {SelectedRequest.Title}",
                Type = "Assigned",
                UserId = _sessionContext.CurrentUser.Id
            });
            _trayNotificationService.ShowInfo($"Вам назначена заявка {SelectedRequest.Title}");

            await LoadRequestsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CloseRequestAsync()
    {
        if (SelectedRequest is null)
        {
            return;
        }

        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _requestService.CloseAsync(SelectedRequest.Id);
            if (!result.IsSuccess)
            {
                _dialogService.ShowError(result.Error ?? "Ошибка закрытия");
                return;
            }

            await _notificationService.LogNotificationAsync(new Notification
            {
                Text = $"Заявка {SelectedRequest.Title} закрыта",
                Type = "Closed",
                UserId = SelectedRequest.AssignedToId
            });
            _trayNotificationService.ShowInfo($"Заявка {SelectedRequest.Title} закрыта");

            await LoadRequestsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateRequestAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var editor = _serviceProvider.GetRequiredService<RequestEditorView>();
        var vm = _serviceProvider.GetRequiredService<RequestEditorViewModel>();
        await vm.InitializeAsync();
        vm.PrepareForCreate();
        editor.DataContext = vm;
        editor.Owner = Application.Current.MainWindow;
        if (editor.ShowDialog() == true)
        {
            await LoadRequestsAsync();
        }
    }

    private async Task EditRequestAsync()
    {
        if (SelectedRequest is null)
        {
            return;
        }

        if (IsBusy)
        {
            return;
        }

        var editor = _serviceProvider.GetRequiredService<RequestEditorView>();
        var vm = _serviceProvider.GetRequiredService<RequestEditorViewModel>();
        await vm.InitializeAsync();
        vm.PrepareForEdit(SelectedRequest);
        editor.DataContext = vm;
        editor.Owner = Application.Current.MainWindow;
        if (editor.ShowDialog() == true)
        {
            await LoadRequestsAsync();
        }
    }

    private async Task DeleteRequestAsync()
    {
        if (SelectedRequest is null)
        {
            return;
        }

        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _requestService.DeleteAsync(SelectedRequest.Id);
            if (!result.IsSuccess)
            {
                _dialogService.ShowError(result.Error ?? "Не удалось удалить заявку");
                return;
            }

            await LoadRequestsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task ExportAsync()
    {
        try
        {
            var path = CsvExporter.ExportRequests(Requests);
            _dialogService.ShowInfo($"Экспорт выполнен: {path}");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Ошибка экспорта: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private void Logout()
    {
        _sessionContext.CurrentUser = null;
        Application.Current.Dispatcher.Invoke(() =>
        {
            var navigation = _serviceProvider.GetRequiredService<NavigationService>();
            navigation.Navigate<LoginView, LoginViewModel>();
        });
    }

    private void OpenRating()
    {
        var view = _serviceProvider.GetRequiredService<RatingView>();
        view.DataContext = _serviceProvider.GetRequiredService<RatingViewModel>();
        view.Owner = Application.Current.MainWindow;
        view.ShowDialog();
    }

    private void OpenAnalytics()
    {
        var view = _serviceProvider.GetRequiredService<AnalyticsView>();
        view.DataContext = _serviceProvider.GetRequiredService<AnalyticsViewModel>();
        view.Owner = Application.Current.MainWindow;
        view.ShowDialog();
    }

    private void OpenSettings()
    {
        var view = _serviceProvider.GetRequiredService<SettingsView>();
        view.DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>();
        view.Owner = Application.Current.MainWindow;
        view.ShowDialog();
    }

    private void OpenUserManagement()
    {
        if (!CanManageUsers)
        {
            _dialogService.ShowError("Недостаточно прав");
            return;
        }

        var view = _serviceProvider.GetRequiredService<UserManagementView>();
        view.DataContext = _serviceProvider.GetRequiredService<UserManagementViewModel>();
        view.Owner = Application.Current.MainWindow;
        view.ShowDialog();
    }
}
