using System.Text.Json.Serialization;

namespace Helios.Core.Update;

internal sealed class GitHubAssetDto
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;


	[JsonPropertyName("browser_download_url")]
	public string BrowserDownloadUrl { get; set; } = string.Empty;


	[JsonPropertyName("size")]
	public long Size { get; set; }
}

