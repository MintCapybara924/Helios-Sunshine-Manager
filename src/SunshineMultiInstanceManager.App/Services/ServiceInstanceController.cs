using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SunshineMultiInstanceManager.Core.Process;
using SunshineMultiInstanceManager.Core.Storage;
using SunshineMultiInstanceManager.Core.Storage.Models;

namespace SunshineMultiInstanceManager.App.Services;

public sealed class ServiceInstanceController : IInstanceController
{
	private readonly SettingsStore _store;
	private readonly ILogger _logger;
	private readonly Dictionary<string, bool> _statusCache = new(StringComparer.OrdinalIgnoreCase);
	private readonly CancellationTokenSource _pollCts = new();
	private readonly Task _pollTask;
	private readonly SemaphoreSlim _pipeCallLock = new(1, 1);

	public event EventHandler<InstanceStateChangedEventArgs>? InstanceStateChanged;

	public static async Task<bool> IsPipeAvailableAsync(CancellationToken ct = default)
	{
		try
		{
			using NamedPipeClientStream pipe = new(".", ServiceControlConstants.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
			await pipe.ConnectAsync(800, ct);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public ServiceInstanceController(SettingsStore store, ILogger logger)
	{
		_store = store;
		_logger = logger;
		foreach (InstanceConfig instance in _store.Settings.Instances)
		{
			_statusCache[instance.Id] = false;
		}

		_pollTask = PollStatusLoopAsync(_pollCts.Token);
	}

	public async Task StartAllAsync(CancellationToken ct = default)
	{
		await SendCommandAsync("start-all", null, ct);
		await RefreshStatusAsync(ct);
	}

	public async Task StopAllAsync(CancellationToken ct = default)
	{
		await SendCommandAsync("stop-all", null, ct);
		await RefreshStatusAsync(ct);
	}

	public async Task RestartInstanceAsync(InstanceConfig instance, CancellationToken ct = default)
	{
		await SendCommandAsync("restart-instance", instance.Id, ct);
		await RefreshStatusAsync(ct);
	}

	public async Task StopInstanceAsync(InstanceConfig instance, CancellationToken ct = default)
	{
		await SendCommandAsync("stop-instance", instance.Id, ct);
		await RefreshStatusAsync(ct);
	}

	public bool IsRunning(string instanceId)
	{
		return _statusCache.TryGetValue(instanceId, out bool running) && running;
	}

	public async ValueTask DisposeAsync()
	{
		await _pollCts.CancelAsync();
		try
		{
			await _pollTask;
		}
		catch
		{
		}
		_pollCts.Dispose();
		_pipeCallLock.Dispose();
	}

	private async Task PollStatusLoopAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(TimeSpan.FromSeconds(3), ct);
				await RefreshStatusAsync(ct);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogDebug("Service status poll failed: {Msg}", ex.Message);
			}
		}
	}

	private async Task RefreshStatusAsync(CancellationToken ct)
	{
		ServiceCommandResponse response = await SendCommandAsync("status", null, ct);
		if (!response.Ok || response.Statuses == null)
		{
			return;
		}

		foreach (KeyValuePair<string, bool> item in response.Statuses)
		{
			bool previous = _statusCache.TryGetValue(item.Key, out bool oldValue) && oldValue;
			_statusCache[item.Key] = item.Value;
			if (previous != item.Value)
			{
				string name = _store.Settings.Instances.Find(i => i.Id == item.Key)?.Name ?? item.Key;
				InstanceStateChanged?.Invoke(this, new InstanceStateChangedEventArgs(item.Key, name, item.Value));
			}
		}
	}

	private async Task<ServiceCommandResponse> SendCommandAsync(string command, string? instanceId, CancellationToken ct)
	{
		ServiceCommandRequest request = new()
		{
			Command = command,
			InstanceId = instanceId
		};

		await _pipeCallLock.WaitAsync(ct);
		try
		{
			using NamedPipeClientStream pipe = new(".", ServiceControlConstants.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
			await pipe.ConnectAsync(12000, ct);

			using StreamWriter writer = new(pipe, new UTF8Encoding(false), leaveOpen: true);
			using StreamReader reader = new(pipe, Encoding.UTF8, leaveOpen: true);

			string jsonRequest = JsonSerializer.Serialize(request);
			await writer.WriteLineAsync(jsonRequest);
			await writer.FlushAsync();

			string? jsonResponse = await reader.ReadLineAsync(ct);
			if (string.IsNullOrWhiteSpace(jsonResponse))
			{
				throw new InvalidOperationException("Service pipe returned empty response.");
			}

			ServiceCommandResponse response = JsonSerializer.Deserialize<ServiceCommandResponse>(jsonResponse)
				?? new ServiceCommandResponse { Ok = false, Error = "Invalid service response" };

			if (!response.Ok)
			{
				throw new InvalidOperationException(response.Error ?? "Service command failed.");
			}

			return response;
		}
		finally
		{
			_pipeCallLock.Release();
		}
	}
}
