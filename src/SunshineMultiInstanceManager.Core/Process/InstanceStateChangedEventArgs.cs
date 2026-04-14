using System;

namespace Helios.Core.Process;

public sealed class InstanceStateChangedEventArgs : EventArgs
{
	public string InstanceId { get; }

	public string InstanceName { get; }

	public bool IsRunning { get; }

	public InstanceStateChangedEventArgs(string id, string name, bool running)
	{
		InstanceId = id;
		InstanceName = name;
		IsRunning = running;
	}
}

