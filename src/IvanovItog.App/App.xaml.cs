using System.Windows;
using IvanovItog.App.Services;
using IvanovItog.App.ViewModels;
using IvanovItog.App.Views;
using IvanovItog.Infrastructure;
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
                var connectionString = context.Configuration.GetConnectionString("Default") ?? "Data Source=ivanov_itog.db";
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

                services.AddTransient<LoginView>();
                services.AddTransient<RequestsView>();
                services.AddTransient<RatingView>();
                services.AddTransient<AnalyticsView>();
                services.AddTransient<SettingsView>();
                services.AddTransient<RequestEditorView>();
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
}
