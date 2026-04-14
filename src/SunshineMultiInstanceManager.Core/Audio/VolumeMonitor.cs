using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Helios.Core.Process;
using Helios.Core.Storage;
using Helios.Core.Storage.Models;

namespace Helios.Core.Audio;

public sealed class VolumeMonitor : IDisposable, IMMNotificationClient
{
	private readonly AudioDeviceService _deviceService;

	private readonly SettingsStore _store;

	private readonly ILogger _logger;

	private MMDevice? _watchedDevice;

	private AudioEndpointVolume? _watchedVolume;

	private float _lastSyncedVolume = -1f;

	private readonly object _syncLock = new object();

	private bool _disposed;

	private bool _enabled;

	private static readonly TaskFactory s_staFactory = CreateStaTaskFactory();

	public VolumeMonitor(AudioDeviceService deviceService, SettingsStore store, ILogger logger)
	{
		_deviceService = deviceService;
		_store = store;
		_logger = logger;
	}

	public void Start()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (!_enabled)
		{
			_enabled = true;
			try
			{
				_deviceService.Enumerator.RegisterEndpointNotificationCallback(this);
			}
			catch (Exception ex)
			{
				_logger.LogWarning("RegisterEndpointNotificationCallback failed: {Msg}", ex.Message);
			}
			AttachToDefaultDevice();
			_logger.LogInformation("VolumeMonitor started.");
		}
	}

	public void Stop()
	{
		if (_enabled)
		{
			_enabled = false;
			try
			{
				_deviceService.Enumerator.UnregisterEndpointNotificationCallback(this);
			}
			catch
			{
			}
			DetachFromCurrentDevice();
			_logger.LogInformation("VolumeMonitor stopped.");
		}
	}

	private void OnVolumeNotification(AudioVolumeNotificationData data)
	{
		if (!_enabled || _disposed || !_store.Settings.SyncVolume)
		{
			return;
		}
		float newVolume = data.MasterVolume;
		lock (_syncLock)
		{
			if (Math.Abs(newVolume - _lastSyncedVolume) < 0.001f)
			{
				return;
			}
			_lastSyncedVolume = newVolume;
		}
		_logger.LogDebug("System volume changed to {Vol:P0}; syncing to configured instances.", newVolume);
		s_staFactory.StartNew(delegate
		{
			SyncVolumeToInstances(newVolume);
		});
	}

	private void SyncVolumeToInstances(float volume)
	{
		string text = _watchedDevice?.ID ?? string.Empty;
		foreach (InstanceConfig instance in _store.Settings.Instances)
		{
			if (instance.Enabled && !string.IsNullOrEmpty(instance.AudioDeviceId) && !(instance.AudioDeviceId == text))
			{
				SetDeviceVolume(instance.AudioDeviceId, volume);
			}
		}
	}

	private void SetDeviceVolume(string deviceId, float volume)
	{
		try
		{
			using MMDevice mMDevice = _deviceService.GetDeviceById(deviceId);
			if (mMDevice != null)
			{
				float num = Math.Clamp(volume, 0f, 1f);
				mMDevice.AudioEndpointVolume.MasterVolumeLevelScalar = num;
				_logger.LogDebug("Set device [{Name}] volume to {Vol:P0}", mMDevice.FriendlyName, num);
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning("SetDeviceVolume failed for deviceId={Id}: {Msg}", deviceId, ex.Message);
		}
	}

	void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
	{
		if (flow == DataFlow.Render && role == Role.Multimedia)
		{
			_logger.LogInformation("Default output device changed; reattaching volume monitor.");
			s_staFactory.StartNew(delegate
			{
				DetachFromCurrentDevice();
				AttachToDefaultDevice();
			});
		}
	}

	void IMMNotificationClient.OnDeviceAdded(string deviceId)
	{
	}

	void IMMNotificationClient.OnDeviceRemoved(string deviceId)
	{
	}

	void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState)
	{
	}

	void IMMNotificationClient.OnPropertyValueChanged(string deviceId, PropertyKey key)
	{
	}

	private void AttachToDefaultDevice()
	{
		MMDevice defaultOutputMMDevice = _deviceService.GetDefaultOutputMMDevice();
		if (defaultOutputMMDevice == null)
		{
			_logger.LogWarning("No default output device available; VolumeMonitor is paused.");
			return;
		}
		_watchedDevice = defaultOutputMMDevice;
		_watchedVolume = defaultOutputMMDevice.AudioEndpointVolume;
		_watchedVolume.OnVolumeNotification += OnVolumeNotification;
		float masterVolumeLevelScalar = _watchedVolume.MasterVolumeLevelScalar;
		_logger.LogInformation("VolumeMonitor attached to [{Name}], current volume {Vol:P0}.", defaultOutputMMDevice.FriendlyName, masterVolumeLevelScalar);
		if (_store.Settings.SyncVolume)
		{
			SyncVolumeToInstances(masterVolumeLevelScalar);
		}
	}

	private void DetachFromCurrentDevice()
	{
		if (_watchedVolume != null)
		{
			try
			{
				_watchedVolume.OnVolumeNotification -= OnVolumeNotification;
			}
			catch
			{
			}
			_watchedVolume = null;
		}
		if (_watchedDevice != null)
		{
			try
			{
				_watchedDevice.Dispose();
			}
			catch
			{
			}
			_watchedDevice = null;
		}
		_lastSyncedVolume = -1f;
	}

	private static TaskFactory CreateStaTaskFactory()
	{
		return new TaskFactory(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, new _003CVolumeMonitor_003EFC01EE3D4726DE05F1053215F16C5C1BC81BBF06C9CB41CF41951C1A108752340__StaTaskScheduler());
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			Stop();
		}
	}
}

