using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SunshineMultiInstanceManager.Core.Storage.Models;

namespace SunshineMultiInstanceManager.Core.Process;

public sealed class ProcessLauncher
{
	private readonly ILogger _logger;

	public ProcessLauncher(ILogger logger)
	{
		_logger = logger;
	}

	public async Task<int> LaunchAsync(InstanceConfig instance, CancellationToken ct = default(CancellationToken))
	{
		InstanceConfig instance2 = instance;
		ct.ThrowIfCancellationRequested();
		if (!File.Exists(instance2.SunshineConfPath))
		{
			throw new FileNotFoundException("sunshine.conf not found: " + instance2.SunshineConfPath, instance2.SunshineConfPath);
		}
		if (!File.Exists(instance2.ResolvedExecutablePath))
		{
			throw new FileNotFoundException("Instance executable not found: " + instance2.ResolvedExecutablePath, instance2.ResolvedExecutablePath);
		}
		_logger.LogInformation("Launching instance [{Name}] port={Port} exe={Exe}", instance2.Name, instance2.Port, instance2.ResolvedExecutablePath);
		int num;
		if (IsRunningAsSystem())
		{
			_logger.LogDebug("Running as SYSTEM; using CreateProcessAsUser.");
			num = await Task.Run(() => LaunchViaCreateProcessAsUser(instance2), ct);
		}
		else
		{
			bool elevated = IsCurrentProcessElevated();
			_logger.LogDebug("Interactive launch context. Elevated={Elevated}; trying scheduled-task launch first.", elevated);
			if (TryLaunchViaScheduledTask(instance2, out int taskPid, out string detail))
			{
				_logger.LogInformation("Scheduled-task launch succeeded for [{Name}]. PID={Pid}. Detail={Detail}", instance2.Name, taskPid, detail);
				num = taskPid;
			}
			else
			{
				_logger.LogWarning("Scheduled-task launch skipped/failed for [{Name}] ({Detail}). Falling back to Process.Start.", instance2.Name, detail);
				num = LaunchViaProcessStart(instance2);
			}
		}
		_logger.LogInformation("Instance [{Name}] launched. PID={Pid}", instance2.Name, num);
		return num;
	}

	private int LaunchViaProcessStart(InstanceConfig instance)
	{
		if (!IsCurrentProcessElevated())
		{
			throw new InvalidOperationException("Manager process is not elevated. Refusing to launch sunshine without administrator privilege.");
		}

		string arguments = BuildCommandLineArgs(instance);
		string workingDirectory = Path.GetDirectoryName(instance.ResolvedExecutablePath) ?? instance.InstanceDirectory;
		System.Diagnostics.Process? obj = System.Diagnostics.Process.Start(new ProcessStartInfo
		{
			FileName = instance.ResolvedExecutablePath,
			Arguments = arguments,
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			CreateNoWindow = true
		}) ?? throw new InvalidOperationException("Process.Start returned null for instance [" + instance.Name + "].");
		if (!IsProcessElevated(obj.Id))
		{
			_logger.LogWarning("ELEVATION_FAIL instance=[{Name}] pid={Pid} mode=local", instance.Name, obj.Id);
			try
			{
				GracefulShutdown.ForceTerminate(obj.Id, _logger);
			}
			catch
			{
			}

			throw new InvalidOperationException("Launched sunshine process is not elevated. Start aborted.");
		}

		if (!HasAdministratorCapability(obj.Id, out string detail))
		{
			_logger.LogWarning("ELEVATION_FAIL instance=[{Name}] pid={Pid} mode=local detail={Detail}", instance.Name, obj.Id, detail);
			try
			{
				GracefulShutdown.ForceTerminate(obj.Id, _logger);
			}
			catch
			{
			}

			throw new InvalidOperationException("Launched sunshine process does not have administrator capability. Start aborted.");
		}

		_logger.LogInformation("ELEVATION_OK instance=[{Name}] pid={Pid} mode=local detail={Detail}", instance.Name, obj.Id, detail);
		_logger.LogInformation("Local launch elevation check passed. PID={Pid}", obj.Id);
		return obj.Id;
	}

	private int LaunchViaCreateProcessAsUser(InstanceConfig instance)
	{
		uint sessionId = NativeMethods.WTSGetActiveConsoleSessionId();
		if (sessionId == uint.MaxValue)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "WTSGetActiveConsoleSessionId failed: no interactive console session.");
		}

		nint hUserToken = IntPtr.Zero;
		nint hSystemToken = IntPtr.Zero;
		nint hSystemTokenDup = IntPtr.Zero;
		nint hEnvironment = IntPtr.Zero;
		nint hProcess = IntPtr.Zero;
		nint hThread = IntPtr.Zero;
		try
		{
			// 1. Get user token — only used for CreateEnvironmentBlock so
			//    the launched process inherits the user's environment variables.
			if (!NativeMethods.WTSQueryUserToken(sessionId, out hUserToken))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"WTSQueryUserToken failed (session {sessionId}). Requires SYSTEM privileges and an interactive user session.");
			}

			// 2. Duplicate current process (SYSTEM) token as a primary token.
			//    Running as SYSTEM gives the process SeTcbPrivilege, which is
			//    required to capture the Winlogon (secure) desktop — the exact
			//    capability that standard Sunshine-as-a-service has.
			if (!NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(),
				NativeMethods.TOKEN_DUPLICATE | NativeMethods.TOKEN_QUERY | NativeMethods.TOKEN_ASSIGN_PRIMARY | (int)NativeMethods.TOKEN_ADJUST_SESSIONID,
				out hSystemToken))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcessToken (SYSTEM) failed.");
			}

			NativeMethods.SECURITY_ATTRIBUTES sa = default;
			sa.nLength = Marshal.SizeOf<NativeMethods.SECURITY_ATTRIBUTES>();
			if (!NativeMethods.DuplicateTokenEx(hSystemToken, NativeMethods.MAXIMUM_ALLOWED, ref sa,
				NativeMethods.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
				NativeMethods.TOKEN_TYPE.TokenPrimary, out hSystemTokenDup))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "DuplicateTokenEx (SYSTEM token) failed.");
			}

			// 3. Assign the duplicated SYSTEM token to the interactive user session
			//    so the process appears on the user's desktop.
			if (!NativeMethods.SetTokenInformation(hSystemTokenDup,
				NativeMethods.TOKEN_INFORMATION_CLASS.TokenSessionId,
				ref sessionId, sizeof(uint)))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"SetTokenInformation(TokenSessionId={sessionId}) failed.");
			}

			_logger.LogInformation("SYSTEM token assigned to session {SessionId} for instance [{Name}].", sessionId, instance.Name);

			// 4. Build the user's environment block so Sunshine sees the
			//    correct %APPDATA%, %TEMP%, etc.
			if (!NativeMethods.CreateEnvironmentBlock(out hEnvironment, hUserToken, false))
			{
				_logger.LogWarning("CreateEnvironmentBlock failed (err={Err}); launching with default SYSTEM environment.", Marshal.GetLastWin32Error());
				hEnvironment = IntPtr.Zero;
			}

			// 5. Launch sunshine.exe with the SYSTEM-in-user-session token.
			NativeMethods.STARTUPINFO si = default;
			si.cb = Marshal.SizeOf<NativeMethods.STARTUPINFO>();
			si.lpDesktop = "winsta0\\default";
			si.dwFlags = (int)NativeMethods.STARTF_USESHOWWINDOW;
			si.wShowWindow = NativeMethods.SW_HIDE;

			string resolvedExecutablePath = instance.ResolvedExecutablePath;
			string lpCurrentDirectory = Path.GetDirectoryName(resolvedExecutablePath) ?? string.Empty;
			string args = BuildCommandLineArgs(instance);
			string commandLine = args.Length > 0
				? "\"" + resolvedExecutablePath + "\" " + args
				: "\"" + resolvedExecutablePath + "\"";

			NativeMethods.SECURITY_ATTRIBUTES lpProcessAttributes = sa;
			NativeMethods.SECURITY_ATTRIBUTES lpThreadAttributes = sa;

			uint creationFlags = NativeMethods.NORMAL_PRIORITY_CLASS
				| NativeMethods.CREATE_NEW_PROCESS_GROUP
				| NativeMethods.CREATE_NO_WINDOW;

			// When we supply an environment block from CreateEnvironmentBlock,
			// it is always Unicode — we must set CREATE_UNICODE_ENVIRONMENT.
			if (hEnvironment != IntPtr.Zero)
			{
				creationFlags |= NativeMethods.CREATE_UNICODE_ENVIRONMENT;
			}

			if (!NativeMethods.CreateProcessAsUser(hSystemTokenDup, resolvedExecutablePath, commandLine,
				ref lpProcessAttributes, ref lpThreadAttributes, bInheritHandles: false,
				creationFlags, hEnvironment, lpCurrentDirectory, ref si, out var pi))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateProcessAsUser failed, command line: " + commandLine);
			}

			hProcess = pi.hProcess;
			hThread = pi.hThread;
			int pid = pi.dwProcessId;

			// 6. Verify elevation (SYSTEM token is inherently elevated).
			if (!IsProcessElevated(pid))
			{
				_logger.LogWarning("ELEVATION_FAIL instance=[{Name}] pid={Pid} mode=service-system", instance.Name, pid);
				GracefulShutdown.ForceTerminate(pid, _logger);
				throw new InvalidOperationException("CreateProcessAsUser launched non-elevated sunshine process. Process terminated.");
			}

			if (!HasAdministratorCapability(pid, out string detail))
			{
				_logger.LogWarning("ELEVATION_FAIL instance=[{Name}] pid={Pid} mode=service-system detail={Detail}", instance.Name, pid, detail);
				GracefulShutdown.ForceTerminate(pid, _logger);
				throw new InvalidOperationException("CreateProcessAsUser launched process without administrator capability. Process terminated.");
			}

			_logger.LogInformation("ELEVATION_OK instance=[{Name}] pid={Pid} mode=service-system detail={Detail}", instance.Name, pid, detail);
			_logger.LogDebug("CreateProcessAsUser (SYSTEM token) succeeded. PID={Pid} hProcess=0x{Handle:X}", pid, hProcess);
			_logger.LogInformation("Service launch elevation check passed. PID={Pid}", pid);
			return pid;
		}
		finally
		{
			if (hThread != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(hThread);
			}
			if (hProcess != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(hProcess);
			}
			if (hEnvironment != IntPtr.Zero)
			{
				NativeMethods.DestroyEnvironmentBlock(hEnvironment);
			}
			if (hSystemTokenDup != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(hSystemTokenDup);
			}
			if (hSystemToken != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(hSystemToken);
			}
			if (hUserToken != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(hUserToken);
			}
		}
	}

	private static bool TryGetLinkedToken(nint tokenHandle, out nint linkedToken)
	{
		linkedToken = IntPtr.Zero;
		if (!NativeMethods.GetTokenInformation(tokenHandle, NativeMethods.TOKEN_INFORMATION_CLASS.TokenLinkedToken, IntPtr.Zero, 0, out int requiredSize)
			&& Marshal.GetLastWin32Error() != 122)
		{
			return false;
		}

		nint buffer = Marshal.AllocHGlobal(requiredSize);
		try
		{
			if (!NativeMethods.GetTokenInformation(tokenHandle, NativeMethods.TOKEN_INFORMATION_CLASS.TokenLinkedToken, buffer, requiredSize, out _))
			{
				return false;
			}

			NativeMethods.TOKEN_LINKED_TOKEN info = Marshal.PtrToStructure<NativeMethods.TOKEN_LINKED_TOKEN>(buffer);
			if (info.LinkedToken == IntPtr.Zero)
			{
				return false;
			}

			linkedToken = info.LinkedToken;
			return true;
		}
		finally
		{
			Marshal.FreeHGlobal(buffer);
		}
	}

	private static bool TryIsTokenElevated(nint tokenHandle, out bool isElevated)
	{
		isElevated = false;
		int size = Marshal.SizeOf<NativeMethods.TOKEN_ELEVATION>();
		nint buffer = Marshal.AllocHGlobal(size);
		try
		{
			if (!NativeMethods.GetTokenInformation(tokenHandle, NativeMethods.TOKEN_INFORMATION_CLASS.TokenElevation, buffer, size, out _))
			{
				return false;
			}

			NativeMethods.TOKEN_ELEVATION elevation = Marshal.PtrToStructure<NativeMethods.TOKEN_ELEVATION>(buffer);
			isElevated = elevation.TokenIsElevated != 0;
			return true;
		}
		finally
		{
			Marshal.FreeHGlobal(buffer);
		}
	}

	private static string BuildCommandLineArgs(InstanceConfig instance)
	{
		string text = "\"" + instance.SunshineConfPath + "\"";
		if (!string.IsNullOrWhiteSpace(instance.ExtraArgs))
		{
			return text + " " + instance.ExtraArgs.Trim();
		}
		return text;
	}

	private static bool IsRunningAsSystem()
	{
		using System.Security.Principal.WindowsIdentity windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
		return windowsIdentity.IsSystem;
	}

	private static bool IsCurrentProcessElevated()
	{
		nint token = IntPtr.Zero;
		try
		{
			if (!NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(), NativeMethods.TOKEN_QUERY, out token))
			{
				return false;
			}

			if (!(TryIsTokenElevated(token, out bool elevated) && elevated))
			{
				return false;
			}

			if (!TryGetTokenElevationType(token, out NativeMethods.TOKEN_ELEVATION_TYPE elevationType))
			{
				return false;
			}

			return elevationType == NativeMethods.TOKEN_ELEVATION_TYPE.TokenElevationTypeFull
				|| elevationType == NativeMethods.TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;
		}
		finally
		{
			if (token != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(token);
			}
		}
	}

	private static bool IsProcessElevated(int pid)
	{
		nint processHandle = IntPtr.Zero;
		nint token = IntPtr.Zero;
		try
		{
			processHandle = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_INFORMATION, bInheritHandle: false, pid);
			if (processHandle == IntPtr.Zero)
			{
				return false;
			}

			if (!NativeMethods.OpenProcessToken(processHandle, NativeMethods.TOKEN_QUERY, out token))
			{
				return false;
			}

			return TryIsTokenElevated(token, out bool elevated) && elevated;
		}
		finally
		{
			if (token != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(token);
			}
			if (processHandle != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(processHandle);
			}
		}
	}

	private static bool HasAdministratorCapability(int pid, out string detail)
	{
		detail = "unknown";
		nint processHandle = IntPtr.Zero;
		nint token = IntPtr.Zero;
		try
		{
			processHandle = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_INFORMATION, bInheritHandle: false, pid);
			if (processHandle == IntPtr.Zero)
			{
				detail = "OpenProcess failed";
				return false;
			}

			if (!NativeMethods.OpenProcessToken(processHandle, NativeMethods.TOKEN_QUERY, out token))
			{
				detail = "OpenProcessToken failed";
				return false;
			}

			if (!TryIsTokenElevated(token, out bool elevated) || !elevated)
			{
				detail = "TokenIsElevated=false";
				return false;
			}

			if (!TryGetTokenElevationType(token, out NativeMethods.TOKEN_ELEVATION_TYPE elevationType))
			{
				detail = "TokenIsElevated=true; TokenElevationType=unavailable";
				return false;
			}

			bool adminCapable = elevationType == NativeMethods.TOKEN_ELEVATION_TYPE.TokenElevationTypeFull
				|| elevationType == NativeMethods.TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;
			detail = "TokenIsElevated=true; TokenElevationType=" + elevationType;
			return adminCapable;
		}
		catch (Exception ex)
		{
			detail = "Exception=" + ex.GetType().Name;
			return false;
		}
		finally
		{
			if (token != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(token);
			}
			if (processHandle != IntPtr.Zero)
			{
				NativeMethods.CloseHandle(processHandle);
			}
		}
	}

	private static bool TryGetTokenElevationType(nint tokenHandle, out NativeMethods.TOKEN_ELEVATION_TYPE elevationType)
	{
		elevationType = NativeMethods.TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;
		int size = Marshal.SizeOf<int>();
		nint buffer = Marshal.AllocHGlobal(size);
		try
		{
			if (!NativeMethods.GetTokenInformation(tokenHandle, NativeMethods.TOKEN_INFORMATION_CLASS.TokenElevationType, buffer, size, out _))
			{
				return false;
			}

			elevationType = (NativeMethods.TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(buffer);
			return true;
		}
		finally
		{
			Marshal.FreeHGlobal(buffer);
		}
	}

	private bool TryLaunchViaScheduledTask(InstanceConfig instance, out int pid, out string detail)
	{
		pid = 0;
		detail = "unknown";

		if (!OperatingSystem.IsWindows())
		{
			detail = "Non-Windows platform";
			return false;
		}

		if (!IsCurrentProcessElevated())
		{
			detail = "Manager is not elevated";
			return false;
		}

		string currentUser;
		try
		{
			currentUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
		}
		catch (Exception ex)
		{
			detail = "GetCurrent user failed: " + ex.GetType().Name;
			return false;
		}

		string taskPath = BuildInstanceLaunchTaskPath(instance);
		string workingDirectory = Path.GetDirectoryName(instance.ResolvedExecutablePath) ?? instance.InstanceDirectory;
		string arguments = BuildCommandLineArgs(instance);
		string hiddenLauncherArgs = BuildHiddenPowerShellLauncherArgs(instance.ResolvedExecutablePath, arguments, workingDirectory);
		DateTime startedAtUtc = DateTime.UtcNow;

		try
		{
			using Microsoft.Win32.TaskScheduler.TaskService taskService = new Microsoft.Win32.TaskScheduler.TaskService();
			EnsureTaskFolder(taskService);

			Microsoft.Win32.TaskScheduler.TaskDefinition taskDefinition = taskService.NewTask();
			taskDefinition.RegistrationInfo.Description = "One-shot elevated launcher for Sunshine instance " + instance.Name;
			taskDefinition.Principal.RunLevel = Microsoft.Win32.TaskScheduler.TaskRunLevel.Highest;
			taskDefinition.Principal.UserId = currentUser;
			taskDefinition.Principal.LogonType = Microsoft.Win32.TaskScheduler.TaskLogonType.InteractiveToken;
			taskDefinition.Settings.AllowDemandStart = true;
			taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
			taskDefinition.Settings.Hidden = true;
			taskDefinition.Settings.MultipleInstances = Microsoft.Win32.TaskScheduler.TaskInstancesPolicy.IgnoreNew;
			taskDefinition.Settings.DisallowStartIfOnBatteries = false;
			taskDefinition.Settings.StopIfGoingOnBatteries = false;
			taskDefinition.Actions.Add(new Microsoft.Win32.TaskScheduler.ExecAction("powershell.exe", hiddenLauncherArgs, workingDirectory));

			taskService.RootFolder.RegisterTaskDefinition(taskPath, taskDefinition, Microsoft.Win32.TaskScheduler.TaskCreation.CreateOrUpdate, currentUser, null, Microsoft.Win32.TaskScheduler.TaskLogonType.InteractiveToken);
			Microsoft.Win32.TaskScheduler.Task task = taskService.GetTask(taskPath) ?? throw new InvalidOperationException("Unable to resolve scheduled task: " + taskPath);
			task.Run();
		}
		catch (Exception ex)
		{
			detail = "Task registration/run failed: " + ex.GetType().Name;
			return false;
		}

		if (!TryFindLaunchedPid(instance, startedAtUtc, TimeSpan.FromSeconds(8.0), out pid))
		{
			detail = "Task started but process PID was not discovered in time";
			return false;
		}

		detail = "TaskPath=" + taskPath;
		return true;
	}

	private static string BuildInstanceLaunchTaskPath(InstanceConfig instance)
	{
		string safeId = string.IsNullOrWhiteSpace(instance.Id)
			? "default"
			: instance.Id.Replace("\\", "_").Replace("/", "_").Replace(":", "_");
		return "\\SunshineMultiInstanceManager\\Launch_" + safeId;
	}

	private static void EnsureTaskFolder(Microsoft.Win32.TaskScheduler.TaskService taskService)
	{
		const string folderName = "SunshineMultiInstanceManager";
		try
		{
			taskService.GetFolder("\\" + folderName);
		}
		catch
		{
			try
			{
				taskService.RootFolder.CreateFolder(folderName, null, exceptionOnExists: false);
			}
			catch
			{
			}
		}
	}

	private static bool TryFindLaunchedPid(InstanceConfig instance, DateTime startedAtUtc, TimeSpan timeout, out int pid)
	{
		pid = 0;
		DateTime deadline = DateTime.UtcNow.Add(timeout);
		string normalizedConfPath = NormalizePathForCompare(instance.SunshineConfPath);
		string normalizedExePath = NormalizePathForCompare(instance.ResolvedExecutablePath);

		while (DateTime.UtcNow < deadline)
		{
			string query = "SELECT ProcessId, CommandLine, ExecutablePath, CreationDate FROM Win32_Process WHERE Name='sunshine.exe'";
			ManagementObjectCollection? results = null;
			try
			{
				using ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
				results = searcher.Get();
				foreach (ManagementObject item in results)
				{
					if (item["ProcessId"] is not uint rawPid || rawPid == 0)
					{
						continue;
					}

					string normalizedCmd = NormalizePathForCompare(item["CommandLine"] as string ?? string.Empty);
					string normalizedProcExe = NormalizePathForCompare(item["ExecutablePath"] as string ?? string.Empty);

					bool matches = normalizedCmd.Contains(normalizedConfPath, StringComparison.OrdinalIgnoreCase)
						&& (string.IsNullOrEmpty(normalizedProcExe)
							|| normalizedProcExe.Equals(normalizedExePath, StringComparison.OrdinalIgnoreCase));

					if (!matches)
					{
						continue;
					}

					if (item["CreationDate"] is string creationRaw)
					{
						DateTime createdUtc = ManagementDateTimeConverter.ToDateTime(creationRaw).ToUniversalTime();
						if (createdUtc < startedAtUtc.AddSeconds(-2.0))
						{
							continue;
						}
					}

					pid = (int)rawPid;
					return true;
				}
			}
			catch
			{
			}
			finally
			{
				results?.Dispose();
			}

			Thread.Sleep(200);
		}

		return false;
	}

	private static string NormalizePathForCompare(string value)
	{
		return value.Replace('/', '\\').Trim().Trim('"');
	}

	private static string BuildHiddenPowerShellLauncherArgs(string executablePath, string arguments, string workingDirectory)
	{
		string exe = EscapePowerShellSingleQuoted(executablePath);
		string args = EscapePowerShellSingleQuoted(arguments);
		string cwd = EscapePowerShellSingleQuoted(workingDirectory);
		string psCommand = "Start-Process -FilePath '" + exe + "' -ArgumentList '" + args + "' -WorkingDirectory '" + cwd + "' -WindowStyle Hidden";
		return "-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -Command \"" + psCommand.Replace("\"", "\\\"") + "\"";
	}

	private static string EscapePowerShellSingleQuoted(string value)
	{
		return (value ?? string.Empty).Replace("'", "''");
	}
}
