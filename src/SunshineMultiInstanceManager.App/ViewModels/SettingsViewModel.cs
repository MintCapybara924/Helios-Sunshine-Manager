using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helios.App.Services;
using Helios.Core.Audio;
using Helios.Core.Process;
using Helios.Core.Profiles;
using Helios.Core.Scheduler;
using Helios.Core.Storage;
using Helios.Core.Update;

namespace Helios.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
	private readonly SchedulerService _scheduler;
	private readonly SettingsStore _store;
	private readonly VolumeMonitor _volumeMonitor;
	private readonly IInstanceController _pm;
	private readonly InstallerService _installer;
	private readonly ILogger _logger;
	private bool _suppressCallbacks;

	[ObservableProperty]
	private bool isAutoStartEnabled;

	[ObservableProperty]
	private bool isAutoStartBusy;

	[ObservableProperty]
	private bool isSyncVolumeEnabled;

	[ObservableProperty]
	private bool isSyncOnDisplayChange;

	[ObservableProperty]
	private bool isRemoveDisplayOnDisconnect;

	[ObservableProperty]
	private bool isSunshineServiceInstalled;

	[ObservableProperty]
	private ServiceControllerStatus? sunshineServiceStatus;

	[ObservableProperty]
	private ServiceStartMode? sunshineServiceStartMode;

	[ObservableProperty]
	private bool isServiceBusy;

	[ObservableProperty]
	private bool includePreRelease;

	[ObservableProperty]
	private bool isCheckingUpdate;

	[ObservableProperty]
	private bool isInstalling;

	[ObservableProperty]
	private int downloadProgress = -1;

	[ObservableProperty]
	private string? installedVersion;

	[ObservableProperty]
	private ReleaseInfo? latestRelease;

	[ObservableProperty]
	private string statusMessage = string.Empty;

	[ObservableProperty]
	private string selectedLanguageCode = "system";

	[ObservableProperty]
	private string selectedProductCode = "sunshine";

	[ObservableProperty]
	private string selectedProductTargetFolder = string.Empty;

	public IReadOnlyList<LanguageOption> LanguageOptions => LocalizationService.GetLanguageOptions();
	public IReadOnlyList<ManagedProductDefinition> ProductOptions => ManagedProductCatalog.All;

	private ManagedProductDefinition CurrentProduct => ManagedProductCatalog.GetByCode(SelectedProductCode);
	private string? CurrentServiceName => CurrentProduct.WindowsServiceName;
	private static readonly string[] ReminderServiceNames = ["SunshineService", "ApolloService"];

	public string AppNameText => LocalizationService.T("AppName");
	public string AppVersionText => $"v{GetAppVersion()}";

	private static string GetAppVersion()
	{
		Version? v = Assembly.GetExecutingAssembly().GetName().Version;
		return v is null ? "?" : $"{v.Major}.{v.Minor}.{v.Build}";
	}
	public string SettingsText => LocalizationService.T("Settings");
	public string LanguageText => LocalizationService.T("Language");
	public string ProductBranchText => LocalizationService.T("ProductBranch");
	public string ProductBranchHintText => LocalizationService.T("ProductBranchHint");
	public string ProductTargetFolderText => LocalizationService.T("ProductTargetFolder");
	public string ProductTargetFolderHintText => LocalizationService.T("ProductTargetFolderHint");
	public string LanguageHintText => LocalizationService.T("LanguageHint");
	public string GroupVibeshineText => LocalizationService.T("GroupVibeshine");
	public string GroupAutoStartText => LocalizationService.T("GroupAutoStart");
	public string GroupVolumeText => LocalizationService.T("GroupVolume");
	public string GroupDisplayText => LocalizationService.T("GroupDisplay");
	public string GroupServiceText => LocalizationService.T("GroupService");
	public string CheckUpdateText => LocalizationService.T("CheckUpdate");
	public string IncludeBetaText => LocalizationService.T("IncludeBeta");
	public string DownloadActionText => IsVibeshinInstalled ? LocalizationService.T("DownloadUpdate") : LocalizationService.T("DownloadInstall");
	public string RefreshText => LocalizationService.T("Refresh");
	public string DisableServiceText => LocalizationService.T("DisableService");
	public string ServiceNotInstalledText => LocalizationService.T("ServiceNotInstalled");
	public string AutoStartTitleText => LocalizationService.T("AutoStartTitle");
	public string AutoStartDescText => LocalizationService.T("AutoStartDesc");
	public string VolumeTitleText => LocalizationService.T("VolumeTitle");
	public string VolumeDescText => LocalizationService.T("VolumeDesc");
	public string DisplayRestartTitleText => LocalizationService.T("DisplayRestartTitle");
	public string DisplayRestartDescText => LocalizationService.T("DisplayRestartDesc");
	public string DisplayRemoveTitleText => LocalizationService.T("DisplayRemoveTitle");
	public string DisplayRemoveDescText => LocalizationService.T("DisplayRemoveDesc");
	public string VibeshineDescText => LocalizationService.T("VibeshineDesc");
	public string BetaHintText => LocalizationService.T("BetaHint");
	public string AutoStartInfoText => LocalizationService.T("AutoStartInfo");
	public string VolumeInfoText => LocalizationService.T("VolumeInfo");
	public string ServiceInfoText => LocalizationService.T("ServiceInfo");
	public string ServiceCardNameText => CurrentServiceName ?? LocalizationService.T("ServiceCardName");
	public string ServiceTargetsText => string.Join(" / ", ReminderServiceNames);
	public string ServiceReminderHintText => LocalizationService.T("ServiceReminderHint");

	public string InstalledDisplayText => IsVibeshinInstalled ? (InstalledVersion ?? string.Empty) : LocalizationService.T("NotInstalled");

	public bool IsVibeshinInstalled => !string.IsNullOrWhiteSpace(InstalledVersion);
	public bool HasLatestRelease => LatestRelease != null;
	public bool IsUpdateAvailable => LatestRelease != null
		&& !string.Equals(NormalizeVersionText(LatestRelease.TagName), NormalizeVersionText(InstalledVersion), StringComparison.OrdinalIgnoreCase);

	public string UpdateAvailableText
	{
		get
		{
			if (LatestRelease == null)
			{
				return LocalizationService.T("UpdateNoRelease");
			}

			if (!IsVibeshinInstalled)
			{
				return string.Format(LocalizationService.T("UpdateNotInstalledFmt"), LatestRelease.TagName);
			}

			if (IsUpdateAvailable)
			{
				return string.Format(LocalizationService.T("UpdateAvailableFmt"), LatestRelease.TagName, InstalledVersion);
			}

			return string.Format(LocalizationService.T("UpdateLatestFmt"), InstalledVersion);
		}
	}

	public string ServiceStatusText
	{
		get
		{
			if (!IsSunshineServiceInstalled)
			{
				return LocalizationService.T("ServiceStatusNotInstalled");
			}

			var statusText = SunshineServiceStatus switch
			{
				ServiceControllerStatus.Running => LocalizationService.T("ServiceStatusRunning"),
				ServiceControllerStatus.Stopped => LocalizationService.T("ServiceStatusStopped"),
				ServiceControllerStatus.StartPending => LocalizationService.T("ServiceStatusStarting"),
				ServiceControllerStatus.StopPending => LocalizationService.T("ServiceStatusStopping"),
				_ => LocalizationService.T("ServiceStatusUnknown")
			};

			var modeText = SunshineServiceStartMode switch
			{
				ServiceStartMode.Automatic => LocalizationService.T("ServiceStartAuto"),
				ServiceStartMode.Manual => LocalizationService.T("ServiceStartManual"),
				ServiceStartMode.Disabled => LocalizationService.T("ServiceStartDisabled"),
				_ => string.Empty
			};

			return string.IsNullOrEmpty(modeText) ? statusText : $"{statusText} {modeText}";
		}
	}

	public IAsyncRelayCommand CheckForUpdateCommand { get; }
	public IAsyncRelayCommand DownloadAndInstallCommand { get; }
	public IAsyncRelayCommand DisableSunshineServiceCommand { get; }
	public IRelayCommand RefreshServiceStatusCommand { get; }

	public SettingsViewModel(SchedulerService scheduler, SettingsStore store, VolumeMonitor volumeMonitor, IInstanceController pm, InstallerService installer, ILogger logger)
	{
		_scheduler = scheduler;
		_store = store;
		_volumeMonitor = volumeMonitor;
		_pm = pm;
		_installer = installer;
		_logger = logger;
		_suppressCallbacks = true;
		SelectedLanguageCode = string.IsNullOrWhiteSpace(_store.Settings.LanguageCode)
			? LocalizationService.CurrentLanguageCode
			: _store.Settings.LanguageCode;
		SelectedProductCode = string.IsNullOrWhiteSpace(_store.Settings.CurrentProduct)
			? "sunshine"
			: _store.Settings.CurrentProduct;
		SelectedProductTargetFolder = _store.Settings.GetProductInstallPath(SelectedProductCode);
		IsAutoStartEnabled = _scheduler.IsAutoStartEnabled();
		IsSyncVolumeEnabled = _store.Settings.SyncVolume;
		IsSyncOnDisplayChange = _store.Settings.SyncOnDisplayChange;
		IsRemoveDisplayOnDisconnect = _store.Settings.RemoveDisplayOnDisconnect;
		InstalledVersion = _installer.GetInstalledVersion(SelectedProductCode, _store.Settings.GetProductExecutablePath(SelectedProductCode));
		_suppressCallbacks = false;

		CheckForUpdateCommand = new AsyncRelayCommand(CheckForUpdateAsync, () => !IsCheckingUpdate && !IsInstalling);
		DownloadAndInstallCommand = new AsyncRelayCommand(DownloadAndInstallAsync, () => LatestRelease != null && !IsInstalling && !IsCheckingUpdate);
		DisableSunshineServiceCommand = new AsyncRelayCommand(DisableServiceAsync, () => !IsServiceBusy);
		RefreshServiceStatusCommand = new RelayCommand(RefreshServiceStatus);

		RefreshServiceStatus();
	}

	partial void OnSelectedLanguageCodeChanged(string value)
	{
		_store.Settings.LanguageCode = value;
		_ = _store.SaveSettingsAsync();
		LocalizationService.Apply(value);
		OnPropertyChanged(string.Empty);
	}

	partial void OnSelectedProductCodeChanged(string value)
	{
		if (_suppressCallbacks)
		{
			return;
		}

		_suppressCallbacks = true;
		SelectedProductTargetFolder = _store.Settings.GetProductInstallPath(value);
		_suppressCallbacks = false;

		_store.Settings.CurrentProduct = value;
		_ = _store.SaveSettingsAsync();

		InstalledVersion = _installer.GetInstalledVersion(value, _store.Settings.GetProductExecutablePath(value));
		LatestRelease = null;
		StatusMessage = string.Format(LocalizationService.T("StatusBranchSwitchedFmt"), CurrentProduct.DisplayName);

		OnPropertyChanged(nameof(ServiceCardNameText));
		OnPropertyChanged(nameof(IsVibeshinInstalled));
		OnPropertyChanged(nameof(InstalledDisplayText));
		OnPropertyChanged(nameof(DownloadActionText));
		OnPropertyChanged(nameof(UpdateAvailableText));

		RefreshServiceStatus();
	}

	partial void OnSelectedProductTargetFolderChanged(string value)
	{
		if (_suppressCallbacks)
		{
			return;
		}

		string code = SelectedProductCode;
		string oldExecutablePath = _store.Settings.GetProductExecutablePath(code);

		_store.Settings.SetProductInstallPath(code, value);

		string newExecutablePath = _store.Settings.GetProductExecutablePath(code);
		foreach (var instance in _store.Settings.Instances)
		{
			if (!BranchConfigAdapter.NormalizeProductCode(instance.ProductCode).Equals(code, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (string.IsNullOrWhiteSpace(instance.ExecutablePath)
				|| instance.ExecutablePath.Equals(oldExecutablePath, StringComparison.OrdinalIgnoreCase))
			{
				instance.ExecutablePath = newExecutablePath;
			}
		}

		_ = _store.SaveSettingsAsync();
		InstalledVersion = _installer.GetInstalledVersion(code, newExecutablePath);
		OnPropertyChanged(nameof(IsVibeshinInstalled));
		OnPropertyChanged(nameof(InstalledDisplayText));
		OnPropertyChanged(nameof(UpdateAvailableText));
		StatusMessage = $"{CurrentProduct.DisplayName} 目標資料夾已更新。";
	}

	partial void OnIsAutoStartEnabledChanged(bool value)
	{
		if (_suppressCallbacks)
		{
			return;
		}

		_ = ApplyAutoStartAsync(value);
	}

	partial void OnIsSyncVolumeEnabledChanged(bool value)
	{
		if (_suppressCallbacks)
		{
			return;
		}

		_ = ApplySyncVolumeAsync(value);
	}

	partial void OnIsSyncOnDisplayChangeChanged(bool value)
	{
		if (_suppressCallbacks)
		{
			return;
		}

		_ = ApplyDisplaySettingAsync(v => _store.Settings.SyncOnDisplayChange = v, value, value ? "顯示器變更自動重啟已啟用" : "顯示器變更自動重啟已停用");
	}

	partial void OnIsRemoveDisplayOnDisconnectChanged(bool value)
	{
		if (_suppressCallbacks)
		{
			return;
		}

		_ = ApplyDisplaySettingAsync(v => _store.Settings.RemoveDisplayOnDisconnect = v, value, value ? "斷線移除虛擬顯示器已啟用" : "斷線移除虛擬顯示器已停用");
	}

	partial void OnLatestReleaseChanged(ReleaseInfo? value)
	{
		OnPropertyChanged(nameof(UpdateAvailableText));
		OnPropertyChanged(nameof(HasLatestRelease));
		OnPropertyChanged(nameof(IsUpdateAvailable));
		DownloadAndInstallCommand.NotifyCanExecuteChanged();
	}

	partial void OnIsCheckingUpdateChanged(bool value)
	{
		CheckForUpdateCommand.NotifyCanExecuteChanged();
		DownloadAndInstallCommand.NotifyCanExecuteChanged();
	}

	partial void OnIsInstallingChanged(bool value)
	{
		CheckForUpdateCommand.NotifyCanExecuteChanged();
		DownloadAndInstallCommand.NotifyCanExecuteChanged();
	}

	partial void OnIsSunshineServiceInstalledChanged(bool value)
	{
		OnPropertyChanged(nameof(ServiceStatusText));
		DisableSunshineServiceCommand.NotifyCanExecuteChanged();
	}

	partial void OnSunshineServiceStatusChanged(ServiceControllerStatus? value)
	{
		OnPropertyChanged(nameof(ServiceStatusText));
	}

	partial void OnSunshineServiceStartModeChanged(ServiceStartMode? value)
	{
		OnPropertyChanged(nameof(ServiceStatusText));
		DisableSunshineServiceCommand.NotifyCanExecuteChanged();
	}

	partial void OnIsServiceBusyChanged(bool value)
	{
		DisableSunshineServiceCommand.NotifyCanExecuteChanged();
	}

	private async Task CheckForUpdateAsync()
	{
		IsCheckingUpdate = true;
		try
		{
			StatusMessage = $"正在查詢 {CurrentProduct.DisplayName} 最新版本…";
			LatestRelease = IncludePreRelease
				? await _installer.GetLatestAnyReleaseAsync(SelectedProductCode)
				: await _installer.GetLatestStableReleaseAsync(SelectedProductCode);

			if (LatestRelease == null)
			{
				StatusMessage = $"查詢失敗：無法取得 {CurrentProduct.DisplayName} 版本資訊，請確認網路連線。";
			}
			else
			{
				StatusMessage = IsUpdateAvailable
					? $"發現新版本：{LatestRelease.TagName}（{LatestRelease.SizeText}）"
					: $"已是最新版（{LatestRelease.TagName}）";
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning("Check update failed: {Msg}", ex.Message);
			StatusMessage = "查詢失敗：" + ex.Message;
		}
		finally
		{
			IsCheckingUpdate = false;
		}
	}

	private async Task DownloadAndInstallAsync()
	{
		if (LatestRelease == null)
		{
			return;
		}

		IsInstalling = true;
		DownloadProgress = 0;
		var tempPath = Path.Combine(Path.GetTempPath(), $"{CurrentProduct.Code}_setup.exe");
		try
		{
			_logger.LogInformation("Update flow started: tag={Tag}", LatestRelease.TagName);
			StatusMessage = "正在停止所有實例…";
			await _pm.StopAllAsync();
			_logger.LogInformation("All instances stopped for update");

			StatusMessage = $"正在下載 {CurrentProduct.DisplayName} {LatestRelease.TagName}（{LatestRelease.SizeText}）…";
			var progress = new Progress<int>(pct =>
			{
				DownloadProgress = pct;
				if (pct >= 0)
				{
					StatusMessage = $"下載中… {pct}%（{LatestRelease.SizeText}）";
				}
			});

			await _installer.DownloadAsync(LatestRelease.DownloadUrl, tempPath, progress);
			_logger.LogInformation("Installer download completed: {Path}", tempPath);
			DownloadProgress = 100;

			string preferredInstallPath = _store.Settings.GetProductInstallPath(SelectedProductCode);
			bool supportsPathOverride = _installer.SupportsInstallDirectoryOverride(SelectedProductCode, tempPath);
			if (!supportsPathOverride && NeedsManualPathConflictWarning(SelectedProductCode))
			{
				ShowManualPathConflictWarning(preferredInstallPath);
			}

			StatusMessage = supportsPathOverride
				? LocalizationService.T("InstallerStarted")
				: string.Format(LocalizationService.T("InstallerStartedManualPathFmt"), preferredInstallPath);
			_logger.LogInformation("Launching installer executable");
			var exitCode = await _installer.RunInstallerAsync(
				tempPath,
				supportsPathOverride ? preferredInstallPath : null,
				SelectedProductCode);
			_logger.LogInformation("Installer finished with exit code: {Code}", exitCode);
			if (exitCode != 0)
			{
				StatusMessage = $"安裝程式退出代碼：{exitCode}（可能已取消或發生錯誤）";
			}
			else
			{
				InstalledVersion = _installer.GetInstalledVersion(SelectedProductCode, _store.Settings.GetProductExecutablePath(SelectedProductCode));
				OnPropertyChanged(nameof(IsVibeshinInstalled));
				OnPropertyChanged(nameof(InstalledDisplayText));
				OnPropertyChanged(nameof(DownloadActionText));
				StatusMessage = !string.IsNullOrWhiteSpace(InstalledVersion)
					? "安裝完成！已安裝版本：" + InstalledVersion
					: "安裝完成！（無法讀取版本號）";

				await _pm.StartAllAsync();
			}
		}
		catch (OperationCanceledException)
		{
			StatusMessage = "下載已取消。";
		}
		catch (Exception ex)
		{
			StatusMessage = "安裝失敗：" + ex.Message;
			_logger.LogWarning("DownloadAndInstall failed: {Msg}", ex.Message);
		}
		finally
		{
			IsInstalling = false;
			DownloadProgress = -1;
			try
			{
				if (File.Exists(tempPath))
				{
					File.Delete(tempPath);
				}
			}
			catch
			{
			}
		}
	}

	private static bool NeedsManualPathConflictWarning(string? productCode)
	{
		return string.Equals(productCode, "vibeshine", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(productCode, "vibepollo", StringComparison.OrdinalIgnoreCase);
	}

	private static void ShowManualPathConflictWarning(string preferredInstallPath)
	{
		string message = string.Format(
			LocalizationService.T("InstallerPathWarningMessageFmt"),
			preferredInstallPath);

		MessageBox.Show(
			message,
			LocalizationService.T("InstallerPathWarningTitle"),
			MessageBoxButton.OK,
			MessageBoxImage.Warning);
	}

	private async Task DisableServiceAsync()
	{
		IsServiceBusy = true;
		StatusMessage = $"正在停用 {ServiceTargetsText}…";
		try
		{
			int disabled = 0;
			int missing = 0;
			int failed = 0;

			await Task.Run(() =>
			{
				foreach (string serviceName in ReminderServiceNames)
				{
					if (!ServiceControllerHelper.IsInstalled(serviceName))
					{
						missing++;
						continue;
					}

					try
					{
						ServiceControllerHelper.StopAndDisable(serviceName);
						disabled++;
					}
					catch
					{
						failed++;
					}
				}
			});

			StatusMessage = $"服務處理完成：已停用 {disabled}，未安裝 {missing}，失敗 {failed}";
		}
		catch (UnauthorizedAccessException)
		{
			StatusMessage = "操作失敗：停止服務需要管理員權限，請以系統管理員身分執行本程式。";
		}
		catch (Exception ex)
		{
			StatusMessage = "停止服務失敗：" + ex.Message;
			_logger.LogWarning("Disable service failed: {Msg}", ex.Message);
		}
		finally
		{
			IsServiceBusy = false;
			RefreshServiceStatus();
		}
	}

	private void RefreshServiceStatus()
	{
		IsSunshineServiceInstalled = ReminderServiceNames.Any(ServiceControllerHelper.IsInstalled);
		if (IsSunshineServiceInstalled)
		{
			SunshineServiceStatus = ServiceControllerHelper.GetStatus("SunshineService");
			SunshineServiceStartMode = ServiceControllerHelper.GetStartMode("SunshineService");
		}
		else
		{
			SunshineServiceStatus = null;
			SunshineServiceStartMode = null;
		}
	}

	private async Task ApplyAutoStartAsync(bool enable)
	{
		IsAutoStartBusy = true;
		try
		{
			await Task.Run(() =>
			{
				if (enable)
				{
					_scheduler.EnableAutoStart();
				}
				else
				{
					_scheduler.DisableAutoStart();
				}
			});

			_store.Settings.AutoStart = enable;
			await _store.SaveSettingsAsync();
			StatusMessage = enable
				? "開機自啟已啟用（Task Scheduler 工作已建立）"
				: "開機自啟已停用（Task Scheduler 工作已刪除）";
		}
		catch (UnauthorizedAccessException)
		{
			_suppressCallbacks = true;
			IsAutoStartEnabled = !enable;
			_suppressCallbacks = false;
			StatusMessage = "操作失敗：需要管理員權限才能修改工作排程器，請以系統管理員身分執行本程式。";
		}
		catch (Exception ex)
		{
			_suppressCallbacks = true;
			IsAutoStartEnabled = !enable;
			_suppressCallbacks = false;
			StatusMessage = "操作失敗：" + ex.Message;
			_logger.LogWarning("ApplyAutoStart failed: {Msg}", ex.Message);
		}
		finally
		{
			IsAutoStartBusy = false;
		}
	}

	private async Task ApplySyncVolumeAsync(bool enable)
	{
		try
		{
			if (enable)
			{
				_volumeMonitor.Start();
			}
			else
			{
				_volumeMonitor.Stop();
			}

			_store.Settings.SyncVolume = enable;
			await _store.SaveSettingsAsync();
			StatusMessage = enable ? "音量同步已啟用" : "音量同步已停用";
		}
		catch (Exception ex)
		{
			_suppressCallbacks = true;
			IsSyncVolumeEnabled = !enable;
			_suppressCallbacks = false;
			StatusMessage = "音量同步切換失敗：" + ex.Message;
			_logger.LogWarning("ApplySyncVolume failed: {Msg}", ex.Message);
		}
	}

	private async Task ApplyDisplaySettingAsync(Action<bool> applyToSettings, bool value, string okMessage)
	{
		try
		{
			applyToSettings(value);
			await _store.SaveSettingsAsync();
			StatusMessage = okMessage;
		}
		catch (Exception ex)
		{
			StatusMessage = "儲存設定失敗：" + ex.Message;
			_logger.LogWarning("ApplyDisplaySetting failed: {Msg}", ex.Message);
		}
	}

	private static string NormalizeVersionText(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		string text = value.Trim();
		if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
		{
			text = text[1..];
		}

		return text;
	}
}
