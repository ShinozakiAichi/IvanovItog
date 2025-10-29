using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.App.Services;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;
using IvanovItog.Shared;

namespace IvanovItog.App.ViewModels;

public partial class RequestEditorViewModel : ObservableObject
{
    private readonly IRequestService _requestService;
    private readonly ILookupService _lookupService;
    private readonly SessionContext _sessionContext;
    private readonly DialogService _dialogService;

    private Request? _editingRequest;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Status> Statuses { get; } = new();
    public IReadOnlyCollection<Priority> Priorities { get; private set; } = Array.Empty<Priority>();

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int? _categoryId;

    [ObservableProperty]
    private Priority _priority = Priority.Medium;

    [ObservableProperty]
    private int? _statusId;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isEditMode;

    public event EventHandler? Saved;

    public IAsyncRelayCommand SaveCommand { get; }

    public RequestEditorViewModel(IRequestService requestService, ILookupService lookupService, SessionContext sessionContext, DialogService dialogService)
    {
        _requestService = requestService;
        _lookupService = lookupService;
        _sessionContext = sessionContext;
        _dialogService = dialogService;
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
    }

    public async Task InitializeAsync()
    {
        Categories.Clear();
        Statuses.Clear();
        foreach (var category in await _lookupService.GetCategoriesAsync())
        {
            Categories.Add(category);
        }

        foreach (var status in await _lookupService.GetStatusesAsync())
        {
            Statuses.Add(status);
        }

        Priorities = _lookupService.GetPriorities();
        OnPropertyChanged(nameof(Priorities));
    }

    public void PrepareForCreate()
    {
        _editingRequest = null;
        IsEditMode = false;
        Title = string.Empty;
        Description = string.Empty;
        CategoryId = Categories.FirstOrDefault()?.Id;
        StatusId = Statuses.FirstOrDefault()?.Id;
        Priority = Priority.Medium;
    }

    public void PrepareForEdit(Request request)
    {
        _editingRequest = request;
        IsEditMode = true;
        Title = request.Title;
        Description = request.Description;
        CategoryId = request.CategoryId;
        Priority = request.Priority;
        StatusId = request.StatusId;
    }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Description) || !CategoryId.HasValue || !StatusId.HasValue)
        {
            _dialogService.ShowError("Заполните все поля");
            return;
        }

        try
        {
            IsBusy = true;
            Result<Request> result;
            if (_editingRequest is null)
            {
                if (_sessionContext.CurrentUser is null)
                {
                    _dialogService.ShowError("Не авторизован");
                    return;
                }

                var request = new Request
                {
                    Title = Title,
                    Description = Description,
                    CategoryId = CategoryId.Value,
                    StatusId = StatusId.Value,
                    Priority = Priority,
                    CreatedById = _sessionContext.CurrentUser.Id
                };

                result = await _requestService.CreateAsync(request);
            }
            else
            {
                _editingRequest.Title = Title;
                _editingRequest.Description = Description;
                _editingRequest.CategoryId = CategoryId!.Value;
                _editingRequest.Priority = Priority;
                _editingRequest.StatusId = StatusId!.Value;
                result = await _requestService.UpdateAsync(_editingRequest);
            }

            if (!result.IsSuccess)
            {
                _dialogService.ShowError(result.Error ?? "Не удалось сохранить заявку");
                return;
            }

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Ошибка сохранения: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
