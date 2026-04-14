using System.Collections.Generic;

namespace Helios.Core.Process;

public static class ServiceControlConstants
{
	public const string ServiceName = "HeliosService";
	public const string PipeName = "HeliosServicePipe";
}

public sealed class ServiceCommandRequest
{
	public string Command { get; set; } = string.Empty;
	public string? InstanceId { get; set; }
}

public sealed class ServiceCommandResponse
{
	public bool Ok { get; set; }
	public string? Error { get; set; }
	public Dictionary<string, bool>? Statuses { get; set; }
}

