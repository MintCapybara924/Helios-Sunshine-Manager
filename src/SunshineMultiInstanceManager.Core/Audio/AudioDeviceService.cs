using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;

namespace Helios.Core.Audio;

public sealed class AudioDeviceService : IDisposable
{
	private readonly MMDeviceEnumerator _enumerator;

	private bool _disposed;

	internal MMDeviceEnumerator Enumerator => _enumerator;

	public AudioDeviceService()
	{
		_enumerator = new MMDeviceEnumerator();
	}

	public IReadOnlyList<AudioDeviceInfo> EnumerateOutputDevices()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		string text = string.Empty;
		try
		{
			using MMDevice mMDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
			text = mMDevice.ID;
		}
		catch
		{
		}
		List<AudioDeviceInfo> list = new List<AudioDeviceInfo>();
		MMDeviceCollection mMDeviceCollection = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
		for (int i = 0; i < mMDeviceCollection.Count; i++)
		{
			using MMDevice mMDevice2 = mMDeviceCollection[i];
			list.Add(new AudioDeviceInfo
			{
				DeviceId = mMDevice2.ID,
				FriendlyName = mMDevice2.FriendlyName,
				IsDefault = (mMDevice2.ID == text)
			});
		}
		return (from d in list
			orderby d.IsDefault descending, d.FriendlyName
			select d).ToList().AsReadOnly();
	}

	public AudioDeviceInfo? GetDefaultOutputDevice()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		try
		{
			using MMDevice mMDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
			return new AudioDeviceInfo
			{
				DeviceId = mMDevice.ID,
				FriendlyName = mMDevice.FriendlyName,
				IsDefault = true
			};
		}
		catch
		{
			return null;
		}
	}

	internal MMDevice? GetDeviceById(string deviceId)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (string.IsNullOrEmpty(deviceId))
		{
			return null;
		}
		try
		{
			return _enumerator.GetDevice(deviceId);
		}
		catch
		{
			return null;
		}
	}

	internal MMDevice? GetDefaultOutputMMDevice()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		try
		{
			return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
		}
		catch
		{
			return null;
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_enumerator.Dispose();
			_disposed = true;
		}
	}
}

