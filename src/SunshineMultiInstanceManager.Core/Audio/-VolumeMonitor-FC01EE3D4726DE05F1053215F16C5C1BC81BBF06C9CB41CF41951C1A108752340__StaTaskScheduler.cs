using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Helios.Core.Audio;

internal sealed class _003CVolumeMonitor_003EFC01EE3D4726DE05F1053215F16C5C1BC81BBF06C9CB41CF41951C1A108752340__StaTaskScheduler : TaskScheduler
{
	protected override void QueueTask(Task task)
	{
		Task task2 = task;
		Thread thread = new Thread((ThreadStart)delegate
		{
			TryExecuteTask(task2);
		});
		thread.IsBackground = true;
		thread.Name = "STA-AudioTask";
		thread.TrySetApartmentState(ApartmentState.STA);
		thread.Start();
	}

	protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
	{
		if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
		{
			return TryExecuteTask(task);
		}
		return false;
	}

	protected override IEnumerable<Task> GetScheduledTasks()
	{
		return Array.Empty<Task>();
	}
}

