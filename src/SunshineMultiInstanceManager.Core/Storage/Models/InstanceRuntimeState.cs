using System;
using System.Text.Json.Serialization;

namespace SunshineMultiInstanceManager.Core.Storage.Models;

public sealed class InstanceRuntimeState
{
	public const int StableRunThresholdSeconds = 30;

	[JsonPropertyName("pid")]
	public int Pid { get; set; }

	[JsonPropertyName("lastStartedUtc")]
	public DateTime? LastStartedUtc { get; set; }

	[JsonPropertyName("crashRestartCount")]
	public int CrashRestartCount { get; set; }

	[JsonIgnore]
	public bool IsShuttingDown { get; set; }

	[JsonIgnore]
	public bool IsAlive { get; set; }

	[JsonIgnore]
	public int ConsecutiveCrashCount { get; set; }

	[JsonIgnore]
	public DateTime NextRestartAllowedUtc { get; set; } = DateTime.MinValue;

	[JsonIgnore]
	public bool ManualStopRequested { get; set; }

}
