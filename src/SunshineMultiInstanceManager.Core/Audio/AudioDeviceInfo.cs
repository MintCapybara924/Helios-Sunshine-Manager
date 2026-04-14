namespace SunshineMultiInstanceManager.Core.Audio;

public sealed class AudioDeviceInfo
{
	public required string DeviceId { get; init; }

	public required string FriendlyName { get; init; }

	public bool IsDefault { get; init; }

	public string DisplayName
	{
		get
		{
			if (!IsDefault)
			{
				return FriendlyName;
			}
			return "鈽?" + FriendlyName;
		}
	}

	public override string ToString()
	{
		return DisplayName;
	}
}
