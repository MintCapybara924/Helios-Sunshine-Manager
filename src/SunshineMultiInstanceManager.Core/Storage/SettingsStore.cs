using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using SunshineMultiInstanceManager.Core.Profiles;
using SunshineMultiInstanceManager.Core.Storage.Models;

namespace SunshineMultiInstanceManager.Core.Storage;

public sealed class SettingsStore
{
	public event Action? SettingsChanged;

	public static readonly string AppDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SunshineMultiInstanceManager");

	private static readonly string LegacyAppDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SunshineMultiInstanceManager");

	public static readonly string SettingsPath = Path.Combine(AppDataRoot, "settings.json");

	public static readonly string TransientPath = Path.Combine(AppDataRoot, "transient.json");

	private AppSettings? _settings;

	private TransientState? _transient;

	public AppSettings Settings => _settings ?? throw new InvalidOperationException("Settings not loaded. Call LoadAsync() first.");

	public TransientState Transient => _transient ?? throw new InvalidOperationException("Transient state not loaded. Call LoadAsync() first.");

	public async Task LoadAsync(CancellationToken ct = default(CancellationToken))
	{
		MigrateLegacyDataIfNeeded();
		Directory.CreateDirectory(AppDataRoot);
		EnsureUsersModifyAccess(AppDataRoot);
		AtomicFile.CleanupTempFiles(AppDataRoot);
		_settings = await AtomicFile.ReadJsonAsync<AppSettings>(SettingsPath, null, ct);
		if (_settings == null)
		{
			_settings = CreateDefaultSettings();
			await SaveSettingsAsync(ct);
		}
		else
		{
			EnsureInstancesValid(_settings);
		}
		ResolveInstancePaths(_settings);
		EnsureUsersModifyAccess(_settings.ResolvedInstancesRootPath);
		foreach (InstanceConfig instance in _settings.Instances)
		{
			Directory.CreateDirectory(instance.InstanceDirectory);
			EnsureUsersModifyAccess(instance.InstanceDirectory);
			Directory.CreateDirectory(instance.LogDirectory);
			AtomicFile.CleanupTempFiles(instance.InstanceDirectory);
		}
		// Only load transient from disk on first load. Subsequent LoadAsync calls (e.g.,
		// triggered by pipe command handlers reloading settings) must preserve the
		// in-memory runtime state — otherwise flags like ManualStopRequested get wiped
		// by any concurrent reload, and the guardian loop will happily re-adopt a
		// residual process the user just asked to stop.
		if (_transient == null)
		{
			_transient = (await AtomicFile.ReadJsonAsync(TransientPath, new TransientState(), ct)) ?? new TransientState();
		}
		ValidateTransientPids(_settings, _transient);
	}

	public async Task SaveSettingsAsync(CancellationToken ct = default(CancellationToken))
	{
		if (_settings != null)
		{
			await AtomicFile.WriteJsonAsync(SettingsPath, _settings, ct);
			SettingsChanged?.Invoke();
		}
	}

	public async Task SaveTransientAsync(CancellationToken ct = default(CancellationToken))
	{
		if (_transient != null)
		{
			await AtomicFile.WriteJsonAsync(TransientPath, _transient, ct);
		}
	}

	public void SaveTransientSync()
	{
		if (_transient != null)
		{
			AtomicFile.WriteJson(TransientPath, _transient);
		}
	}

	public bool SyncInstanceConf(InstanceConfig instance)
	{
		Dictionary<string, string> existing = AtomicFile.ReadConf(instance.SunshineConfPath);
		string targetCode = BranchConfigAdapter.NormalizeProductCode(instance.ProductCode);

		// Keep all existing keys so common settings survive branch switching,
		// then overwrite runtime-critical fields managed by the manager.
		Dictionary<string, string> output = new(existing, StringComparer.OrdinalIgnoreCase);

		foreach ((string key, string value) in BranchConfigAdapter.BuildManagedFields(instance))
		{
			output[key] = value;
		}

		foreach (string key in output.Keys.ToList())
		{
			if (!BranchConfigAdapter.IsKeyAllowedForProduct(targetCode, key))
			{
				output.Remove(key);
			}
		}

		bool changed = !File.Exists(instance.SunshineConfPath)
			|| output.Count != existing.Count
			|| output.Any(kv => !existing.TryGetValue(kv.Key, out string? oldVal) || oldVal != kv.Value);

		if (!changed)
		{
			instance.LastSyncedProductCode = targetCode;
			return false;
		}

		bool written = AtomicFile.WriteConf(instance.SunshineConfPath, output);
		if (written)
		{
			instance.LastSyncedProductCode = targetCode;
		}

		return written;
	}

	public InstanceConfig AddInstance(string name, InstanceConfig? cloneFrom = null)
	{
		InstanceConfig instanceConfig = cloneFrom?.Clone() ?? new InstanceConfig();
		instanceConfig.Id = Guid.NewGuid().ToString("N");
		instanceConfig.Name = name;
		instanceConfig.Port = Settings.GetNextAvailablePort();
		ResolveInstancePath(instanceConfig, Settings.ResolvedInstancesRootPath);
		if (cloneFrom != null)
		{
			CloneInstanceFiles(cloneFrom, instanceConfig);
		}
		if (string.IsNullOrWhiteSpace(instanceConfig.ExecutablePath))
		{
			instanceConfig.ExecutablePath = Settings.GetProductExecutablePath(instanceConfig.ProductCode);
		}
		Settings.Instances.Add(instanceConfig);
		return instanceConfig;
	}

	private static void CloneInstanceFiles(InstanceConfig source, InstanceConfig target)
	{
		if (string.IsNullOrWhiteSpace(source.InstanceDirectory) || !Directory.Exists(source.InstanceDirectory))
		{
			return;
		}

		Directory.CreateDirectory(target.InstanceDirectory);

		foreach (string sourcePath in Directory.EnumerateFiles(source.InstanceDirectory, "*", SearchOption.AllDirectories))
		{
			string relativePath = Path.GetRelativePath(source.InstanceDirectory, sourcePath);
			if (relativePath.StartsWith("logs" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
				|| relativePath.Equals("logs", StringComparison.OrdinalIgnoreCase)
				|| sourcePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			string destinationPath = Path.Combine(target.InstanceDirectory, relativePath);
			string? destinationDir = Path.GetDirectoryName(destinationPath);
			if (!string.IsNullOrWhiteSpace(destinationDir))
			{
				Directory.CreateDirectory(destinationDir);
			}

			File.Copy(sourcePath, destinationPath, overwrite: true);
		}
	}

	public void RemoveInstance(string instanceId)
	{
		string instanceId2 = instanceId;
		InstanceConfig instanceConfig = Settings.Instances.FirstOrDefault((InstanceConfig i) => i.Id == instanceId2);
		if (instanceConfig != null)
		{
			Settings.Instances.Remove(instanceConfig);
			Transient.Instances.Remove(instanceId2);
		}
	}

	private static AppSettings CreateDefaultSettings()
	{
		// Fresh install starts with an empty list; user explicitly creates instances.
		return new AppSettings();
	}

	private static void ResolveInstancePaths(AppSettings settings)
	{
		string resolvedInstancesRootPath = settings.ResolvedInstancesRootPath;
		foreach (InstanceConfig instance in settings.Instances)
		{
			ResolveInstancePath(instance, resolvedInstancesRootPath);
			if (string.IsNullOrWhiteSpace(instance.ExecutablePath))
			{
				instance.ExecutablePath = settings.GetProductExecutablePath(instance.ProductCode);
			}
		}
	}

	private static void ResolveInstancePath(InstanceConfig inst, string rootPath)
	{
		inst.InstanceDirectory = Path.Combine(rootPath, inst.Id);
	}

	private static void EnsureInstancesValid(AppSettings settings)
	{
		settings.ProductInstallPaths ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		HashSet<string> hashSet = new HashSet<string>();
		HashSet<int> hashSet2 = new HashSet<int>();
		for (int i = 0; i < settings.Instances.Count; i++)
		{
			InstanceConfig instanceConfig = settings.Instances[i];
			if (string.IsNullOrWhiteSpace(instanceConfig.Name))
			{
				instanceConfig.Name = $"Instance {i + 1}";
			}
			if (!hashSet.Add(instanceConfig.Id))
			{
				instanceConfig.Id = Guid.NewGuid().ToString("N");
			}
			if (!hashSet2.Add(instanceConfig.Port))
			{
				int j;
				for (j = 48100; hashSet2.Contains(j); j += 100)
				{
				}
				instanceConfig.Port = j;
				hashSet2.Add(j);
			}

			instanceConfig.ProductCode = BranchConfigAdapter.NormalizeProductCode(instanceConfig.ProductCode);
			instanceConfig.LastSyncedProductCode = BranchConfigAdapter.NormalizeProductCode(
				string.IsNullOrWhiteSpace(instanceConfig.LastSyncedProductCode)
					? instanceConfig.ProductCode
					: instanceConfig.LastSyncedProductCode);
			instanceConfig.BranchConfOverrides ??= new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private static void ValidateTransientPids(AppSettings settings, TransientState transient)
	{
		HashSet<string> validIds = settings.Instances.Select((InstanceConfig i) => i.Id).ToHashSet();
		foreach (string item in transient.Instances.Keys.Where((string k) => !validIds.Contains(k)).ToList())
		{
			transient.Instances.Remove(item);
		}
		foreach (InstanceConfig instance in settings.Instances)
		{
			transient.Instances.TryAdd(instance.Id, new InstanceRuntimeState());
		}
	}

	private static void MigrateLegacyDataIfNeeded()
	{
		try
		{
			if (string.Equals(AppDataRoot, LegacyAppDataRoot, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			if (!Directory.Exists(LegacyAppDataRoot))
			{
				return;
			}

			Directory.CreateDirectory(AppDataRoot);

			string legacySettings = Path.Combine(LegacyAppDataRoot, "settings.json");
			string currentSettings = Path.Combine(AppDataRoot, "settings.json");
			if (!File.Exists(currentSettings) && File.Exists(legacySettings))
			{
				File.Copy(legacySettings, currentSettings, overwrite: false);
			}

			string legacyTransient = Path.Combine(LegacyAppDataRoot, "transient.json");
			string currentTransient = Path.Combine(AppDataRoot, "transient.json");
			if (!File.Exists(currentTransient) && File.Exists(legacyTransient))
			{
				File.Copy(legacyTransient, currentTransient, overwrite: false);
			}
		}
		catch
		{
		}
	}

	private static void EnsureUsersModifyAccess(string directoryPath)
	{
		if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(directoryPath))
		{
			return;
		}

		try
		{
			Directory.CreateDirectory(directoryPath);
			DirectoryInfo directory = new(directoryPath);
			DirectorySecurity security = directory.GetAccessControl();

			FileSystemAccessRule rule = new(
				new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
				FileSystemRights.Modify | FileSystemRights.Synchronize,
				InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
				PropagationFlags.None,
				AccessControlType.Allow);

			bool modified;
			security.ModifyAccessRule(AccessControlModification.Add, rule, out modified);
			if (modified)
			{
				directory.SetAccessControl(security);
			}
		}
		catch
		{
		}
	}
}
