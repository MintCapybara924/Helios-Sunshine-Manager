using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using Microsoft.Win32;

namespace Helios.Core.Scheduler;

public static class ServiceControllerHelper
{
	private const string SpawnerExeName = "Helios.Spawner.exe";

	/// <summary>
	/// Locates the Spawner executable in a "service" subdirectory next to the running app.
	/// </summary>
	public static string? FindSpawnerExePath()
	{
		string? appDir = Path.GetDirectoryName(Environment.ProcessPath);
		if (appDir == null) return null;

		// Primary: service subdirectory (recommended for isolation)
		string subDirCandidate = Path.Combine(appDir, "service", SpawnerExeName);
		if (File.Exists(subDirCandidate)) return subDirCandidate;

		// Fallback: same directory as app
		string flatCandidate = Path.Combine(appDir, SpawnerExeName);
		if (File.Exists(flatCandidate)) return flatCandidate;

		// Development fallback: running from App bin output, probe Spawner project bin output.
		string[] devCandidates =
		[
			Path.GetFullPath(Path.Combine(appDir, @"..\..\..\..\..\SunshineMultiInstanceManager.Spawner\bin\x64\Debug\net8.0-windows", SpawnerExeName)),
			Path.GetFullPath(Path.Combine(appDir, @"..\..\..\..\..\SunshineMultiInstanceManager.Spawner\bin\x64\Release\net8.0-windows", SpawnerExeName)),
			Path.GetFullPath(Path.Combine(appDir, @"..\..\..\..\..\SunshineMultiInstanceManager.Spawner\bin\Debug\net8.0-windows", SpawnerExeName)),
			Path.GetFullPath(Path.Combine(appDir, @"..\..\..\..\..\SunshineMultiInstanceManager.Spawner\bin\Release\net8.0-windows", SpawnerExeName))
		];

		foreach (string candidate in devCandidates)
		{
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		return null;
	}

	/// <summary>
	/// Installs the Spawner as a Windows Service (LocalSystem, auto-start).
	/// Requires administrator privileges.
	/// </summary>
	public static bool InstallService(string serviceName, string spawnerExePath)
	{
		if (IsInstalled(serviceName))
		{
			// Service already registered — update the binary path in case the app was moved.
			RunSc($"config \"{serviceName}\" binPath= \"\\\"{spawnerExePath}\\\"\" start= auto obj= \"LocalSystem\"");
			return true;
		}

		int exitCode = RunSc($"create \"{serviceName}\" binPath= \"\\\"{spawnerExePath}\\\"\" start= auto obj= \"LocalSystem\"");
		return exitCode == 0;
	}

	/// <summary>
	/// Removes the Spawner Windows Service registration.
	/// </summary>
	public static bool UninstallService(string serviceName)
	{
		if (!IsInstalled(serviceName)) return true;

		try
		{
			using ServiceController sc = new(serviceName);
			if (sc.Status != ServiceControllerStatus.Stopped && sc.CanStop)
			{
				sc.Stop();
				sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8));
			}
		}
		catch { }

		return RunSc($"delete \"{serviceName}\"") == 0;
	}

	private static int RunSc(string arguments)
	{
		using System.Diagnostics.Process proc = new();
		proc.StartInfo = new ProcessStartInfo
		{
			FileName = "sc.exe",
			Arguments = arguments,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		proc.Start();
		proc.WaitForExit(15_000);
		return proc.ExitCode;
	}

	public static bool IsInstalled(string serviceName)
	{
		try
		{
			ServiceController serviceController = new ServiceController(serviceName);
			try
			{
				_ = serviceController.Status;
				return true;
			}
			finally
			{
				((IDisposable)serviceController)?.Dispose();
			}
		}
		catch
		{
			return false;
		}
	}

	public static ServiceControllerStatus? GetStatus(string serviceName)
	{
		try
		{
			ServiceController serviceController = new ServiceController(serviceName);
			try
			{
				return serviceController.Status;
			}
			finally
			{
				((IDisposable)serviceController)?.Dispose();
			}
		}
		catch
		{
			return null;
		}
	}

	public static ServiceStartMode? GetStartMode(string serviceName)
	{
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + serviceName, writable: false);
			if (registryKey == null)
			{
				return null;
			}
			if (registryKey.GetValue("Start") is int value)
			{
				return (ServiceStartMode)value;
			}
			return null;
		}
		catch
		{
			return null;
		}
	}

	public static void StopAndDisable(string serviceName, int stopTimeoutMs = 10000)
	{
		ServiceController serviceController = new ServiceController(serviceName);
		try
		{
			serviceController.Refresh();
			ServiceControllerStatus status = serviceController.Status;
			if (status != ServiceControllerStatus.Stopped && status != ServiceControllerStatus.StopPending && serviceController.CanStop)
			{
				serviceController.Stop();
				serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(stopTimeoutMs));
			}
			SetStartType(serviceName, ServiceStartMode.Disabled);
		}
		finally
		{
			((IDisposable)serviceController)?.Dispose();
		}
	}

	public static void SetStartType(string serviceName, ServiceStartMode mode)
	{
		using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + serviceName, writable: true) ?? throw new InvalidOperationException("鎵句笉鍒版湇鍕?'" + serviceName + "' 鐨勭櫥閷勬纰硷紝鏈嶅嫏鍙兘鏈畨瑁濄。");
		registryKey.SetValue("Start", (int)mode, RegistryValueKind.DWord);
	}
}

