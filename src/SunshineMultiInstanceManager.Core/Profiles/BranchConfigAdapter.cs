using System;
using System.Collections.Generic;
using System.IO;
using SunshineMultiInstanceManager.Core.Storage.Models;

namespace SunshineMultiInstanceManager.Core.Profiles;

public static class BranchConfigAdapter
{
	private const string ConfKeyAppsFileAlias = "apps_file";

	private static readonly HashSet<string> ManagedKeys = new(StringComparer.OrdinalIgnoreCase)
	{
		VibeshineProfile.ConfKeySunshineName,
		VibeshineProfile.ConfKeyPort,
		VibeshineProfile.ConfKeyFileState,
		VibeshineProfile.ConfKeyFileApps,
		ConfKeyAppsFileAlias,
		VibeshineProfile.ConfKeyVibeshineFileState,
		VibeshineProfile.ConfKeyCredentialsFile,
		VibeshineProfile.ConfKeyPkey,
		VibeshineProfile.ConfKeyCert,
		VibeshineProfile.ConfKeyLogPath,
		VibeshineProfile.ConfKeyAutoCaptureSink,
		VibeshineProfile.ConfKeyHeadlessMode,
		VibeshineProfile.ConfKeyTerminateOnPause,
		VibeshineProfile.ConfKeyAudioSink,
		VibeshineProfile.ConfKeyVirtualSink
	};

	public static IReadOnlyCollection<string> GetManagedKeys() => ManagedKeys;

	public static Dictionary<string, string> BuildManagedFields(InstanceConfig instance)
	{
		string productCode = NormalizeProductCode(instance.ProductCode);
		string statePath = instance.StateJsonPath.Replace('\\', '/');
		string appsPath = instance.AppsJsonPath.Replace('\\', '/');
		string vibeshineStatePath = Path.Combine(instance.InstanceDirectory, "vibeshine_state.json").Replace('\\', '/');
		string credentialsPath = Path.Combine(instance.InstanceDirectory, "credentials.json").Replace('\\', '/');
		string pkeyPath = Path.Combine(instance.InstanceDirectory, "cakey.pem").Replace('\\', '/');
		string certPath = Path.Combine(instance.InstanceDirectory, "cacert.pem").Replace('\\', '/');
		string logPath = Path.Combine(instance.LogDirectory, "sunshine.log").Replace('\\', '/');

		Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
		{
			[VibeshineProfile.ConfKeySunshineName] = instance.Name,
			[VibeshineProfile.ConfKeyPort] = instance.Port.ToString(),
			[VibeshineProfile.ConfKeyFileState] = statePath,
			[VibeshineProfile.ConfKeyFileApps] = appsPath,
			[ConfKeyAppsFileAlias] = appsPath,
			[VibeshineProfile.ConfKeyCredentialsFile] = credentialsPath,
			[VibeshineProfile.ConfKeyPkey] = pkeyPath,
			[VibeshineProfile.ConfKeyCert] = certPath,
			[VibeshineProfile.ConfKeyLogPath] = logPath,
			[VibeshineProfile.ConfKeyAutoCaptureSink] = instance.AutoCaptureSink ? "enabled" : "disabled",
			[VibeshineProfile.ConfKeyHeadlessMode] = instance.HeadlessMode ? "enabled" : "disabled",
			[VibeshineProfile.ConfKeyTerminateOnPause] = instance.TerminateOnPause ? "enabled" : "disabled"
		};

		if (productCode is "vibeshine" or "vibepollo")
		{
			fields[VibeshineProfile.ConfKeyVibeshineFileState] = vibeshineStatePath;
		}

		if (!string.IsNullOrEmpty(instance.AudioDeviceId))
		{
			fields[VibeshineProfile.ConfKeyAudioSink] = instance.AudioDeviceId;
			fields[VibeshineProfile.ConfKeyVirtualSink] = instance.AudioDeviceId;
		}

		return fields;
	}

	public static string NormalizeProductCode(string? code)
	{
		return ManagedProductCatalog.GetByCode(code).Code;
	}

	public static bool IsKeyAllowedForProduct(string productCode, string key)
	{
		if (key.Equals(VibeshineProfile.ConfKeyVibeshineFileState, StringComparison.OrdinalIgnoreCase))
		{
			return productCode is "vibeshine" or "vibepollo";
		}

		return true;
	}
}
