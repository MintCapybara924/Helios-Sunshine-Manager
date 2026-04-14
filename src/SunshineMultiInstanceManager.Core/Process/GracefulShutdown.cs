using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SunshineMultiInstanceManager.Core.Process;

public static class GracefulShutdown
{
	public static async Task<bool> ShutdownAsync(int pid, int timeoutMs, ILogger? logger = null, CancellationToken ct = default(CancellationToken))
	{
		if (!IsAlive(pid))
		{
			logger?.LogDebug("PID={Pid} is already gone; skip shutdown sequence.", pid);
			return true;
		}
		int halfTimeout = timeoutMs / 2;
		logger?.LogDebug("PID={Pid}: posting WM_CLOSE to all windows.", pid);
		PostWmCloseToAllWindows(pid);
		if (await WaitForExitAsync(pid, halfTimeout, ct))
		{
			logger?.LogInformation("PID={Pid}: exited after WM_CLOSE.", pid);
			return true;
		}
		if (!ct.IsCancellationRequested)
		{
			logger?.LogDebug("PID={Pid}: sending CTRL_BREAK_EVENT.", pid);
			SendCtrlBreak(pid, logger);
			if (await WaitForExitAsync(pid, halfTimeout, ct))
			{
				logger?.LogInformation("PID={Pid}: exited after CTRL_BREAK_EVENT.", pid);
				return true;
			}
		}
		logger?.LogWarning("PID={Pid}: still alive after {Timeout}ms; force terminating.", pid, timeoutMs);
		ForceTerminate(pid, logger);
		return !IsAlive(pid);
	}

	public static void ForceTerminate(int pid, ILogger? logger = null)
	{
		nint num = NativeMethods.OpenProcess(1025u, bInheritHandle: false, pid);
		if (num == IntPtr.Zero)
		{
			logger?.LogDebug("OpenProcess failed for PID={Pid} (might have already exited), Win32Error={Err}", pid, Marshal.GetLastWin32Error());
			return;
		}
		try
		{
			if (!NativeMethods.TerminateProcess(num, 1u))
			{
				logger?.LogWarning("TerminateProcess failed for PID={Pid}, Win32Error={Err}", pid, Marshal.GetLastWin32Error());
			}
		}
		finally
		{
			NativeMethods.CloseHandle(num);
		}
	}

	private static void PostWmCloseToAllWindows(int pid)
	{
		NativeMethods.EnumWindows(delegate(nint hWnd, nint _)
		{
			NativeMethods.GetWindowThreadProcessId(hWnd, out var lpdwProcessId);
			if (lpdwProcessId == (uint)pid)
			{
				NativeMethods.PostMessage(hWnd, 16u, IntPtr.Zero, IntPtr.Zero);
			}
			return true;
		}, IntPtr.Zero);
	}

	private static void SendCtrlBreak(int pid, ILogger? logger)
	{
		NativeMethods.SetConsoleCtrlHandler(null, add: true);
		bool flag = false;
		try
		{
			NativeMethods.FreeConsole();
			flag = NativeMethods.AttachConsole(pid);
			if (!flag)
			{
				logger?.LogDebug("AttachConsole PID={Pid} failed, Win32Error={Err}.", pid, Marshal.GetLastWin32Error());
			}
			else if (!NativeMethods.GenerateConsoleCtrlEvent(1u, (uint)pid))
			{
				logger?.LogDebug("GenerateConsoleCtrlEvent failed for PID={Pid}, Win32Error={Err}", pid, Marshal.GetLastWin32Error());
			}
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Unexpected error while sending Ctrl-Break to PID={Pid}.", pid);
		}
		finally
		{
			if (flag)
			{
				NativeMethods.FreeConsole();
			}
			NativeMethods.SetConsoleCtrlHandler(null, add: false);
		}
	}

	private static async Task<bool> WaitForExitAsync(int pid, int timeoutMs, CancellationToken ct)
	{
		DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
		while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
		{
			if (!IsAlive(pid))
			{
				return true;
			}
			int val = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
			int num = Math.Min(250, Math.Max(0, val));
			if (num == 0)
			{
				break;
			}
			try
			{
				await Task.Delay(num, ct);
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
		return !IsAlive(pid);
	}

	internal static bool IsAlive(int pid)
	{
		if (pid <= 0)
		{
			return false;
		}
		try
		{
			using System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(pid);
			return !process.HasExited;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}
}
