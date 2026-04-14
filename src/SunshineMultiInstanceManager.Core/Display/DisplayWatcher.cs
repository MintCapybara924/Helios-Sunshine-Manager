using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Helios.Core.Process;
using Helios.Core.Storage;
using Helios.Core.Storage.Models;

namespace Helios.Core.Display;

public sealed class DisplayWatcher : IDisposable
{
	private readonly ProcessManager _pm;

	private readonly SettingsStore _store;

	private readonly ILogger _logger;

	private readonly object _timerLock = new object();

	private const int DebounceMs = 3000;
	private long _lastDisplaySignature;

	private Timer? _debounceTimer;

	private bool _disposed;

	public DisplayWatcher(ProcessManager pm, SettingsStore store, ILogger logger)
	{
		_pm = pm;
		_store = store;
		_logger = logger;
		_lastDisplaySignature = GetDisplaySignature();
	}

	public void OnDisplayChanged()
	{
		if (_disposed)
		{
			return;
		}
		_logger.LogDebug("Received WM_DISPLAYCHANGE. Debouncing for {Debounce}ms.", DebounceMs);
		lock (_timerLock)
		{
			_debounceTimer?.Dispose();
			_debounceTimer = new Timer(HandleDisplayChangeCore, null, DebounceMs, -1);
		}
	}

	private void HandleDisplayChangeCore(object? state)
	{
		lock (_timerLock)
		{
			_debounceTimer?.Dispose();
			_debounceTimer = null;
		}
		if (_disposed || !_store.Settings.SyncOnDisplayChange)
		{
			return;
		}

		long currentSignature = GetDisplaySignature();
		if (currentSignature == _lastDisplaySignature)
		{
			_logger.LogInformation("WM_DISPLAYCHANGE ignored because display signature is unchanged.");
			return;
		}
		_lastDisplaySignature = currentSignature;

		if (IsSecureDesktopActive())
		{
			_logger.LogInformation("WM_DISPLAYCHANGE ignored because secure desktop is active (likely UAC prompt).");
			return;
		}

		if (IsUacPromptLikelyActive())
		{
			_logger.LogInformation("WM_DISPLAYCHANGE ignored because UAC prompt process is active.");
			return;
		}

		_logger.LogInformation("WM_DISPLAYCHANGE detected; restarting enabled instances.");
		Task.Run(async delegate
		{
			List<InstanceConfig> list = _store.Settings.Instances.Where((InstanceConfig i) => i.Enabled).ToList();
			foreach (InstanceConfig inst in list)
			{
				if (_disposed)
				{
					return;
				}
				try
				{
					await _pm.RestartInstanceAsync(inst);
				}
				catch (Exception ex)
				{
					_logger.LogWarning("WM_DISPLAYCHANGE restart failed for instance [{Name}]: {Msg}", inst.Name, ex.Message);
				}
			}
			_logger.LogInformation("WM_DISPLAYCHANGE restart sequence completed.");
		});
	}

	private static bool IsSecureDesktopActive()
	{
		nint desktop = IntPtr.Zero;
		try
		{
			desktop = OpenInputDesktop(0, false, DESKTOP_READOBJECTS);
			if (desktop == IntPtr.Zero)
			{
				return false;
			}

			StringBuilder name = new(128);
			if (!GetUserObjectInformation(desktop, UOI_NAME, name, name.Capacity * sizeof(char), out _))
			{
				return false;
			}

			return name.ToString().Equals("Winlogon", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
		finally
		{
			if (desktop != IntPtr.Zero)
			{
				CloseDesktop(desktop);
			}
		}
	}

	private const uint DESKTOP_READOBJECTS = 0x0001;
	private const int UOI_NAME = 2;
	private const int SM_CXSCREEN = 0;
	private const int SM_CYSCREEN = 1;
	private const int SM_CMONITORS = 80;
	private const int BITSPIXEL = 12;
	private const int PLANES = 14;

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern bool GetUserObjectInformation(nint hObj, int nIndex, StringBuilder pvInfo, int nLength, out int lpnLengthNeeded);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool CloseDesktop(nint hDesktop);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int nIndex);

	[DllImport("gdi32.dll")]
	private static extern int GetDeviceCaps(nint hdc, int index);

	[DllImport("user32.dll")]
	private static extern nint GetDC(nint hWnd);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(nint hWnd, nint hdc);

	private static long GetDisplaySignature()
	{
		int width = GetSystemMetrics(SM_CXSCREEN);
		int height = GetSystemMetrics(SM_CYSCREEN);
		int monitors = GetSystemMetrics(SM_CMONITORS);
		int bpp = 0;

		nint hdc = GetDC(IntPtr.Zero);
		if (hdc != IntPtr.Zero)
		{
			try
			{
				int bits = GetDeviceCaps(hdc, BITSPIXEL);
				int planes = GetDeviceCaps(hdc, PLANES);
				bpp = bits * planes;
			}
			finally
			{
				ReleaseDC(IntPtr.Zero, hdc);
			}
		}

		long signature = width;
		signature = (signature * 397) ^ height;
		signature = (signature * 397) ^ monitors;
		signature = (signature * 397) ^ bpp;
		return signature;
	}

	private static bool IsUacPromptLikelyActive()
	{
		try
		{
			if (System.Diagnostics.Process.GetProcessesByName("consent").Length > 0)
			{
				return true;
			}

			if (System.Diagnostics.Process.GetProcessesByName("CredentialUIBroker").Length > 0)
			{
				return true;
			}
		}
		catch
		{
		}

		return false;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		lock (_timerLock)
		{
			_debounceTimer?.Dispose();
			_debounceTimer = null;
		}
	}
}

