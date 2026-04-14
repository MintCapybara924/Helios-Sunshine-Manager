using System;
using System.Threading;
using System.Threading.Tasks;
using SunshineMultiInstanceManager.Core.Process;
using SunshineMultiInstanceManager.Core.Storage.Models;

namespace SunshineMultiInstanceManager.App.Services;

public sealed class LocalInstanceController : IInstanceController
{
	private readonly ProcessManager _processManager;

	public event EventHandler<InstanceStateChangedEventArgs>? InstanceStateChanged;

	public LocalInstanceController(ProcessManager processManager)
	{
		_processManager = processManager;
		_processManager.InstanceStateChanged += OnInstanceStateChanged;
	}

	public Task StartAllAsync(CancellationToken ct = default)
	{
		return _processManager.StartAllAsync(ct);
	}

	public Task StopAllAsync(CancellationToken ct = default)
	{
		return _processManager.StopAllAsync(ct);
	}

	public Task RestartInstanceAsync(InstanceConfig instance, CancellationToken ct = default)
	{
		return _processManager.RestartInstanceAsync(instance, ct);
	}

	public Task StopInstanceAsync(InstanceConfig instance, CancellationToken ct = default)
	{
		return _processManager.StopInstanceAsync(instance, ct);
	}

	public bool IsRunning(string instanceId)
	{
		return _processManager.IsRunning(instanceId);
	}

	public async ValueTask DisposeAsync()
	{
		_processManager.InstanceStateChanged -= OnInstanceStateChanged;
		await _processManager.DisposeAsync();
	}

	private void OnInstanceStateChanged(object? sender, InstanceStateChangedEventArgs e)
	{
		InstanceStateChanged?.Invoke(this, e);
	}
}
