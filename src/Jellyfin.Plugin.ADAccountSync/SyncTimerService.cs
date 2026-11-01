using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AdAccountDisableSync;

/// <summary>Registers the plugin's internal timer with Jellyfin's application lifetime.</summary>
public sealed class ServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.TryAddSingleton(TimeProvider.System);
        serviceCollection.AddHostedService<SyncTimerService>();
    }
}

/// <summary>Queues synchronization without a Jellyfin interval trigger.</summary>
internal sealed class SyncTimerService(
    ITaskManager taskManager,
    IHostApplicationLifetime lifetime,
    TimeProvider timeProvider,
    ILogger<SyncTimerService> logger) : BackgroundService
{
    private readonly SemaphoreSlim _settingsChanged = new(0, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Plugin? plugin = null;
        try
        {
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = lifetime.ApplicationStarted.Register(() => started.TrySetResult());
            await started.Task.WaitAsync(stoppingToken).ConfigureAwait(false);
            plugin = Plugin.Instance ?? throw new InvalidOperationException("AD Account Sync is unavailable.");

            var worker = taskManager.ScheduledTasks.FirstOrDefault(task => task.ScheduledTask is AdAccountDisableSyncTask)
                ?? throw new InvalidOperationException("AD Account Sync scheduled task is unavailable.");
            var remaining = worker.Triggers.Where(trigger => trigger.Type != TaskTriggerInfoType.IntervalTrigger).ToArray();
            if (remaining.Length != worker.Triggers.Count())
            {
                // The setter persists the triggers and reloads them through Jellyfin's scheduler.
                worker.Triggers = remaining;
                logger.LogInformation("AD Account Sync removed its old scheduled interval trigger. Automatic sync now uses the plugin timer.");
            }

            plugin.ScheduleChanged += OnSettingsChanged;
            logger.LogInformation("AD Account Sync timer started; interval {Minutes} minutes.", plugin.Configuration.SyncIntervalMinutes);
            while (!stoppingToken.IsCancellationRequested)
            {
                var settings = plugin.Configuration;
                var minutes = Math.Clamp(settings.SyncIntervalMinutes, 5, 1440);
                var changed = false;
                using (var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken))
                {
                    try
                    {
                        await _settingsChanged.WaitAsync(waitCancellation.Token)
                            .WaitAsync(TimeSpan.FromMinutes(minutes), timeProvider, stoppingToken).ConfigureAwait(false);
                        changed = true;
                    }
                    catch (TimeoutException)
                    {
                        // The configured interval has elapsed.
                    }
                    finally
                    {
                        await waitCancellation.CancelAsync().ConfigureAwait(false);
                    }
                }
                if (changed)
                {
                    continue;
                }

                if (plugin.Configuration.Enabled)
                {
                    try
                    {
                        // Use Jellyfin's worker for progress, cancellation, and non-overlapping manual/timed runs.
                        taskManager.QueueIfNotRunning<AdAccountDisableSyncTask>();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "AD Account Sync timer could not queue synchronization; it will retry at the next interval.");
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal server shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AD Account Sync timer failed to start. Check the task registration and trigger storage permissions. Manual sync remains available.");
        }
        finally
        {
            if (plugin is not null)
            {
                plugin.ScheduleChanged -= OnSettingsChanged;
            }
        }
    }

    private void OnSettingsChanged()
    {
        try
        {
            _settingsChanged.Release();
        }
        catch (SemaphoreFullException)
        {
            // A settings refresh is already pending.
        }
    }
}
