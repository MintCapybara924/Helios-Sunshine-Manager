using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Microsoft.Win32.TaskScheduler;
using SunshineMultiInstanceManager.Core.Process;

namespace SunshineMultiInstanceManager.Core.Scheduler;

public sealed class SchedulerService
{
	private readonly ILogger _logger;

	private static string FullTaskPath => "\\SunshineMultiInstanceManager\\SunshineMultiInstanceManager_AutoStart";

	public SchedulerService(ILogger logger)
	{
		_logger = logger;
	}

	public bool IsAutoStartEnabled()
	{
		try
		{
			TaskService taskService = new TaskService();
			try
			{
				Microsoft.Win32.TaskScheduler.Task? task = taskService.GetTask(FullTaskPath);
				if (task == null)
				{
					return false;
				}

				if (task.Enabled && IsLegacySystemBootTask(task.Definition))
				{
					_logger.LogInformation("Migrating legacy boot/system autostart task to user-logon task.");
					EnableAutoStart();
					return true;
				}

				return task.Enabled;
			}
			finally
			{
				((IDisposable)(object)taskService)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning("IsAutoStartEnabled 鏌ヨ澶辨晽锛歿Msg}", ex.Message);
			return false;
		}
	}

	public void EnableAutoStart()
	{
		string path = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
		string workingDirectory = Path.GetDirectoryName(path) ?? string.Empty;
		string currentUser = WindowsIdentity.GetCurrent().Name;
		TaskService taskService = new TaskService();
		try
		{
			EnsureTaskFolder(taskService);
			TaskDefinition taskDefinition = taskService.NewTask();
				taskDefinition.RegistrationInfo.Description = "Sunshine Multi-Instance Manager tray autostart at user sign-in";
			taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
			taskDefinition.Principal.UserId = currentUser;
			taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
			taskDefinition.Settings.DisallowStartIfOnBatteries = false;
			taskDefinition.Settings.StopIfGoingOnBatteries = false;
			taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
			taskDefinition.Settings.StartWhenAvailable = true;
			taskDefinition.Triggers.Add(new LogonTrigger
			{
				UserId = currentUser,
				Delay = TimeSpan.FromSeconds(8.0)
			});
			taskDefinition.Actions.Add(new ExecAction(path, null, workingDirectory));
			taskService.RootFolder.RegisterTaskDefinition(FullTaskPath, taskDefinition, TaskCreation.CreateOrUpdate, currentUser, null, TaskLogonType.InteractiveToken);
				_logger.LogInformation("Autostart scheduled task created: {Path} (logon trigger for {User}, delay {Delay}s)", FullTaskPath, currentUser, 8);
		}
		finally
		{
			((IDisposable)(object)taskService)?.Dispose();
		}
	}

	public void DisableAutoStart()
	{
		TaskService taskService = new TaskService();
		try
		{
			taskService.RootFolder.DeleteTask(FullTaskPath, exceptionOnNotExists: false);
			_logger.LogInformation("宸插埅闄ら枊姗熻嚜鍟熸帓绋嬪伐浣滐細{Path}", FullTaskPath);
		}
		finally
		{
			((IDisposable)(object)taskService)?.Dispose();
		}
	}

	private static void EnsureTaskFolder(TaskService ts)
	{
		string text = "\\SunshineMultiInstanceManager".TrimStart('\\');
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		try
		{
			ts.GetFolder("\\" + text);
		}
		catch
		{
			try
			{
				ts.RootFolder.CreateFolder(text, null, exceptionOnExists: false);
			}
			catch
			{
			}
		}
	}

	private static bool IsLegacySystemBootTask(TaskDefinition definition)
	{
		bool isSystemPrincipal = string.Equals(definition.Principal?.UserId, "SYSTEM", StringComparison.OrdinalIgnoreCase)
			|| definition.Principal?.LogonType == TaskLogonType.ServiceAccount;

		bool hasBootTrigger = definition.Triggers.Cast<Trigger>().Any(t => t.TriggerType == TaskTriggerType.Boot);
		return isSystemPrincipal || hasBootTrigger;
	}
}
