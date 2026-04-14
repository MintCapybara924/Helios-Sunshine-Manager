using System;

namespace SunshineMultiInstanceManager.Core.Update;

public sealed record ReleaseInfo(string TagName, string Name, bool IsPreRelease, string DownloadUrl, long SizeBytes, DateTimeOffset PublishedAt, string ReleaseNotes)
{
	public string SizeText
	{
		get
		{
			long sizeBytes = SizeBytes;
			if (sizeBytes < 1048576)
			{
				if (sizeBytes >= 1024)
				{
					return $"{(double)SizeBytes / 1024.0:F1} KB";
				}
				return $"{SizeBytes} B";
			}
			return $"{(double)SizeBytes / 1048576.0:F1} MB";
		}
	}
}
