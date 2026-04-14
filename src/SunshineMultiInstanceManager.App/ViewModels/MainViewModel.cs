using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helios.App.Services;
using Helios.Core.Audio;
using Helios.Core.Process;
using Helios.Core.Profiles;
using Helios.Core.Storage;
using Helios.Core.Storage.Models;
using Wpf.Ui.Appearance;

namespace Helios.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
	private readonly SettingsStore _store;
	private readonly IInstanceController _pm;
	private readonly AudioDeviceService _audioService;
	private readonly CancellationTokenSource _statusProbeCts = new();
	private readonly Task _statusProbeTask;
	private readonly Queue<string> _logLines = new();
	private const int MaxLogLines = 300;
	private bool _themeInitialized;

	[ObservableProperty]
	private InstanceViewModel? selectedInstance;

	[ObservableProperty]
	private string statusText = string.Empty;

	[ObservableProperty]
	private bool isBusy;

	[ObservableProperty]
	private bool isDarkTheme;

	[ObservableProperty]
	private string liveLogText = string.Empty;

	[ObservableProperty]
	private bool isLogExpanded;

	public bool CanEditSelectedInstance => SelectedInstance != null && !SelectedInstance.IsRunning;
	public bool CanApplyDiscardSelectedInstance => SelectedInstance != null && !SelectedInstance.IsRunning && SelectedInstance.IsDirty;
	public bool CanStartSelectedInstance => SelectedInstance != null && !SelectedInstance.IsRunning && SelectedInstance.EditEnabled;
	public bool CanStopSelectedInstance => SelectedInstance?.IsRunning == true;
	public bool CanOpenWebUiSelectedInstance => SelectedInstance?.IsRunning == true && !(SelectedInstance?.IsRuntimeConfigDirty ?? true);

	public ObservableCollection<InstanceViewModel> Instances { get; } = new();
	public ObservableCollection<AudioDeviceViewModel> AvailableAudioDevices { get; } = new();
	public IReadOnlyList<ProductBranchOptionViewModel> ProductOptions => ManagedProductCatalog.All
		.Select(p =>
		{
			bool installed = File.Exists(_store.Settings.GetProductExecutablePath(p.Code));
			string display = installed
				? p.DisplayName
				: $"{p.DisplayName} ({LocalizationService.T("NotInstalled")})";
			return new ProductBranchOptionViewModel(p.Code, display, installed);
		})
		.ToList();

	public string BusyText => LocalizationService.T("StatusBusy");
	public string ListAddText => LocalizationService.T("ListAdd");
	public string ListCloneText => LocalizationService.T("ListClone");
	public string ListDeleteText => LocalizationService.T("ListDelete");
	public string TipAddText => LocalizationService.T("TipAdd");
	public string TipCloneText => LocalizationService.T("TipClone");
	public string TipDeleteText => LocalizationService.T("TipDelete");
	public string EditorSelectPromptText => LocalizationService.T("EditorSelectPrompt");
	public string EditorApplyText => LocalizationService.T("EditorApply");
	public string EditorDiscardText => LocalizationService.T("EditorDiscard");
	public string EditorStartText => LocalizationService.T("EditorStart");
	public string EditorStopText => LocalizationService.T("EditorStop");
	public string EditorOpenWebUiText => LocalizationService.T("EditorOpenWebUi");
	public string StartAllText => LocalizationService.T("StartAll");
	public string StopAllText => LocalizationService.T("StopAll");
	public string EditorGroupBasicText => LocalizationService.T("GroupBasic");
	public string EditorEnableText => LocalizationService.T("EditorEnable");
	public string EditorEnableHintText => LocalizationService.T("EditorEnableHint");
	public string EditorInstanceNameText => LocalizationService.T("EditorInstanceName");
	public string EditorInstanceNamePlaceholderText => LocalizationService.T("EditorInstanceNamePlaceholder");
	public string EditorPortText => LocalizationService.T("EditorPort");
	public string EditorPortPlaceholderText => LocalizationService.T("EditorPortPlaceholder");
	public string EditorBranchText => LocalizationService.T("EditorBranch");
	public string EditorBranchHintText => LocalizationService.T("EditorBranchHint");
	public string EditorGroupAudioText => LocalizationService.T("GroupAudio");
	public string EditorAudioDescText => LocalizationService.T("EditorAudioDesc");
	public string EditorRefreshDevicesText => LocalizationService.T("EditorRefreshDevices");
	public string EditorGroupDisplayText => LocalizationService.T("GroupDisplay");
	public string EditorHeadlessTitleText => LocalizationService.T("EditorHeadlessTitle");
	public string EditorHeadlessDescText => LocalizationService.T("EditorHeadlessDesc");
	public string EditorTerminateTitleText => LocalizationService.T("EditorTerminateTitle");
	public string EditorTerminateDescText => LocalizationService.T("EditorTerminateDesc");
	public string EditorGroupAdvancedText => LocalizationService.T("GroupAdvanced");
	public string EditorExtraArgsText => LocalizationService.T("EditorExtraArgs");
	public string EditorExtraArgsPlaceholderText => LocalizationService.T("EditorExtraArgsPlaceholder");
	public string EditorExePathText => LocalizationService.T("EditorExePath");
	public string EditorBrowseText => LocalizationService.T("EditorBrowse");
	public string ThemeQuickSwitchText => LocalizationService.T("ThemeQuickSwitch");
	public string ThemeLightText => LocalizationService.T("ThemeLight");
	public string ThemeDarkText => LocalizationService.T("ThemeDark");
	public string CurrentThemeText => IsDarkTheme ? ThemeDarkText : ThemeLightText;
	public string RuntimeLogTitle => "Runtime Log";
	public string ClearLogText => "Clear Log";

	public MainViewModel(SettingsStore store, IInstanceController pm, AudioDeviceService audioService)
	{
		_store = store;
		_pm = pm;
		_audioService = audioService;
		LocalizationService.LanguageChanged += OnLanguageChanged;
		_store.SettingsChanged += OnStoreSettingsChanged;
		_pm.InstanceStateChanged += OnInstanceStateChanged;

		foreach (var model in _store.Settings.Instances)
		{
			var vm = new InstanceViewModel(model, code => _store.Settings.GetProductExecutablePath(code))
			{
				IsRunning = _pm.IsRunning(model.Id)
			};
			HookInstanceViewModel(vm);
			Instances.Add(vm);
		}

		_themeInitialized = false;
		isDarkTheme = string.Equals(_store.Settings.UiTheme, "dark", StringComparison.OrdinalIgnoreCase);
		_themeInitialized = true;

		SelectedInstance = Instances.FirstOrDefault();
		RefreshAudioDevicesCommand.Execute(null);
		_statusProbeTask = ProbeInstanceStateLoopAsync(_statusProbeCts.Token);
	}

	private async Task ProbeInstanceStateLoopAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(TimeSpan.FromSeconds(2), ct);
				foreach (var vm in Instances)
				{
					vm.IsRunning = _pm.IsRunning(vm.Id);
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch
			{
			}
		}
	}

	private void OnInstanceStateChanged(object? sender, InstanceStateChangedEventArgs e)
	{
		void UpdateState()
		{
			var vm = Instances.FirstOrDefault(i => i.Id == e.InstanceId);
			if (vm != null)
			{
				vm.IsRunning = e.IsRunning;
				if (ReferenceEquals(vm, SelectedInstance))
				{
					NotifySelectedInstanceUiStateChanged();
				}
			}
		}

		if (Application.Current?.Dispatcher?.CheckAccess() == true)
		{
			UpdateState();
		}
		else
		{
			Application.Current?.Dispatcher?.Invoke(UpdateState);
		}
	}

	private void OnLanguageChanged()
	{
		_ = RefreshAudioDevices();

		foreach (var instance in Instances)
		{
			instance.NotifyLocalizationChanged();
		}

		OnPropertyChanged(string.Empty);
	}

	private void OnStoreSettingsChanged()
	{
		if (Application.Current?.Dispatcher?.CheckAccess() == true)
		{
			OnPropertyChanged(nameof(ProductOptions));
			return;
		}

		Application.Current?.Dispatcher?.Invoke(() => OnPropertyChanged(nameof(ProductOptions)));
	}

	partial void OnIsDarkThemeChanged(bool value)
	{
		OnPropertyChanged(nameof(CurrentThemeText));

		if (!_themeInitialized)
		{
			return;
		}

		var theme = value ? ApplicationTheme.Dark : ApplicationTheme.Light;
		ApplicationThemeManager.Apply(theme);
		_store.Settings.UiTheme = value ? "dark" : "light";
		_ = _store.SaveSettingsAsync();
		StatusText = string.Format(LocalizationService.T("StatusThemeChangedFmt"), CurrentThemeText);
	}

	[RelayCommand]
	private Task RefreshAudioDevices()
	{
		AvailableAudioDevices.Clear();
		AvailableAudioDevices.Add(AudioDeviceViewModel.CreateAuto());
		foreach (var device in _audioService.EnumerateOutputDevices())
		{
			AvailableAudioDevices.Add(new AudioDeviceViewModel
			{
				DeviceId = device.DeviceId,
				FriendlyName = device.FriendlyName
			});
		}

		foreach (var vm in Instances)
		{
			vm.ResolveAudioDevice(AvailableAudioDevices);
		}

		return Task.CompletedTask;
	}

	[RelayCommand]
	private async Task AddInstanceAsync()
	{
		var model = _store.AddInstance($"Instance {Instances.Count + 1}");
		var vm = new InstanceViewModel(model, code => _store.Settings.GetProductExecutablePath(code));
		HookInstanceViewModel(vm);
		Instances.Add(vm);
		SelectedInstance = vm;
		vm.ResolveAudioDevice(AvailableAudioDevices);
		StatusText = LocalizationService.T("StatusInstanceAdded");
		await TrySaveSettingsSafeAsync();
	}

	[RelayCommand]
	private async Task CloneInstanceAsync()
	{
		if (SelectedInstance == null) return;
		var source = _store.Settings.Instances.FirstOrDefault(x => x.Id == SelectedInstance.Id);
		if (source == null) return;
		var model = _store.AddInstance($"{source.Name} Copy", source);
		var vm = new InstanceViewModel(model, code => _store.Settings.GetProductExecutablePath(code));
		HookInstanceViewModel(vm);
		Instances.Add(vm);
		SelectedInstance = vm;
		vm.ResolveAudioDevice(AvailableAudioDevices);
		StatusText = LocalizationService.T("StatusInstanceCloned");
		await TrySaveSettingsSafeAsync();
	}

	[RelayCommand]
	private async Task DeleteInstanceAsync()
	{
		if (SelectedInstance == null) return;

		var toRemove = SelectedInstance;
		var instance = _store.Settings.Instances.FirstOrDefault(i => i.Id == toRemove.Id);
		if (instance != null)
		{
			try
			{
				await _pm.StopInstanceAsync(instance, CancellationToken.None);
			}
			catch (Exception ex)
			{
				StatusText = $"Stop before delete failed: {ex.Message}";
			}
		}

		_store.RemoveInstance(toRemove.Id);
		UnhookInstanceViewModel(toRemove);
		Instances.Remove(toRemove);
		SelectedInstance = Instances.FirstOrDefault();
		StatusText = LocalizationService.T("StatusInstanceDeleted");
		await TrySaveSettingsSafeAsync();
	}

	[RelayCommand]
	private async Task ApplyAsync()
	{
		if (SelectedInstance == null) return;
		if (!SelectedInstance.IsDirty)
		{
			return;
		}
		if (SelectedInstance.HasValidationErrors)
		{
			StatusText = "Please fix validation errors before applying.";
			return;
		}

		SelectedInstance.SaveToModel();
		StatusText = LocalizationService.T("StatusChangesApplied");
		await TrySaveSettingsSafeAsync();
	}

	[RelayCommand]
	private void DiscardEdits()
	{
		if (SelectedInstance?.IsDirty != true)
		{
			return;
		}

		SelectedInstance?.LoadFromModel();
		StatusText = LocalizationService.T("StatusChangesDiscarded");
	}

	[RelayCommand]
	private async Task StartInstanceAsync()
	{
		if (SelectedInstance == null) return;
		if (SelectedInstance.IsRunning) return;
		if (SelectedInstance.HasValidationErrors)
		{
			StatusText = "Please fix validation errors before starting.";
			return;
		}
		if (!IsBranchExecutableInstalled(SelectedInstance, out string singleMissingMessage))
		{
			StatusText = singleMissingMessage;
			return;
		}

		try
		{
			IsBusy = true;
			SelectedInstance.SaveToModel();
			await TryPersistSettingsForLaunchAsync();

			var instance = _store.Settings.Instances.FirstOrDefault(i => i.Id == SelectedInstance.Id);
			if (instance == null)
			{
				StatusText = "Instance not found.";
				return;
			}

			if (!instance.Enabled)
			{
				StatusText = "Instance is disabled.";
				return;
			}

			await _pm.RestartInstanceAsync(instance, CancellationToken.None);
			SelectedInstance.IsRunning = _pm.IsRunning(instance.Id);
			StatusText = SelectedInstance.IsRunning ? LocalizationService.T("StatusInstanceStarted") : LocalizationService.T("StatusInstanceStopped");
		}
		catch (System.Exception ex)
		{
			StatusText = ex.Message;
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task StopInstanceAsync()
	{
		if (SelectedInstance == null) return;
		if (!SelectedInstance.IsRunning) return;

		try
		{
			IsBusy = true;
			var instance = _store.Settings.Instances.FirstOrDefault(i => i.Id == SelectedInstance.Id);
			if (instance == null)
			{
				StatusText = "Instance not found.";
				return;
			}

			await _pm.StopInstanceAsync(instance, CancellationToken.None);
			SelectedInstance.IsRunning = _pm.IsRunning(instance.Id);
			StatusText = LocalizationService.T("StatusInstanceStopped");
		}
		catch (System.Exception ex)
		{
			StatusText = ex.Message;
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private void OpenWebUi()
	{
		if (SelectedInstance == null)
		{
			return;
		}

		if (SelectedInstance.IsRuntimeConfigDirty)
		{
			StatusText = "偵測到未套用的分支/連接埠/執行檔變更，請先按「套用」並重新啟動此實例，再開啟 Web UI。";
			return;
		}

		if (!SelectedInstance.IsRunning)
		{
			StatusText = "此實例目前未執行，為避免開到其他程序的頁面，請先啟動此實例。";
			return;
		}

		if (SelectedInstance.Port is < 1 or >= 65535)
		{
			StatusText = "Invalid port.";
			return;
		}

		var webUiPort = SelectedInstance.Port + 1;

		var urls = new[]
		{
			$"https://127.0.0.1:{webUiPort}",
			$"http://127.0.0.1:{webUiPort}"
		};

		Exception? lastError = null;
		foreach (var url in urls)
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = url,
					UseShellExecute = true
				});
				StatusText = LocalizationService.T("StatusOpenWebUiRequested");
				return;
			}
			catch (Exception ex)
			{
				lastError = ex;
			}
		}

		StatusText = lastError?.Message ?? "Failed to open Web UI.";
	}

		partial void OnSelectedInstanceChanged(InstanceViewModel? value)
		{
			NotifySelectedInstanceUiStateChanged();
		}

		private void HookInstanceViewModel(InstanceViewModel vm)
		{
			vm.PropertyChanged += OnInstanceViewModelPropertyChanged;
		}

		private void UnhookInstanceViewModel(InstanceViewModel vm)
		{
			vm.PropertyChanged -= OnInstanceViewModelPropertyChanged;
		}

		private void OnInstanceViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is not InstanceViewModel vm || !ReferenceEquals(vm, SelectedInstance))
			{
				return;
			}

			if (string.IsNullOrEmpty(e.PropertyName)
				|| e.PropertyName == nameof(InstanceViewModel.IsRunning)
				|| e.PropertyName == nameof(InstanceViewModel.IsDirty)
				|| e.PropertyName == nameof(InstanceViewModel.IsRuntimeConfigDirty)
				|| e.PropertyName == nameof(InstanceViewModel.EditEnabled))
			{
				NotifySelectedInstanceUiStateChanged();
			}
		}

		private void NotifySelectedInstanceUiStateChanged()
		{
			OnPropertyChanged(nameof(CanEditSelectedInstance));
			OnPropertyChanged(nameof(CanApplyDiscardSelectedInstance));
			OnPropertyChanged(nameof(CanStartSelectedInstance));
			OnPropertyChanged(nameof(CanStopSelectedInstance));
			OnPropertyChanged(nameof(CanOpenWebUiSelectedInstance));
		}

	[RelayCommand]
	private async Task StartAllAsync()
	{
		try
		{
			var invalid = Instances.FirstOrDefault(vm => vm.HasValidationErrors);
			if (invalid != null)
			{
				StatusText = $"Please fix validation errors in '{invalid.EditName}' before starting all.";
				return;
			}

			var missingBranch = Instances.FirstOrDefault(vm => !IsBranchExecutableInstalled(vm, out _));
			if (missingBranch != null && !IsBranchExecutableInstalled(missingBranch, out string allMissingMessage))
			{
				StatusText = allMissingMessage;
				return;
			}

			IsBusy = true;
			foreach (var vm in Instances)
			{
				vm.SaveToModel();
			}
			await TryPersistSettingsForLaunchAsync();
			await _pm.StartAllAsync(CancellationToken.None);
			foreach (var vm in Instances)
			{
				vm.IsRunning = _pm.IsRunning(vm.Id);
			}
			StatusText = LocalizationService.T("StatusAllInstancesStarted");
		}
		catch (System.Exception ex)
		{
			StatusText = ex.Message;
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task StopAllAsync()
	{
		try
		{
			IsBusy = true;
			await _pm.StopAllAsync(CancellationToken.None);
			foreach (var vm in Instances)
			{
				vm.IsRunning = false;
			}
			StatusText = LocalizationService.T("StatusAllInstancesStopped");
		}
		catch (System.Exception ex)
		{
			StatusText = ex.Message;
		}
		finally
		{
			IsBusy = false;
		}
	}

	private bool IsBranchExecutableInstalled(InstanceViewModel vm, out string message)
	{
		string code = BranchConfigAdapter.NormalizeProductCode(vm.EditProductCode);
		ManagedProductDefinition product = ManagedProductCatalog.GetByCode(code);
		string executablePath = string.IsNullOrWhiteSpace(vm.EditExecutablePath)
			? _store.Settings.GetProductExecutablePath(code)
			: vm.EditExecutablePath;

		if (File.Exists(executablePath))
		{
			message = string.Empty;
			return true;
		}

		message = $"Cannot start '{vm.EditName}': {product.DisplayName} is not installed (missing executable: {executablePath}).";
		return false;
	}

	/// <summary>
	/// Toggles the Enabled state of an instance immediately (used by the tray menu).
	/// Persists settings so the new state survives restart.
	/// </summary>
	public async Task ToggleInstanceEnabledAsync(InstanceViewModel vm)
	{
		try
		{
			vm.SetEnabledImmediate(!vm.EditEnabled);
			await _store.SaveSettingsAsync();
			StatusText = vm.EditEnabled
				? LocalizationService.T("StatusInstanceEnabled")
				: LocalizationService.T("StatusInstanceDisabled");
		}
		catch (Exception ex)
		{
			StatusText = ex.Message;
		}
	}

	private async Task TryPersistSettingsForLaunchAsync()
	{
		try
		{
			await _store.SaveSettingsAsync();
		}
		catch (UnauthorizedAccessException)
		{
			// Keep start path alive when ProgramData ACL is temporarily restrictive.
			StatusText = "Settings save skipped due to permissions; launch will continue with current runtime state.";
		}
	}

	private async Task TrySaveSettingsSafeAsync()
	{
		try
		{
			await _store.SaveSettingsAsync();
		}
		catch (UnauthorizedAccessException)
		{
			StatusText = "Access to settings path is denied. Please run once as administrator to repair permissions.";
		}
	}

	public void AppendLogLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return;
		}

		if (Application.Current?.Dispatcher?.CheckAccess() != true)
		{
			Application.Current?.Dispatcher?.Invoke(() => AppendLogLine(line));
			return;
		}

		_logLines.Enqueue(line);
		while (_logLines.Count > MaxLogLines)
		{
			_ = _logLines.Dequeue();
		}

		LiveLogText = string.Join(Environment.NewLine, _logLines);
	}

	[RelayCommand]
	private void ClearLog()
	{
		_logLines.Clear();
		LiveLogText = string.Empty;
	}
}
