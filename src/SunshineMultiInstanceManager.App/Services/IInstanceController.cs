using System;
using System.Threading;
using System.Threading.Tasks;
using Helios.Core.Process;
using Helios.Core.Storage.Models;

namespace Helios.App.Services;

public interface IInstanceController : IAsyncDisposable
{
	event EventHandler<InstanceStateChangedEventArgs>? InstanceStateChanged;

	Task StartAllAsync(CancellationToken ct = default);
	Task StopAllAsync(CancellationToken ct = default);
	Task RestartInstanceAsync(InstanceConfig instance, CancellationToken ct = default);
	Task StopInstanceAsync(InstanceConfig instance, CancellationToken ct = default);
	bool IsRunning(string instanceId);
}

