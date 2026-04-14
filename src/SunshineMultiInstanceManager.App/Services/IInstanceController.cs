using System;
using System.Threading;
using System.Threading.Tasks;
using SunshineMultiInstanceManager.Core.Process;
using SunshineMultiInstanceManager.Core.Storage.Models;

namespace SunshineMultiInstanceManager.App.Services;

public interface IInstanceController : IAsyncDisposable
{
	event EventHandler<InstanceStateChangedEventArgs>? InstanceStateChanged;

	Task StartAllAsync(CancellationToken ct = default);
	Task StopAllAsync(CancellationToken ct = default);
	Task RestartInstanceAsync(InstanceConfig instance, CancellationToken ct = default);
	Task StopInstanceAsync(InstanceConfig instance, CancellationToken ct = default);
	bool IsRunning(string instanceId);
}
