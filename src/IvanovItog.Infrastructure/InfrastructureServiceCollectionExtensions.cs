using FluentValidation;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Interfaces;
using IvanovItog.Infrastructure.Services;
using IvanovItog.Infrastructure.Seed;
using IvanovItog.Shared.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace IvanovItog.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, ILogger logger)
    {
        services.AddSingleton(logger);
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRequestService, RequestService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IValidator<User>, UserValidator>();
        services.AddScoped<RequestValidator>();

        services.AddHostedService<DatabaseInitializer>();

        return services;
    }
}

internal sealed class DatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedData.InitializeAsync(context, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
