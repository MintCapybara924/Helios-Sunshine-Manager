using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SunshineMultiInstanceManager.Core.Process;
using SunshineMultiInstanceManager.Core.Profiles;

namespace SunshineMultiInstanceManager.Core.Update;

public sealed class InstallerService
{
	private const string UserAgent = "SunshineMultiInstanceManager/1.0";

	private static readonly HttpClient s_http = CreateHttpClient();

	private readonly ILogger _logger;

	public InstallerService(ILogger logger)
	{
		_logger = logger;
	}

	public string? GetInstalledVersion(string productCode, string? executablePathOverride = null)
	{
		ManagedProductDefinition product = ManagedProductCatalog.GetByCode(productCode);
		string defaultExecutablePath = string.IsNullOrWhiteSpace(executablePathOverride)
			? product.DefaultExecutablePath
			: executablePathOverride;
		if (!File.Exists(defaultExecutablePath))
		{
			return null;
		}
		try
		{
			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(defaultExecutablePath);
			return string.IsNullOrWhiteSpace(versionInfo.ProductVersion) ? versionInfo.FileVersion : versionInfo.ProductVersion;
		}
		catch (Exception ex)
		{
			_logger.LogWarning("Read executable version failed for {Product}: {Msg}", product.DisplayName, ex.Message);
			return null;
		}
	}

	public async Task<ReleaseInfo?> GetLatestStableReleaseAsync(string productCode, CancellationToken ct = default)
	{
		ManagedProductDefinition product = ManagedProductCatalog.GetByCode(productCode);
		try
		{
			ReleaseInfo? release = ToReleaseInfo(
				JsonSerializer.Deserialize(await s_http.GetStringAsync(product.ApiLatestUrl, ct), GitHubJsonContext.Default.GitHubReleaseDto),
				product);
			return release ?? await GetLatestViaRedirectFallbackAsync(product, ct);
		}
		catch (HttpRequestException ex)
		{
			_logger.LogWarning("Fetch latest stable release failed for {Product} (HTTP): {Msg}", product.DisplayName, ex.Message);
			return await GetLatestViaRedirectFallbackAsync(product, ct);
		}
		catch (Exception ex)
		{
			_logger.LogWarning("Fetch latest stable release failed for {Product}: {Msg}", product.DisplayName, ex.Message);
			return await GetLatestViaRedirectFallbackAsync(product, ct);
		}
	}

	public async Task<ReleaseInfo?> GetLatestAnyReleaseAsync(string productCode, CancellationToken ct = default)
	{
		ManagedProductDefinition product = ManagedProductCatalog.GetByCode(productCode);
		try
		{
			GitHubReleaseDto[] array = JsonSerializer.Deserialize(await s_http.GetStringAsync(product.ApiReleasesUrl, ct), GitHubJsonContext.Default.GitHubReleaseDtoArray);
			if (array != null && array.Length > 0)
			{
				ReleaseInfo? release = ToReleaseInfo(array[0], product);
				if (release != null)
				{
					return release;
				}
			}

			return await GetLatestViaRedirectFallbackAsync(product, ct);
		}
		catch (HttpRequestException ex)
		{
			_logger.LogWarning("Fetch latest release failed for {Product} (HTTP): {Msg}", product.DisplayName, ex.Message);
			return await GetLatestViaRedirectFallbackAsync(product, ct);
		}
		catch (Exception ex)
		{
			_logger.LogWarning("Fetch latest release failed for {Product}: {Msg}", product.DisplayName, ex.Message);
			return await GetLatestViaRedirectFallbackAsync(product, ct);
		}
	}

	private async Task<ReleaseInfo?> GetLatestViaRedirectFallbackAsync(ManagedProductDefinition product, CancellationToken ct)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(product.InstallerAssetName))
			{
				return null;
			}

			string? tag = await TryResolveLatestTagFromRedirectAsync(product, ct);
			string downloadUrl = $"https://github.com/{product.RepoOwner}/{product.RepoName}/releases/latest/download/{product.InstallerAssetName}";

			long sizeBytes = 0;
			using (HttpResponseMessage response = await s_http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct))
			{
				if (!response.IsSuccessStatusCode)
				{
					return null;
				}

				sizeBytes = response.Content.Headers.ContentLength ?? 0;
			}

			string tagName = string.IsNullOrWhiteSpace(tag) ? "latest" : tag;
			bool isPreRelease = tagName.Contains("beta", StringComparison.OrdinalIgnoreCase)
				|| tagName.Contains("pre", StringComparison.OrdinalIgnoreCase)
				|| tagName.Contains("rc", StringComparison.OrdinalIgnoreCase);

			_logger.LogInformation("Using non-API fallback for {Product}: {Tag}", product.DisplayName, tagName);
			return new ReleaseInfo(tagName, tagName, isPreRelease, downloadUrl, sizeBytes, DateTimeOffset.UtcNow, string.Empty);
		}
		catch (Exception ex)
		{
			_logger.LogWarning("Fallback release discovery failed for {Product}: {Msg}", product.DisplayName, ex.Message);
			return null;
		}
	}

	private static async Task<string?> TryResolveLatestTagFromRedirectAsync(ManagedProductDefinition product, CancellationToken ct)
	{
		using var handler = new HttpClientHandler
		{
			AllowAutoRedirect = false
		};

		using var client = new HttpClient(handler)
		{
			Timeout = TimeSpan.FromSeconds(15)
		};

		client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
		client.DefaultRequestHeaders.Accept.ParseAdd("text/html");

		using var request = new HttpRequestMessage(HttpMethod.Get, product.ReleasesLatestUrl);
		using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
		if ((int)response.StatusCode is < 300 or >= 400)
		{
			return null;
		}

		Uri? location = response.Headers.Location;
		if (location == null)
		{
			return null;
		}

		string path = location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString;
		int idx = path.IndexOf("/releases/tag/", StringComparison.OrdinalIgnoreCase);
		if (idx < 0)
		{
			return null;
		}

		string tag = path[(idx + "/releases/tag/".Length)..].Trim('/');
		return string.IsNullOrWhiteSpace(tag) ? null : tag;
	}

	public async Task DownloadAsync(string downloadUrl, string destPath, IProgress<int>? progress, CancellationToken ct = default)
	{
		using HttpResponseMessage response = await s_http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
		response.EnsureSuccessStatusCode();
		long? total = response.Content.Headers.ContentLength;

		await using Stream srcStream = await response.Content.ReadAsStreamAsync(ct);
		await using FileStream destStream = File.Create(destPath);

		byte[] buffer = new byte[81920];
		long downloaded = 0;
		int lastReported = -1;

		while (true)
		{
			int bytesRead = await srcStream.ReadAsync(buffer, ct);
			if (bytesRead <= 0)
			{
				break;
			}

			await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
			downloaded += bytesRead;

			if (progress != null)
			{
				int percentage = total.HasValue ? (int)(downloaded * 100 / total.Value) : -1;
				if (percentage != lastReported)
				{
					progress.Report(percentage);
					lastReported = percentage;
				}
			}
		}

		_logger.LogInformation("Download completed: {Path} ({Size} bytes)", destPath, downloaded);
	}

	public bool SupportsInstallDirectoryOverride(string productCode, string installerPath)
	{
		string extension = Path.GetExtension(installerPath);
		if (extension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// Vibeshine/Vibepollo setup currently rejects /DIR and may pop msiexec help; keep interactive install clean.
		if (productCode.Equals("vibeshine", StringComparison.OrdinalIgnoreCase)
			|| productCode.Equals("vibepollo", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return true;
	}

	public async Task<int> RunInstallerAsync(string installerPath, string? preferredInstallDirectory = null, string? productCode = null, CancellationToken ct = default)
	{
		if (!File.Exists(installerPath))
		{
			throw new FileNotFoundException("Installer file not found.", installerPath);
		}

		string extension = Path.GetExtension(installerPath);
		if (extension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
		{
			string msiArgs = BuildMsiArguments(installerPath, preferredInstallDirectory);
			_logger.LogInformation("Launching MSI installer via msiexec: {Path} Args={Args}", installerPath, msiArgs);
			return await RunProcessAndWaitAsync("msiexec.exe", msiArgs, useShellExecute: false, ct);
		}

		bool allowDirOverride = !string.IsNullOrWhiteSpace(productCode)
			&& SupportsInstallDirectoryOverride(productCode, installerPath);
		string exeArgs = allowDirOverride ? BuildExeInstallerArguments(preferredInstallDirectory) : string.Empty;
		_logger.LogInformation("Launching EXE installer: {Path} Args={Args}", installerPath, exeArgs);
		int exitCode = await RunProcessAndWaitAsync(installerPath, exeArgs, useShellExecute: true, ct);

		if (!string.IsNullOrWhiteSpace(exeArgs) && exitCode != 0)
		{
			_logger.LogWarning("Installer exited with code {Code} using custom args. Retrying without args.", exitCode);
			exitCode = await RunProcessAndWaitAsync(installerPath, string.Empty, useShellExecute: true, ct);
		}

		return exitCode;
	}

	private static string BuildExeInstallerArguments(string? preferredInstallDirectory)
	{
		if (string.IsNullOrWhiteSpace(preferredInstallDirectory))
		{
			return string.Empty;
		}

		return $"/DIR=\"{preferredInstallDirectory.Trim()}\"";
	}

	private static string BuildMsiArguments(string installerPath, string? preferredInstallDirectory)
	{
		string quotedPath = $"\"{installerPath}\"";
		if (string.IsNullOrWhiteSpace(preferredInstallDirectory))
		{
			return $"/i {quotedPath}";
		}

		string dir = preferredInstallDirectory.Trim();
		return $"/i {quotedPath} INSTALLDIR=\"{dir}\" TARGETDIR=\"{dir}\"";
	}

	private static async Task<int> RunProcessAndWaitAsync(string fileName, string arguments, bool useShellExecute, CancellationToken ct)
	{
		using var proc = new System.Diagnostics.Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				UseShellExecute = useShellExecute
			}
		};

		if (!proc.Start())
		{
			return -1;
		}

		try
		{
			await proc.WaitForExitAsync(ct);
			return proc.ExitCode;
		}
		catch (OperationCanceledException)
		{
			try
			{
				if (!proc.HasExited)
				{
					proc.Kill();
				}
			}
			catch
			{
			}

			throw;
		}
	}

	private static ReleaseInfo? ToReleaseInfo(GitHubReleaseDto? dto, ManagedProductDefinition product)
	{
		if (dto == null)
		{
			return null;
		}

		GitHubAssetDto asset = ResolveInstallerAsset(dto, product);
		if (asset == null)
		{
			return null;
		}

		return new ReleaseInfo(dto.TagName, dto.ReleaseName, dto.PreRelease, asset.BrowserDownloadUrl, asset.Size, dto.PublishedAt, dto.Body ?? string.Empty);
	}

	private static GitHubAssetDto? ResolveInstallerAsset(GitHubReleaseDto dto, ManagedProductDefinition product)
	{
		if (!string.IsNullOrWhiteSpace(product.InstallerAssetName))
		{
			GitHubAssetDto? exact = dto.Assets.FirstOrDefault(a => a.Name.Equals(product.InstallerAssetName, StringComparison.OrdinalIgnoreCase));
			if (exact != null)
			{
				return exact;
			}
		}

		GitHubAssetDto? hinted = dto.Assets.FirstOrDefault(a =>
			a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
			&& product.AssetNameHints.Any(h => a.Name.Contains(h, StringComparison.OrdinalIgnoreCase)));
		if (hinted != null)
		{
			return hinted;
		}

		return dto.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
	}

	private static HttpClient CreateHttpClient()
	{
		var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
		httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
		httpClient.Timeout = TimeSpan.FromSeconds(30);
		return httpClient;
	}
}
