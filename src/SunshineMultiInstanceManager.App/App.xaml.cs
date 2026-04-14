using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.ServiceProcess;
using SunshineMultiInstanceManager.App.Services;
using SunshineMultiInstanceManager.App.ViewModels;
using SunshineMultiInstanceManager.App.Views;
using SunshineMultiInstanceManager.Core.Audio;
using SunshineMultiInstanceManager.Core.Display;
using SunshineMultiInstanceManager.Core.Process;
using SunshineMultiInstanceManager.Core.Scheduler;
using SunshineMultiInstanceManager.Core.Storage;
using SunshineMultiInstanceManager.Core.Update;
using Wpf.Ui.Appearance;

namespace SunshineMultiInstanceManager.App;

public partial class App : Application
{
	public static bool AllowClose { get; private set; }
	private static readonly string[] ConflictingServiceNames = ["SunshineService", "ApolloService"];

	private const string SingleInstanceMutexName = @"Local\SunshineMultiInstanceManager.SingleInstance";
	private Mutex? _singleInstanceMutex;
	private bool _ownsSingleInstanceMutex;

	private readonly CancellationTokenSource _cts = new();
	private SystrayService? _systray;
	private SettingsStore? _store;
	private AppLogger? _logger;
	private IInstanceController? _instanceController;
	private MainViewModel? _mainVm;
	private readonly Queue<string> _pendingLogLines = new();

	protected override async void OnStartup(StartupEventArgs e)
	{
		_singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew);
		_ownsSingleInstanceMutex = createdNew;
		if (!createdNew)
		{
			MessageBox.Show(
				"Sunshine Multi-Instance Manager is already running.",
				"Already Running",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
			Shutdown();
			return;
		}

		base.OnStartup(e);

		_logger = new AppLogger(onLog: OnLogLineProduced);
		EnforceConflictingServicesDisabled(_logger);
		DispatcherUnhandledException += OnDispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

		var logger = _logger;
		_store = new SettingsStore();
		await _store.LoadAsync(_cts.Token);

		var scheduler = new SchedulerService(logger);
		var audioService = new AudioDeviceService();
		var volumeMonitor = new VolumeMonitor(audioService, _store, logger);
		var installer = new InstallerService(logger);

		DisplayWatcher? displayWatcher = null;
		TryEnsureSpawnerServiceInstalled(logger);
		bool serviceInstalled = ServiceControllerHelper.IsInstalled(ServiceControlConstants.ServiceName);
		if (!serviceInstalled)
		{
			logger.LogError("Manager service is not installed. Service-only mode requires the spawner service.");
			MessageBox.Show(
				"Service-only mode is enabled, but manager service is not installed.\nPlease run as administrator and repair/install the service.",
				"Service Required",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
			Shutdown();
			return;
		}

		TryStartManagerService(logger);
		bool pipeReady = await WaitForServicePipeReadyAsync(TimeSpan.FromSeconds(12), _cts.Token);
		if (!pipeReady)
		{
			logger.LogError("Manager service pipe is unavailable. Service-only mode will not fall back to local controller.");
			MessageBox.Show(
				"Service-only mode is enabled, but manager service pipe is unavailable.\nPlease verify the SunshineMultiInstanceManager service is running.",
				"Service Unavailable",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
			Shutdown();
			return;
		}

		_instanceController = new ServiceInstanceController(_store, logger);
		logger.LogInformation("Using service-backed instance controller.");

		LocalizationService.Apply(_store.Settings.LanguageCode);
		ApplyUiTheme(_store.Settings.UiTheme);
		try
		{
			await _instanceController.StartAllAsync(_cts.Token);
		}
		catch (Exception ex)
		{
			logger.LogWarning("Auto-start instances failed: {Msg}", ex.Message);
		}

		var mainVm = new MainViewModel(_store, _instanceController, audioService);
		_mainVm = mainVm;
		FlushPendingLogLines();
		var settingsVm = new SettingsViewModel(scheduler, _store, volumeMonitor, _instanceController, installer, logger);
		var mainWindow = new MainWindow(mainVm, settingsVm, _store, displayWatcher);

		MainWindow = mainWindow;
		_systray = new SystrayService();
		_systray.Initialize(mainWindow);

		mainWindow.Show();
	}

	public async Task RequestShutdownAsync(bool stopInstances)
	{
		AllowClose = true;
		try
		{
			if (stopInstances && _instanceController != null)
			{
				using CancellationTokenSource stopCts = new(TimeSpan.FromSeconds(12));
				await _instanceController.StopAllAsync(stopCts.Token);
			}

			if (_store != null)
			{
				await _store.SaveTransientAsync(_cts.Token);
			}
		}
		catch
		{
		}

		Shutdown();

		// If shutdown is blocked by a stuck UI/dispatcher path, force process exit as fallback.
		_ = Task.Run(async () =>
		{
			await Task.Delay(3000);
			Environment.Exit(0);
		});
	}

	protected override void OnExit(ExitEventArgs e)
	{
		DispatcherUnhandledException -= OnDispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
		TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
		_systray?.Dispose();
		if (_ownsSingleInstanceMutex)
		{
			_singleInstanceMutex?.ReleaseMutex();
		}
		_singleInstanceMutex?.Dispose();
		_cts.Cancel();
		if (_instanceController != null)
		{
			try
			{
				Task.Run(async () => await _instanceController.DisposeAsync()).Wait(TimeSpan.FromSeconds(3));
			}
			catch
			{
			}
		}
		_cts.Dispose();
		base.OnExit(e);
	}

	private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		_logger?.LogError(e.Exception, "Unhandled UI exception");
		e.Handled = true;
	}

	private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception ex)
		{
			_logger?.LogError(ex, "Unhandled domain exception");
		}
	}

	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		_logger?.LogError(e.Exception, "Unobserved task exception");
		e.SetObserved();
	}

	private static void ApplyUiTheme(string themeCode)
	{
		var theme = string.Equals(themeCode, "dark", StringComparison.OrdinalIgnoreCase)
			? ApplicationTheme.Dark
			: ApplicationTheme.Light;

		ApplicationThemeManager.Apply(theme);
	}

	private static void TryEnsureSpawnerServiceInstalled(AppLogger logger)
	{
		try
		{
			string serviceName = ServiceControlConstants.ServiceName;
			string? spawnerExe = ServiceControllerHelper.FindSpawnerExePath();
			if (spawnerExe == null)
			{
				logger.LogDebug("Spawner executable not found next to app; skipping auto-install.");
				return;
			}

			if (ServiceControllerHelper.InstallService(serviceName, spawnerExe))
			{
				logger.LogInformation("Spawner service registered: {Path}", spawnerExe);
			}
			else
			{
				logger.LogWarning("Failed to register spawner service.");
			}
		}
		catch (Exception ex)
		{
			logger.LogWarning("Auto-install spawner service failed: {Msg}", ex.Message);
		}
	}

	private static void EnforceConflictingServicesDisabled(AppLogger logger)
	{
		foreach (string serviceName in ConflictingServiceNames)
		{
			try
			{
				if (!ServiceControllerHelper.IsInstalled(serviceName))
				{
					continue;
				}

				ServiceControllerHelper.StopAndDisable(serviceName);
				logger.LogInformation("Conflicting service disabled at startup: {ServiceName}", serviceName);
			}
			catch (Exception ex)
			{
				logger.LogWarning("Failed to disable conflicting service {ServiceName}: {Msg}", serviceName, ex.Message);
			}
		}
	}

	private static void TryStartManagerService(AppLogger logger)
	{
		try
		{
			ServiceControllerStatus? status = ServiceControllerHelper.GetStatus(ServiceControlConstants.ServiceName);
			if (status == null || status == ServiceControllerStatus.Running || status == ServiceControllerStatus.StartPending)
			{
				return;
			}

			using ServiceController service = new(ServiceControlConstants.ServiceName);
			service.Start();
			service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8));
			logger.LogInformation("Manager service started from app startup.");
		}
		catch (Exception ex)
		{
			logger.LogWarning("Failed to start manager service: {Msg}", ex.Message);
		}
	}

	private static async Task<bool> WaitForServicePipeReadyAsync(TimeSpan timeout, CancellationToken ct)
	{
		DateTime deadline = DateTime.UtcNow.Add(timeout);
		while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
		{
			if (await ServiceInstanceController.IsPipeAvailableAsync(ct))
			{
				return true;
			}

			await Task.Delay(500, ct);
		}

		return false;
	}

	private void OnLogLineProduced(string line)
	{
		if (_mainVm == null)
		{
			lock (_pendingLogLines)
			{
				_pendingLogLines.Enqueue(line);
				while (_pendingLogLines.Count > 300)
				{
					_ = _pendingLogLines.Dequeue();
				}
			}
			return;
		}

		if (Dispatcher.CheckAccess())
		{
			_mainVm.AppendLogLine(line);
			return;
		}

		Dispatcher.Invoke(() => _mainVm.AppendLogLine(line));
	}

	private void FlushPendingLogLines()
	{
		if (_mainVm == null)
		{
			return;
		}

		Queue<string> lines;
		lock (_pendingLogLines)
		{
			lines = new Queue<string>(_pendingLogLines);
			_pendingLogLines.Clear();
		}

		while (lines.Count > 0)
		{
			_mainVm.AppendLogLine(lines.Dequeue());
		}
	}
}
