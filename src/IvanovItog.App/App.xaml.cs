using System;
using System.IO;
using System.Windows;
using IvanovItog.App.Services;
using IvanovItog.App.ViewModels;
using IvanovItog.App.Views;
using IvanovItog.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace IvanovItog.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                var connectionString = BuildSqliteConnectionString(context.Configuration.GetConnectionString("Default"));
                services.AddInfrastructure(connectionString, Log.Logger);

                services.AddSingleton<NavigationService>();
                services.AddSingleton<DialogService>();
                services.AddSingleton<SessionContext>();
                services.AddSingleton<LocalSettingsService>();
                services.AddSingleton<TrayNotificationService>();

                services.AddTransient<LoginViewModel>();
                services.AddTransient<RequestsViewModel>();
                services.AddTransient<RatingViewModel>();
                services.AddTransient<AnalyticsViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<RequestEditorViewModel>();
                services.AddTransient<RegistrationViewModel>();
                services.AddTransient<UserManagementViewModel>();
                services.AddTransient<UserEditorViewModel>();

                services.AddTransient<LoginView>();
                services.AddTransient<RequestsView>();
                services.AddTransient<RatingView>();
                services.AddTransient<AnalyticsView>();
                services.AddTransient<SettingsView>();
                services.AddTransient<RequestEditorView>();
                services.AddTransient<RegistrationView>();
                services.AddTransient<UserManagementView>();
                services.AddTransient<UserEditorView>();
            })
            .Build();

        await _host.StartAsync();

        var loginView = _host.Services.GetRequiredService<LoginView>();
        loginView.DataContext = _host.Services.GetRequiredService<LoginViewModel>();
        MainWindow = loginView;
        loginView.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
}

    private static string BuildSqliteConnectionString(string? configuredConnectionString)
    {
        var defaultDataSource = Path.Combine(AppContext.BaseDirectory, "ivanov_itog.db");

        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return $"Data Source={defaultDataSource}";
        }

        var builder = new SqliteConnectionStringBuilder(configuredConnectionString);

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            builder.DataSource = defaultDataSource;
        }
        else if (!Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);
        }

        return builder.ToString();
    }
}
