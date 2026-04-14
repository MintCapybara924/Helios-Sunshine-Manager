using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Helios.Core.Storage;
using Helios.Core.Storage.Models;

namespace Helios.Core.Process;

public sealed class ProcessManager : IAsyncDisposable
{
	private readonly SettingsStore _store;

	private readonly ProcessLauncher _launcher;

	private readonly ILogger _logger;

	private CancellationTokenSource? _cts;

	private Task? _guardianTask;

	private readonly HashSet<string> _restarting = new HashSet<string>();

	private readonly object _restartLock = new object();

	private readonly object _guardianLock = new object();

	public event EventHandler<InstanceStateChangedEventArgs>? InstanceStateChanged;

	public ProcessManager(SettingsStore store, ProcessLauncher launcher, ILogger logger)
	{
		_store = store;
		_launcher = launcher;
		_logger = logger;
	}

	public async Task StartAllAsync(CancellationToken ct = default(CancellationToken))
	{
		EnsureGuardianLoopStarted(ct);
		foreach (InstanceConfig item in _store.Settings.Instances.Where((InstanceConfig i) => i.Enabled))
		{
			if (_store.Transient.Instances.TryGetValue(item.Id, out InstanceRuntimeState? state))
			{
				state.ManualStopRequested = false;
			}
			await EnsureInstanceRunningAsync(item, forceRestart: false, ct);
		}
	}

	public async Task StopAllAsync(CancellationToken ct = default(CancellationToken))
	{
		if (_cts != null)
		{
			await _cts.CancelAsync();
			if (_guardianTask != null)
			{
				try
				{
					await _guardianTask;
				}
				catch (OperationCanceledException)
				{
				}
			}
		}
		_cts?.Dispose();
		_cts = null;
		_guardianTask = null;
		await Task.WhenAll(_store.Settings.Instances.Select((InstanceConfig inst) => StopInstanceAsync(inst, ct)).ToArray());
		foreach (InstanceConfig instance in _store.Settings.Instances)
		{
			if (_store.Transient.Instances.TryGetValue(instance.Id, out InstanceRuntimeState value))
			{
				value.Pid = 0;
				value.IsAlive = false;
			}
		}
		_store.SaveTransientSync();
	}

	public async Task StopInstanceAsync(InstanceConfig instance, CancellationToken ct = default(CancellationToken))
	{
		if (!_store.Transient.Instances.TryGetValue(instance.Id, out InstanceRuntimeState state))
		{
			state = new InstanceRuntimeState();
			_store.Transient.Instances[instance.Id] = state;
		}

		state.ManualStopRequested = true;

		HashSet<int> pidsToStop = new HashSet<int>();
		if (state.Pid > 0 && GracefulShutdown.IsAlive(state.Pid))
		{
			pidsToStop.Add(state.Pid);
		}

		foreach (int residualPid in FindResidualInstancePids(instance))
		{
			if (GracefulShutdown.IsAlive(residualPid))
			{
				pidsToStop.Add(residualPid);
			}
		}

		if (pidsToStop.Count == 0)
		{
			state.Pid = 0;
			state.IsAlive = false;
			return;
		}

		state.IsShuttingDown = true;
		try
		{
			foreach (int pid in pidsToStop)
			{
				_logger.LogInformation("Stopping instance [{Name}] PID={Pid}", instance.Name, pid);
				await GracefulShutdown.ShutdownAsync(pid, 8000, _logger, ct);
			}

			List<int> stubbornPids = FindResidualInstancePids(instance)
				.Where(GracefulShutdown.IsAlive)
				.ToList();
			foreach (int stubbornPid in stubbornPids)
			{
				_logger.LogWarning("Force terminating stubborn residual process for [{Name}] PID={Pid}", instance.Name, stubbornPid);
				GracefulShutdown.ForceTerminate(stubbornPid, _logger);
			}
		}
		finally
		{
			state.Pid = 0;
			state.IsAlive = false;
			state.IsShuttingDown = false;
			RaiseStateChanged(instance, running: false);
		}
	}

	public async Task RestartInstanceAsync(InstanceConfig instance, CancellationToken ct = default(CancellationToken))
	{
		if (_store.Transient.Instances.TryGetValue(instance.Id, out InstanceRuntimeState? state))
		{
			state.ManualStopRequested = false;
		}

		_store.SyncInstanceConf(instance);
		await StopInstanceAsync(instance, ct);
		if (_store.Transient.Instances.TryGetValue(instance.Id, out InstanceRuntimeState? stateAfterStop))
		{
			stateAfterStop.ManualStopRequested = false;
		}
		if (instance.Enabled)
		{
			EnsureGuardianLoopStarted(ct);
			await EnsureInstanceRunningAsync(instance, forceRestart: true, ct);
		}
	}

	private void EnsureGuardianLoopStarted(CancellationToken ct)
	{
		lock (_guardianLock)
		{
			if (_cts != null && !_cts.IsCancellationRequested && _guardianTask != null && !_guardianTask.IsCompleted)
			{
				return;
			}

			_cts?.Dispose();
			_cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			_guardianTask = RunGuardianLoopAsync(_cts.Token);
		}
	}

	private async Task RunGuardianLoopAsync(CancellationToken ct)
	{
		using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(5000.0));
		_logger.LogInformation("Guardian loop started. Interval={Ms}ms.", 5000);
		try
		{
			while (await timer.WaitForNextTickAsync(ct))
			{
				List<InstanceConfig> list = _store.Settings.Instances.Where((InstanceConfig i) => i.Enabled).ToList();
				foreach (InstanceConfig item in list)
				{
					await CheckAndGuardAsync(item, ct);
				}
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Guardian loop stopped.");
		}
	}

	private async Task CheckAndGuardAsync(InstanceConfig inst, CancellationToken ct)
	{
		if (!_store.Transient.Instances.TryGetValue(inst.Id, out InstanceRuntimeState value) || value.IsShuttingDown)
		{
			return;
		}

		int singletonPid = await EnforceSingleProcessConstraintAsync(inst, value, ct);
		if (value.ManualStopRequested)
		{
			if (singletonPid > 0)
			{
				_logger.LogInformation("ManualStopRequested is set for [{Name}]. Terminating residual PID={Pid} detected by guardian.", inst.Name, singletonPid);
				GracefulShutdown.ForceTerminate(singletonPid, _logger);
			}
			if (value.Pid > 0 && GracefulShutdown.IsAlive(value.Pid))
			{
				_logger.LogInformation("ManualStopRequested is set for [{Name}]. Terminating tracked PID={Pid}.", inst.Name, value.Pid);
				GracefulShutdown.ForceTerminate(value.Pid, _logger);
			}
			value.IsAlive = false;
			value.Pid = 0;
			RaiseStateChanged(inst, running: false);
			return;
		}

		if (singletonPid > 0)
		{
			value.Pid = singletonPid;
			value.IsAlive = true;
		}

		lock (_restartLock)
		{
			if (_restarting.Contains(inst.Id))
			{
				return;
			}
		}

		if (TryAdoptResidualRunningProcess(inst, value))
		{
			if (value.ConsecutiveCrashCount > 0)
			{
				value.ConsecutiveCrashCount = 0;
			}
			return;
		}

		if (value.IsAlive = GracefulShutdown.IsAlive(value.Pid))
		{
			if (value.ConsecutiveCrashCount > 0 && value.LastStartedUtc.HasValue && (DateTime.UtcNow - value.LastStartedUtc.Value).TotalSeconds >= 30.0)
			{
				_logger.LogInformation("Instance [{Name}] is stable again. Resetting crash counter from {Count}.", inst.Name, value.ConsecutiveCrashCount);
				value.ConsecutiveCrashCount = 0;
			}
			return;
		}
		if (DateTime.UtcNow < value.NextRestartAllowedUtc)
		{
			double totalSeconds = (value.NextRestartAllowedUtc - DateTime.UtcNow).TotalSeconds;
			_logger.LogDebug("Instance [{Name}] is in backoff window. Remaining={Sec:F0}s.", inst.Name, totalSeconds);
			return;
		}
		if (value.Pid > 0)
		{
			value.CrashRestartCount++;
			value.ConsecutiveCrashCount++;
			int consecutiveCrashCount = value.ConsecutiveCrashCount;
			int num = ((consecutiveCrashCount > 2) ? (consecutiveCrashCount switch
			{
				3 => 30, 
				4 => 60, 
				_ => 120, 
			}) : 0);
			int num2 = num;
			if (num2 > 0)
			{
				value.NextRestartAllowedUtc = DateTime.UtcNow.AddSeconds(num2);
			}
			string backoffMessage = num2 > 0
				? $" Backoff {num2}s before restart."
				: " Restarting immediately.";
			_logger.LogWarning("Instance [{Name}] PID={Pid} exited unexpectedly (consecutive={Consec}, total={Total}).{Backoff}", inst.Name, value.Pid, value.ConsecutiveCrashCount, value.CrashRestartCount, backoffMessage);
			if (num2 > 0)
			{
				value.Pid = 0;
				RaiseStateChanged(inst, running: false);
				return;
			}
		}
		else
		{
			_logger.LogInformation("Instance [{Name}] is not running. Starting now.", inst.Name);
		}
		value.Pid = 0;
		lock (_restartLock)
		{
			_restarting.Add(inst.Id);
		}
		try
		{
			await EnsureInstanceRunningAsync(inst, forceRestart: false, ct);
		}
		finally
		{
			lock (_restartLock)
			{
				_restarting.Remove(inst.Id);
			}
		}
	}

	private async Task EnsureInstanceRunningAsync(InstanceConfig instance, bool forceRestart, CancellationToken ct)
	{
		if (!_store.Transient.Instances.TryGetValue(instance.Id, out InstanceRuntimeState state))
		{
			state = new InstanceRuntimeState();
			_store.Transient.Instances[instance.Id] = state;
		}

		int singletonPid = await EnforceSingleProcessConstraintAsync(instance, state, ct);
		if (!forceRestart && singletonPid > 0)
		{
			state.Pid = singletonPid;
			state.IsAlive = true;
			RaiseStateChanged(instance, running: true);
			return;
		}

		if (forceRestart)
		{
			state.ManualStopRequested = false;
		}

		if (!forceRestart && TryAdoptResidualRunningProcess(instance, state))
		{
			return;
		}

		if (!forceRestart && state.Pid > 0 && GracefulShutdown.IsAlive(state.Pid))
		{
			if (!IsProcessElevated(state.Pid))
			{
				_logger.LogWarning("Tracked process for [{Name}] PID={Pid} is not elevated. Terminating before restart.", instance.Name, state.Pid);
				GracefulShutdown.ForceTerminate(state.Pid, _logger);
				state.Pid = 0;
				state.IsAlive = false;
			}
			else
			{
				state.IsAlive = true;
				RaiseStateChanged(instance, running: true);
				return;
			}
		}

		if (!forceRestart && GracefulShutdown.IsAlive(state.Pid))
		{
			state.IsAlive = true;
			RaiseStateChanged(instance, running: true);
			return;
		}
		_store.SyncInstanceConf(instance);
		EnsureAppsJsonExists(instance);
		try
		{
			int pid = await _launcher.LaunchAsync(instance, ct);
			state.Pid = pid;
			state.IsAlive = false;
			state.LastStartedUtc = DateTime.UtcNow;
			state.IsShuttingDown = false;
			state.NextRestartAllowedUtc = DateTime.MinValue;

			await Task.Delay(1200, ct);
			state.IsAlive = GracefulShutdown.IsAlive(pid);
			if (!state.IsAlive)
			{
				if (!TryAdoptResidualRunningProcess(instance, state))
				{
					state.Pid = 0;
					_logger.LogWarning("Instance [{Name}] exited shortly after launch. Check sunshine.conf and executable path.", instance.Name);
					RaiseStateChanged(instance, running: false);
					await _store.SaveTransientAsync(ct);
					return;
				}
			}

			RaiseStateChanged(instance, running: true);
			await _store.SaveTransientAsync(ct);
		}
		catch (Exception ex) when (!ct.IsCancellationRequested)
		{
			_logger.LogError(ex, "Failed to launch instance [{Name}]. Guardian will retry later.", instance.Name);
			state.Pid = 0;
			state.IsAlive = false;
			RaiseStateChanged(instance, running: false);
			await _store.SaveTransientAsync(ct);
		}
	}

	public int GetPid(string instanceId)
	{
		if (!_store.Transient.Instances.TryGetValue(instanceId, out InstanceRuntimeState value))
		{
			return 0;
		}
		return value.Pid;
	}

	public bool IsRunning(string instanceId)
	{
		if (!_store.Transient.Instances.TryGetValue(instanceId, out InstanceRuntimeState value))
		{
			return false;
		}

		InstanceConfig? instance = _store.Settings.Instances.FirstOrDefault(i => i.Id.Equals(instanceId, StringComparison.OrdinalIgnoreCase));
		if (instance != null && TryAdoptResidualRunningProcess(instance, value))
		{
			return true;
		}

		if (value.IsAlive)
		{
			return GracefulShutdown.IsAlive(value.Pid) && IsProcessElevated(value.Pid);
		}
		return false;
	}

	private static void EnsureAppsJsonExists(InstanceConfig instance)
	{
		if (!File.Exists(instance.AppsJsonPath))
		{
			Directory.CreateDirectory(instance.InstanceDirectory);
			AtomicFile.WriteJson(instance.AppsJsonPath, JsonDocument.Parse("{\n  \"env\": {\n    \"PATH\": \"$(PATH)\"\n  },\n  \"apps\": []\n}").RootElement);
		}
	}

	private IReadOnlyList<int> FindResidualInstancePids(InstanceConfig instance)
	{
		List<int> pids = new List<int>();
		if (!OperatingSystem.IsWindows())
		{
			return pids;
		}

		string normalizedConfPath = NormalizePathForCompare(instance.SunshineConfPath);
		string normalizedInstanceDir = NormalizePathForCompare(instance.InstanceDirectory);
		string normalizedExePath = NormalizePathForCompare(instance.ResolvedExecutablePath);
		HashSet<string> candidateNames = new(StringComparer.OrdinalIgnoreCase)
		{
			"sunshine.exe"
		};

		string currentExeName = Path.GetFileName(instance.ResolvedExecutablePath);
		if (!string.IsNullOrWhiteSpace(currentExeName))
		{
			candidateNames.Add(currentExeName);
		}

		string nameFilter = string.Join(" OR ", candidateNames.Select(n => $"Name='{n.Replace("'", "''")}'"));
		string query = $"SELECT ProcessId, Name, CommandLine, ExecutablePath FROM Win32_Process WHERE {nameFilter}";
		ManagementObjectCollection? results = null;
		try
		{
			using ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
			results = searcher.Get();
			foreach (ManagementObject item in results)
			{
				string? commandLine = item["CommandLine"] as string;
				string normalizedCmd = NormalizePathForCompare(commandLine ?? string.Empty);
				string normalizedProcExe = NormalizePathForCompare(item["ExecutablePath"] as string ?? string.Empty);

				bool matchesByConf = !string.IsNullOrWhiteSpace(normalizedCmd)
					&& normalizedCmd.Contains(normalizedConfPath, StringComparison.OrdinalIgnoreCase);

				bool matchesByExeAndDir = !string.IsNullOrWhiteSpace(normalizedCmd)
					&& !string.IsNullOrWhiteSpace(normalizedProcExe)
					&& normalizedProcExe.Equals(normalizedExePath, StringComparison.OrdinalIgnoreCase)
					&& normalizedCmd.Contains(normalizedInstanceDir, StringComparison.OrdinalIgnoreCase);

				if (!matchesByConf && !matchesByExeAndDir)
				{
					continue;
				}

				if (item["ProcessId"] is uint pid && pid > 0)
				{
					pids.Add((int)pid);
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug("Residual process lookup failed: {Msg}", ex.Message);
		}
		finally
		{
			results?.Dispose();
		}

		return pids;
	}

	private static string NormalizePathForCompare(string value)
	{
		return value.Replace('/', '\\').Trim().Trim('"');
	}

	private bool TryAdoptResidualRunningProcess(InstanceConfig instance, InstanceRuntimeState state)
	{
		int adoptedPid = 0;
		foreach (int residualPid in FindResidualInstancePids(instance).Distinct())
		{
			if (!GracefulShutdown.IsAlive(residualPid))
			{
				continue;
			}

			if (!IsProcessElevated(residualPid))
			{
				_logger.LogWarning("Residual sunshine for [{Name}] PID={Pid} is not elevated. Terminating.", instance.Name, residualPid);
				GracefulShutdown.ForceTerminate(residualPid, _logger);
				continue;
			}

			if (adoptedPid == 0)
			{
				adoptedPid = residualPid;
				continue;
			}

			_logger.LogWarning("Duplicate elevated residual process for [{Name}] detected. Keeping PID={KeepPid}, terminating PID={DupPid}", instance.Name, adoptedPid, residualPid);
			GracefulShutdown.ForceTerminate(residualPid, _logger);
		}

		if (adoptedPid <= 0)
		{
			return false;
		}

		// If a manual stop was requested, do not adopt the residual process —
		// terminate it instead so the instance stays stopped.
		if (state.ManualStopRequested)
		{
			_logger.LogInformation("ManualStopRequested is set for [{Name}]. Terminating residual PID={Pid} instead of adopting.", instance.Name, adoptedPid);
			GracefulShutdown.ForceTerminate(adoptedPid, _logger);
			return false;
		}

		bool changed = state.Pid != adoptedPid || !state.IsAlive;
		state.Pid = adoptedPid;
		state.IsAlive = true;
		if (changed)
		{
			RaiseStateChanged(instance, running: true);
		}

		return true;
	}

	private async Task<int> EnforceSingleProcessConstraintAsync(InstanceConfig instance, InstanceRuntimeState state, CancellationToken ct)
	{
		List<int> alivePids = FindResidualInstancePids(instance)
			.Where(GracefulShutdown.IsAlive)
			.Distinct()
			.OrderBy(pid => pid)
			.ToList();

		if (state.Pid > 0 && GracefulShutdown.IsAlive(state.Pid) && !alivePids.Contains(state.Pid))
		{
			alivePids.Insert(0, state.Pid);
		}

		foreach (int pid in alivePids.ToList())
		{
			if (IsProcessElevated(pid))
			{
				continue;
			}

			_logger.LogWarning("Non-elevated sunshine detected for [{Name}] PID={Pid}. Terminating.", instance.Name, pid);
			await GracefulShutdown.ShutdownAsync(pid, 2500, _logger, ct);
			if (GracefulShutdown.IsAlive(pid))
			{
				GracefulShutdown.ForceTerminate(pid, _logger);
			}
			alivePids.Remove(pid);
		}

		if (alivePids.Count <= 1)
		{
			return alivePids.Count == 1 ? alivePids[0] : 0;
		}

		int keeperPid = state.Pid > 0 && alivePids.Contains(state.Pid) ? state.Pid : alivePids[0];
		foreach (int extraPid in alivePids.Where(pid => pid != keeperPid))
		{
			_logger.LogWarning("Multiple sunshine processes detected for [{Name}]. Keeping PID={KeepPid}, terminating PID={ExtraPid}", instance.Name, keeperPid, extraPid);
			await GracefulShutdown.ShutdownAsync(extraPid, 3000, _logger, ct);
			if (GracefulShutdown.IsAlive(extraPid))
			{
				GracefulShutdown.ForceTerminate(extraPid, _logger);
			}
		}

		List<int> remainingPids = FindResidualInstancePids(instance)
			.Where(GracefulShutdown.IsAlive)
			.Distinct()
			.OrderBy(pid => pid)
			.ToList();

		if (remainingPids.Count == 0)
		{
			return 0;
		}

		if (!remainingPids.Contains(keeperPid))
		{
			keeperPid = remainingPids[0];
		}

		foreach (int extraPid in remainingPids.Where(pid => pid != keeperPid))
		{
			_logger.LogWarning("Residual duplicate process still found for [{Name}]. Force terminating PID={ExtraPid}", instance.Name, extraPid);
			GracefulShutdown.ForceTerminate(extraPid, _logger);
		}

		return GracefulShutdown.IsAlive(keeperPid) ? keeperPid : 0;
	}

	private static bool IsProcessElevated(int pid)
	{
		if (pid <= 0)
		{
			return false;
		}

		nint processHandle = IntPtr.Zero;
		nint tokenHandle = IntPtr.Zero;
		try
		{
			processHandle = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_INFORMATION, bInheritHandle: false, pid);
			if (processHandle == IntPtr.Zero)
			{
				return false;
			}

			if (!NativeMethods.OpenProcessToken(processHandle, NativeMethods.TOKEN_QUERY, out tokenHandle))
			{
				return false;
			}

			int size = Marshal.SizeOf<NativeMethods.TOKEN_ELEVATION>();
			nint buffer = Marshal.AllocHGlobal(size);
			try
			{
				if (!NativeMethods.GetTokenInformation(tokenHandle, NativeMethods.TOKEN_INFORMATION_CLASS.TokenElevation, buffer, size, out _))
				{
					return false;
				}

				NativeMethods.TOKEN_ELEVATION elevation = Marshal.PtrToStructure<NativeMethods.TOKEN_ELEVATION>(buffer);
				return elevation.TokenIsElevated != 0;
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}
		catch
		{
			return false;
		}
		finally
		{
			if (tokenHandle != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(tokenHandle);
			}

			if (processHandle != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(processHandle);
			}
		}
	}

	private void RaiseStateChanged(InstanceConfig instance, bool running)
	{
		this.InstanceStateChanged?.Invoke(this, new InstanceStateChangedEventArgs(instance.Id, instance.Name, running));
	}

	public async ValueTask DisposeAsync()
	{
		if (_cts != null)
		{
			await _cts.CancelAsync();
			_cts.Dispose();
			_cts = null;
		}
		if (_guardianTask != null)
		{
			try
			{
				await _guardianTask;
			}
			catch
			{
			}
		}
	}
}

