using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SunshineMultiInstanceManager.Core.Update;

internal sealed class GitHubReleaseDto
{
	[JsonPropertyName("tag_name")]
	public string TagName { get; set; } = string.Empty;


	[JsonPropertyName("name")]
	public string ReleaseName { get; set; } = string.Empty;


	[JsonPropertyName("prerelease")]
	public bool PreRelease { get; set; }

	[JsonPropertyName("published_at")]
	public DateTimeOffset PublishedAt { get; set; }

	[JsonPropertyName("body")]
	public string? Body { get; set; }

	[JsonPropertyName("assets")]
	public List<GitHubAssetDto> Assets { get; set; } = new List<GitHubAssetDto>();

}
