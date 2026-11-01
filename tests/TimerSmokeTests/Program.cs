using Jellyfin.Plugin.AdAccountDisableSync;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

var plugin = new Plugin();
using var started = new CancellationTokenSource();
var lifetime = new Mock<IHostApplicationLifetime>();
lifetime.SetupGet(x => x.ApplicationStarted).Returns(started.Token);
var worker = new Mock<IScheduledTaskWorker>();
worker.SetupGet(x => x.ScheduledTask).Returns(new AdAccountDisableSyncTask());
worker.SetupProperty(x => x.Triggers, new TaskTriggerInfo[]
{
    new() { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromMinutes(5).Ticks },
    new() { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromMinutes(15).Ticks },
    new() { Type = TaskTriggerInfoType.StartupTrigger },
    new() { Type = TaskTriggerInfoType.DailyTrigger, TimeOfDayTicks = TimeSpan.FromHours(3).Ticks }
});
var manager = new Mock<ITaskManager>();
IReadOnlyList<IScheduledTaskWorker> registeredTasks = [];
manager.SetupGet(x => x.ScheduledTasks).Returns(() => registeredTasks);
worker.SetupGet(x => x.State).Returns(TaskState.Idle);
var clock = new FakeTimeProvider();
using var service = new SyncTimerService(manager.Object, lifetime.Object, clock, NullLogger<SyncTimerService>.Instance);
await service.StartAsync(CancellationToken.None);
await Task.Delay(50);
if (worker.Object.Triggers.Count != 4) throw new Exception("Migration ran before application startup.");
started.Cancel();
await Task.Delay(50);
if (worker.Object.Triggers.Count != 4) throw new Exception("Migration ran before task registration.");
registeredTasks = new[] { worker.Object };
clock.Advance(TimeSpan.FromSeconds(1));
await Until(() => worker.Object.Triggers.Count == 1);
if (worker.Object.Triggers[0].Type != TaskTriggerInfoType.DailyTrigger) throw new Exception("Custom daily trigger was not preserved.");
plugin.ChangeSettings();
await Task.Delay(50);
manager.Verify(x => x.QueueScheduledTask(worker.Object.ScheduledTask, It.IsAny<TaskOptions>()), Times.Never);
clock.Advance(TimeSpan.FromMinutes(4));
await Task.Delay(50);
manager.Verify(x => x.QueueScheduledTask(worker.Object.ScheduledTask, It.IsAny<TaskOptions>()), Times.Never);
clock.Advance(TimeSpan.FromMinutes(1));
await Until(() => manager.Invocations.Any(x => x.Method.Name == "QueueScheduledTask"));
manager.Verify(x => x.QueueScheduledTask(worker.Object.ScheduledTask, It.IsAny<TaskOptions>()), Times.Once);
worker.SetupGet(x => x.State).Returns(TaskState.Running);
await Task.Delay(50);
clock.Advance(TimeSpan.FromMinutes(5));
await Task.Delay(50);
manager.Verify(x => x.QueueScheduledTask(worker.Object.ScheduledTask, It.IsAny<TaskOptions>()), Times.Once);
worker.SetupGet(x => x.State).Returns(TaskState.Idle);
plugin.Configuration.Enabled = false;
plugin.ChangeSettings();
await Task.Delay(50);
clock.Advance(TimeSpan.FromMinutes(5));
await Task.Delay(50);
manager.Verify(x => x.QueueScheduledTask(worker.Object.ScheduledTask, It.IsAny<TaskOptions>()), Times.Once);
plugin.Configuration.Enabled = true;
plugin.Configuration.SyncIntervalMinutes = 10;
plugin.ChangeSettings();
await Task.Delay(50);
clock.Advance(TimeSpan.FromMinutes(5));
await Task.Delay(50);
manager.Verify(x => x.QueueScheduledTask(worker.Object.ScheduledTask, It.IsAny<TaskOptions>()), Times.Once);
clock.Advance(TimeSpan.FromMinutes(5));
await Until(() => manager.Invocations.Count(x => x.Method.Name == "QueueScheduledTask") == 2);
manager.Verify(x => x.QueueScheduledTask(worker.Object.ScheduledTask, It.IsAny<TaskOptions>()), Times.Exactly(2));
await service.StopAsync(CancellationToken.None);
Console.WriteLine("PASS: migration, 5-minute timer, disabled sync, saved 10-minute interval, and clean shutdown.");

// Restart against migrated triggers: the migration must not add or duplicate any triggers.
using var second = new SyncTimerService(manager.Object, lifetime.Object, clock, NullLogger<SyncTimerService>.Instance);
await second.StartAsync(CancellationToken.None);
await Task.Delay(50);
await second.StopAsync(CancellationToken.None);
if (worker.Object.Triggers.Count != 1) throw new Exception("Migration was not idempotent.");
Console.WriteLine("PASS: repeated startup preserves the migrated schedule.");

registeredTasks = [];
using var waitingService = new SyncTimerService(manager.Object, lifetime.Object, clock, NullLogger<SyncTimerService>.Instance);
await waitingService.StartAsync(CancellationToken.None);
await Task.Delay(50);
await waitingService.StopAsync(CancellationToken.None);
Console.WriteLine("PASS: shutdown while waiting for task registration; busy worker skipped.");

static async Task Until(Func<bool> predicate)
{
    for (var i = 0; i < 100; i++)
    {
        if (predicate()) return;
        await Task.Delay(10);
    }
    throw new Exception("Timed out waiting for migration.");
}

namespace Jellyfin.Plugin.AdAccountDisableSync
{
    // Stand-ins isolate the real timer service from LDAP and server configuration storage.
    internal sealed class Plugin
    {
        public static Plugin? Instance { get; private set; }
        public Plugin() => Instance = this;
        public TestConfiguration Configuration { get; } = new();
        internal event Action? ScheduleChanged;
        internal void ChangeSettings() => ScheduleChanged?.Invoke();
    }
    internal sealed class TestConfiguration
    {
        public bool Enabled { get; set; } = true;
        public int SyncIntervalMinutes { get; set; } = 5;
    }
    internal sealed class AdAccountDisableSyncTask : IScheduledTask
    {
        public string Name => "Test";
        public string Key => "AdAccountDisableSync";
        public string Description => "Test";
        public string Category => "AD Account Sync";
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken token) => Task.CompletedTask;
    }
}
