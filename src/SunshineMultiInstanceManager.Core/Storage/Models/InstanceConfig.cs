using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using SunshineMultiInstanceManager.Core.Profiles;

namespace SunshineMultiInstanceManager.Core.Storage.Models;

public sealed class InstanceConfig
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString("N");


	[JsonPropertyName("name")]
	public string Name { get; set; } = "Instance 1";


	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;


	[JsonPropertyName("port")]
	public int Port { get; set; } = 48100;

	[JsonPropertyName("productCode")]
	public string ProductCode { get; set; } = "sunshine";

	[JsonPropertyName("lastSyncedProductCode")]
	public string LastSyncedProductCode { get; set; } = "sunshine";


	[JsonPropertyName("audioDeviceId")]
	public string? AudioDeviceId { get; set; }

	[JsonPropertyName("audioDeviceFriendlyName")]
	public string? AudioDeviceFriendlyName { get; set; }

	[JsonIgnore]
	public bool AutoCaptureSink => string.IsNullOrEmpty(AudioDeviceId);

	[JsonPropertyName("headlessMode")]
	public bool HeadlessMode { get; set; } = true;


	[JsonPropertyName("terminateOnPause")]
	public bool TerminateOnPause { get; set; }

	[JsonPropertyName("extraArgs")]
	public string ExtraArgs { get; set; } = string.Empty;


	[JsonPropertyName("executablePath")]
	public string? ExecutablePath { get; set; }

	[JsonPropertyName("branchConfOverrides")]
	public Dictionary<string, Dictionary<string, string>> BranchConfOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

	[JsonIgnore]
	public string InstanceDirectory { get; set; } = string.Empty;


	[JsonIgnore]
	public string SunshineConfPath => Path.Combine(InstanceDirectory, "sunshine.conf");

	[JsonIgnore]
	public string AppsJsonPath => Path.Combine(InstanceDirectory, "apps.json");

	[JsonIgnore]
	public string StateJsonPath => Path.Combine(InstanceDirectory, "sunshine_state.json");

	[JsonIgnore]
	public string LogDirectory => Path.Combine(InstanceDirectory, "logs");

	public string ResolvedExecutablePath
	{
		get
		{
			if (!string.IsNullOrEmpty(ExecutablePath))
			{
				return ExecutablePath;
			}
			return ManagedProductCatalog.GetByCode(ProductCode).DefaultExecutablePath;
		}
	}

	public InstanceConfig Clone()
	{
		Dictionary<string, Dictionary<string, string>> overridesCopy = new(StringComparer.OrdinalIgnoreCase);
		foreach ((string key, Dictionary<string, string> value) in BranchConfOverrides)
		{
			overridesCopy[key] = new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase);
		}

		return new InstanceConfig
		{
			Id = Id,
			Name = Name,
			Enabled = Enabled,
			Port = Port,
			ProductCode = ProductCode,
			LastSyncedProductCode = LastSyncedProductCode,
			AudioDeviceId = AudioDeviceId,
			AudioDeviceFriendlyName = AudioDeviceFriendlyName,
			HeadlessMode = HeadlessMode,
			TerminateOnPause = TerminateOnPause,
			ExtraArgs = ExtraArgs,
			ExecutablePath = ExecutablePath,
			BranchConfOverrides = overridesCopy,
			InstanceDirectory = InstanceDirectory
		};
	}
}
