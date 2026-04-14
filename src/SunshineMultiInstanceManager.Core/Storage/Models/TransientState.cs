using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Helios.Core.Storage.Models;

public sealed class TransientState
{
	[JsonPropertyName("instances")]
	public Dictionary<string, InstanceRuntimeState> Instances { get; set; } = new Dictionary<string, InstanceRuntimeState>();


	[JsonPropertyName("windowVisible")]
	public bool WindowVisible { get; set; } = true;


	[JsonPropertyName("logPanelVisible")]
	public bool LogPanelVisible { get; set; }
}

