using SunshineMultiInstanceManager.App.Services;

namespace SunshineMultiInstanceManager.App.ViewModels;

public sealed class AudioDeviceViewModel
{
	public string DeviceId { get; init; } = string.Empty;

	public string FriendlyName { get; init; } = string.Empty;

	public override string ToString()
	{
		return FriendlyName;
	}

	public static AudioDeviceViewModel CreateAuto()
	{
		return new AudioDeviceViewModel
		{
			DeviceId = string.Empty,
			FriendlyName = LocalizationService.T("AudioAutoSelect")
		};
	}
}
