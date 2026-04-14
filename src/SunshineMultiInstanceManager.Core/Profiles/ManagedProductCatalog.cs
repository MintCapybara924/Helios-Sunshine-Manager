using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helios.Core.Profiles;

public sealed class ManagedProductDefinition
{
	public required string Code { get; init; }
	public required string DisplayName { get; init; }
	public required string RepoOwner { get; init; }
	public required string RepoName { get; init; }
	public string? InstallerAssetName { get; init; }
	public required string InstallFolderName { get; init; }
	public required string ExecutableName { get; init; }
	public string? WindowsServiceName { get; init; }
	public string[] AssetNameHints { get; init; } = Array.Empty<string>();

	public string DefaultInstallPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), InstallFolderName);

	public string DefaultExecutablePath => Path.Combine(DefaultInstallPath, ExecutableName);

	public string ApiLatestUrl => $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

	public string ApiReleasesUrl => $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases?per_page=10";

	public string ReleasesLatestUrl => $"https://github.com/{RepoOwner}/{RepoName}/releases/latest";
}

public static class ManagedProductCatalog
{
	public static IReadOnlyList<ManagedProductDefinition> All { get; } =
	[
		new ManagedProductDefinition
		{
			Code = "sunshine",
			DisplayName = "Sunshine",
			RepoOwner = "lizardbyte",
			RepoName = "sunshine",
			InstallerAssetName = null,
			InstallFolderName = "Sunshine",
			ExecutableName = "sunshine.exe",
			WindowsServiceName = "SunshineService",
			AssetNameHints = ["sunshine", "installer", "windows"]
		},
		new ManagedProductDefinition
		{
			Code = "apollo",
			DisplayName = "Apollo",
			RepoOwner = "ClassicOldSong",
			RepoName = "Apollo",
			InstallerAssetName = null,
			InstallFolderName = "Apollo",
			ExecutableName = "sunshine.exe",
			WindowsServiceName = "SunshineService",
			AssetNameHints = ["apollo", "installer", "windows"]
		},
		new ManagedProductDefinition
		{
			Code = "vibeshine",
			DisplayName = "Vibeshine",
			RepoOwner = "Nonary",
			RepoName = "vibeshine",
			InstallerAssetName = "VibeshineSetup.exe",
			InstallFolderName = "Vibeshine",
			ExecutableName = "sunshine.exe",
			WindowsServiceName = "SunshineService",
			AssetNameHints = ["vibeshine", "setup", "sunshine"]
		},
		new ManagedProductDefinition
		{
			Code = "vibepollo",
			DisplayName = "Vibepollo",
			RepoOwner = "Nonary",
			RepoName = "Vibepollo",
			InstallerAssetName = null,
			InstallFolderName = "Vibepollo",
			ExecutableName = "sunshine.exe",
			WindowsServiceName = "SunshineService",
			AssetNameHints = ["vibepollo", "vibepollo", "setup", "installer"]
		}
	];

	public static ManagedProductDefinition GetByCode(string? code)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			return All.First(p => p.Code == "sunshine");
		}

		return All.FirstOrDefault(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
			?? All.First(p => p.Code == "sunshine");
	}
}

