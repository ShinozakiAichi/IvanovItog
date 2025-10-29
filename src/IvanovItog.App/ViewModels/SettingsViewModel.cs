using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanovItog.App.Services;

namespace IvanovItog.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly LocalSettingsService _settingsService;

    public ObservableCollection<string> Themes { get; } = new(["Light", "Dark"]);

    [ObservableProperty]
    private string _selectedTheme = "Light";

    [ObservableProperty]
    private string _attachmentsPath = AppUserSettings.Default.AttachmentsPath;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private bool _isBusy;

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }

    public SettingsViewModel(LocalSettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
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
