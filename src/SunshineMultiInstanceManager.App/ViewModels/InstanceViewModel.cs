using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SunshineMultiInstanceManager.App.Services;
using SunshineMultiInstanceManager.Core.Profiles;
using SunshineMultiInstanceManager.Core.Storage.Models;

namespace SunshineMultiInstanceManager.App.ViewModels;

public partial class InstanceViewModel : ObservableObject
{
	private readonly InstanceConfig _model;
	private readonly Func<string, string> _resolveExecutablePath;
	private string _lastResolvedProductCode;

	[ObservableProperty]
	private bool isRunning;

	[ObservableProperty]
	private string editName = string.Empty;

	[ObservableProperty]
	private string editPort = string.Empty;

	[ObservableProperty]
	private string editProductCode = "sunshine";

	[ObservableProperty]
	private bool editEnabled;

	[ObservableProperty]
	private bool editHeadlessMode;

	[ObservableProperty]
	private bool editTerminateOnPause;

	[ObservableProperty]
	private string editExtraArgs = string.Empty;

	[ObservableProperty]
	private string editExecutablePath = string.Empty;

	[ObservableProperty]
	private AudioDeviceViewModel? editAudioDevice;

	[ObservableProperty]
	private string? portValidationError;

	[ObservableProperty]
	private string? nameValidationError;

	public string Id => _model.Id;
	public string Name => EditName;
	public int Port => int.TryParse(EditPort, out var p) ? p : _model.Port;
	public string RunStatusText => IsRunning
		? LocalizationService.T("RunStatusRunning")
		: LocalizationService.T("RunStatusStopped");
	public string RunStatusColor => IsRunning ? "#34C759" : "#FF3B30";
	public bool HasValidationErrors => !string.IsNullOrWhiteSpace(NameValidationError) || !string.IsNullOrWhiteSpace(PortValidationError);
	public bool IsDirty
	{
		get
		{
			string modelName = (_model.Name ?? string.Empty).Trim();
			string editName = (EditName ?? string.Empty).Trim();
			if (!editName.Equals(modelName, StringComparison.Ordinal))
			{
				return true;
			}

			if (!int.TryParse(EditPort, out int editPortValue))
			{
				editPortValue = _model.Port;
			}

			if (editPortValue != _model.Port)
			{
				return true;
			}

			string editCode = BranchConfigAdapter.NormalizeProductCode(EditProductCode);
			string modelCode = BranchConfigAdapter.NormalizeProductCode(_model.ProductCode);
			if (!editCode.Equals(modelCode, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (EditEnabled != _model.Enabled
				|| EditHeadlessMode != _model.HeadlessMode
				|| EditTerminateOnPause != _model.TerminateOnPause)
			{
				return true;
			}

			string editArgs = (EditExtraArgs ?? string.Empty).Trim();
			string modelArgs = (_model.ExtraArgs ?? string.Empty).Trim();
			if (!editArgs.Equals(modelArgs, StringComparison.Ordinal))
			{
				return true;
			}

			string editExe = (EditExecutablePath ?? string.Empty).Trim();
			string modelExe = (_model.ExecutablePath ?? string.Empty).Trim();
			if (!editExe.Equals(modelExe, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			string? editAudioId = EditAudioDevice?.DeviceId;
			string? modelAudioId = _model.AudioDeviceId;
			return !string.Equals(editAudioId, modelAudioId, StringComparison.OrdinalIgnoreCase);
		}
	}
	public bool IsRuntimeConfigDirty
	{
		get
		{
			string editCode = BranchConfigAdapter.NormalizeProductCode(EditProductCode);
			string modelCode = BranchConfigAdapter.NormalizeProductCode(_model.ProductCode);
			if (!editCode.Equals(modelCode, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (!int.TryParse(EditPort, out int editPortValue))
			{
				editPortValue = _model.Port;
			}

			if (editPortValue != _model.Port)
			{
				return true;
			}

			string editExe = (EditExecutablePath ?? string.Empty).Trim();
			string modelExe = (_model.ExecutablePath ?? string.Empty).Trim();
			return !editExe.Equals(modelExe, StringComparison.OrdinalIgnoreCase);
		}
	}

	public ObservableCollection<AudioDeviceViewModel> AudioDevices { get; } = new();

	public InstanceViewModel(InstanceConfig model, Func<string, string> resolveExecutablePath)
	{
		_model = model;
		_resolveExecutablePath = resolveExecutablePath;
		_lastResolvedProductCode = BranchConfigAdapter.NormalizeProductCode(model.ProductCode);
		LoadFromModel();
	}

	public void LoadFromModel()
	{
		EditName = _model.Name;
		EditPort = _model.Port.ToString();
		EditProductCode = BranchConfigAdapter.NormalizeProductCode(_model.ProductCode);
		EditEnabled = _model.Enabled;
		EditHeadlessMode = _model.HeadlessMode;
		EditTerminateOnPause = _model.TerminateOnPause;
		EditExtraArgs = _model.ExtraArgs;
		EditExecutablePath = string.IsNullOrWhiteSpace(_model.ExecutablePath)
			? _resolveExecutablePath(EditProductCode)
			: _model.ExecutablePath;
		_lastResolvedProductCode = EditProductCode;
		EditAudioDevice = AudioDevices.FirstOrDefault(x => x.DeviceId == _model.AudioDeviceId)
			?? AudioDevices.FirstOrDefault();
		ValidateName();
		ValidatePort();
		OnPropertyChanged(nameof(IsDirty));
		OnPropertyChanged(nameof(IsRuntimeConfigDirty));
	}

	public void SaveToModel()
	{
		ValidateName();
		ValidatePort();

		if (string.IsNullOrWhiteSpace(NameValidationError))
		{
			_model.Name = EditName.Trim();
		}

		if (string.IsNullOrWhiteSpace(PortValidationError) && int.TryParse(EditPort, out var port))
		{
			_model.Port = port;
		}

		_model.ProductCode = EditProductCode;

		_model.Enabled = EditEnabled;
		_model.HeadlessMode = EditHeadlessMode;
		_model.TerminateOnPause = EditTerminateOnPause;
		_model.ExtraArgs = EditExtraArgs;
		_model.ExecutablePath = EditExecutablePath;
		_model.AudioDeviceId = EditAudioDevice?.DeviceId;
		OnPropertyChanged(nameof(IsDirty));
		OnPropertyChanged(nameof(IsRuntimeConfigDirty));
	}

	public void ResolveAudioDevice(IEnumerable<AudioDeviceViewModel> devices)
	{
		AudioDevices.Clear();
		foreach (var device in devices)
		{
			AudioDevices.Add(device);
		}

		EditAudioDevice = AudioDevices.FirstOrDefault(x => x.DeviceId == _model.AudioDeviceId)
			?? AudioDevices.FirstOrDefault();
		OnPropertyChanged(nameof(IsDirty));
	}

	public void NotifyLocalizationChanged()
	{
		OnPropertyChanged(nameof(RunStatusText));
	}

	partial void OnIsRunningChanged(bool value)
	{
		OnPropertyChanged(nameof(RunStatusText));
		OnPropertyChanged(nameof(RunStatusColor));
	}

	partial void OnEditNameChanged(string value)
	{
		ValidateName();
		OnPropertyChanged(nameof(Name));
		OnPropertyChanged(nameof(IsDirty));
	}

	partial void OnEditPortChanged(string value)
	{
		ValidatePort();
		OnPropertyChanged(nameof(Port));
		OnPropertyChanged(nameof(IsDirty));
		OnPropertyChanged(nameof(IsRuntimeConfigDirty));
	}

	partial void OnEditProductCodeChanged(string value)
	{
		string normalized = BranchConfigAdapter.NormalizeProductCode(value);
		string previousDefaultPath = _resolveExecutablePath(_lastResolvedProductCode);
		string nextDefaultPath = _resolveExecutablePath(normalized);

		if (string.IsNullOrWhiteSpace(EditExecutablePath)
			|| EditExecutablePath.Equals(previousDefaultPath, StringComparison.OrdinalIgnoreCase))
		{
			EditExecutablePath = nextDefaultPath;
		}

		_lastResolvedProductCode = normalized;
		OnPropertyChanged(nameof(IsDirty));
		OnPropertyChanged(nameof(IsRuntimeConfigDirty));
	}

	partial void OnEditExecutablePathChanged(string value)
	{
		OnPropertyChanged(nameof(IsDirty));
		OnPropertyChanged(nameof(IsRuntimeConfigDirty));
	}

	partial void OnEditEnabledChanged(bool value)
	{
		OnPropertyChanged(nameof(IsDirty));
	}

	partial void OnEditHeadlessModeChanged(bool value)
	{
		OnPropertyChanged(nameof(IsDirty));
	}

	partial void OnEditTerminateOnPauseChanged(bool value)
	{
		OnPropertyChanged(nameof(IsDirty));
	}

	partial void OnEditExtraArgsChanged(string value)
	{
		OnPropertyChanged(nameof(IsDirty));
	}

	partial void OnEditAudioDeviceChanged(AudioDeviceViewModel? value)
	{
		OnPropertyChanged(nameof(IsDirty));
	}

	partial void OnPortValidationErrorChanged(string? value)
	{
		OnPropertyChanged(nameof(HasValidationErrors));
	}

	partial void OnNameValidationErrorChanged(string? value)
	{
		OnPropertyChanged(nameof(HasValidationErrors));
	}

	private void ValidateName()
	{
		NameValidationError = string.IsNullOrWhiteSpace(EditName)
			? "Instance name is required."
			: null;
	}

	private void ValidatePort()
	{
		if (string.IsNullOrWhiteSpace(EditPort))
		{
			PortValidationError = "Port is required.";
			return;
		}

		if (!int.TryParse(EditPort, out var port))
		{
			PortValidationError = "Port must be a number.";
			return;
		}

		PortValidationError = (port < 1 || port > 65534)
			? "Port must be between 1 and 65534."
			: null;
	}
}
