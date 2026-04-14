using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using SunshineMultiInstanceManager.Core.Profiles;

namespace SunshineMultiInstanceManager.Core.Storage.Models;

public sealed class AppSettings
{
	[JsonPropertyName("autoStart")]
	public bool AutoStart { get; set; } = true;


	[JsonPropertyName("syncVolume")]
	public bool SyncVolume { get; set; } = true;


	[JsonPropertyName("removeDisplayOnDisconnect")]
	public bool RemoveDisplayOnDisconnect { get; set; } = true;


	[JsonPropertyName("syncOnDisplayChange")]
	public bool SyncOnDisplayChange { get; set; } = true;

	[JsonPropertyName("languageCode")]
	public string LanguageCode { get; set; } = "system";

	[JsonPropertyName("currentProduct")]
	public string CurrentProduct { get; set; } = "sunshine";

	[JsonPropertyName("productInstallPaths")]
	public Dictionary<string, string> ProductInstallPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

	[JsonPropertyName("uiTheme")]
	public string UiTheme { get; set; } = "light";


	[JsonPropertyName("vibeshinePath")]
	public string VibeshineExecutablePath { get; set; } = string.Empty;


	[JsonPropertyName("instancesRootPath")]
	public string InstancesRootPath { get; set; } = string.Empty;


	[JsonPropertyName("restoreWindowPosition")]
	public bool RestoreWindowPosition { get; set; } = true;


	[JsonPropertyName("windowX")]
	public double? WindowX { get; set; }

	[JsonPropertyName("windowY")]
	public double? WindowY { get; set; }

	[JsonPropertyName("windowWidth")]
	public double? WindowWidth { get; set; }

	[JsonPropertyName("windowHeight")]
	public double? WindowHeight { get; set; }

	[JsonPropertyName("instances")]
	public List<InstanceConfig> Instances { get; set; } = new List<InstanceConfig>();


	public string ResolvedVibeshineExecutablePath
	{
		get
		{
			if (!string.IsNullOrEmpty(VibeshineExecutablePath))
			{
				return VibeshineExecutablePath;
			}
			return VibeshineProfile.DefaultExecutablePath;
		}
	}

	public string ResolvedInstancesRootPath
	{
		get
		{
			if (!string.IsNullOrEmpty(InstancesRootPath))
			{
				return InstancesRootPath;
			}
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SunshineMultiInstanceManager", "instances");
		}
	}

	public HashSet<int> GetUsedPorts()
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (int item in Instances.Select((InstanceConfig i) => i.Port))
		{
			hashSet.Add(item);
		}
		return hashSet;
	}

	public int GetNextAvailablePort()
	{
		HashSet<int> usedPorts = GetUsedPorts();
		int i;
		for (i = 48100; usedPorts.Contains(i); i += 100)
		{
		}
		return i;
	}

	public string GetProductInstallPath(string? productCode)
	{
		ManagedProductDefinition product = ManagedProductCatalog.GetByCode(productCode);
		if (ProductInstallPaths.TryGetValue(product.Code, out string? configured) && !string.IsNullOrWhiteSpace(configured))
		{
			return configured;
		}

		return product.DefaultInstallPath;
	}

	public string GetProductExecutablePath(string? productCode)
	{
		ManagedProductDefinition product = ManagedProductCatalog.GetByCode(productCode);
		return Path.Combine(GetProductInstallPath(product.Code), product.ExecutableName);
	}

	public void SetProductInstallPath(string? productCode, string? installPath)
	{
		ManagedProductDefinition product = ManagedProductCatalog.GetByCode(productCode);
		if (string.IsNullOrWhiteSpace(installPath))
		{
			ProductInstallPaths.Remove(product.Code);
			return;
		}

		ProductInstallPaths[product.Code] = installPath.Trim();
	}
}
