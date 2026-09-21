using Drive.Application.Common.Models;
using Drive.Application.Features.DriveItems.Commands.HardDeleteExpiredItems;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Drive.Infrastructure.Services;

public class AutomaticHardDeleteBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<TrashSettings> _trashSettings;
    private readonly ILogger<AutomaticHardDeleteBackgroundService> _logger;

    public AutomaticHardDeleteBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<TrashSettings> trashSettings,
        ILogger<AutomaticHardDeleteBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _trashSettings = trashSettings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = Math.Max(1, _trashSettings.Value.CleanupIntervalHours);
        var interval = TimeSpan.FromHours(intervalHours);

        _logger.LogInformation(
            "AutomaticHardDeleteBackgroundService started. Interval: {Hours}h, Retention: {Days}d.",
            intervalHours,
            _trashSettings.Value.RetentionDays);

        using var timer = new PeriodicTimer(interval);

        try
        {
            // Run on startup
            await RunCleanupAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCleanupAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("AutomaticHardDeleteBackgroundService is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in AutomaticHardDeleteBackgroundService.");
        }
    }

    private async Task RunCleanupAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var deletedCount = await sender.Send(new HardDeleteExpiredItemsCommand(), stoppingToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation("Automatic hard-delete purged {Count} expired items.", deletedCount);
            }
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Error occurred during automated hard-delete execution.");
        }
    }
}
