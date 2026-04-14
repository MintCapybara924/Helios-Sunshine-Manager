#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Helios.Core.Process;
using Helios.Core.Storage;

namespace Helios.App.Services;

public sealed class AppLogger : ILogger
{
	private readonly string _name;

	private readonly Action<string>? _onLog;

	private static readonly object s_fileLock = new object();

	private static readonly string s_logPath = Path.Combine(SettingsStore.AppDataRoot, "launcher.log");

	public AppLogger(string name = "App", Action<string>? onLog = null)
	{
		_name = name;
		_onLog = onLog;
	}

	public void LogInformation(string message, params object?[] args)
	{
		Write("INFO", message, args);
	}

	public void LogDebug(string message, params object?[] args)
	{
		Write("DEBG", message, args);
	}

	public void LogWarning(string message, params object?[] args)
	{
		Write("WARN", message, args);
	}

	public void LogError(string message, params object?[] args)
	{
		Write("ERR ", message, args);
	}

	public void LogError(Exception ex, string message, params object?[] args)
	{
		Write("ERR ", message, args);
		Write("ERR ", ex.ToString());
	}

	private void Write(string level, string message, object?[]? args = null)
	{
		string value = ((args != null && args.Length > 0) ? SafeFormat(message, args) : message);
		string text = $"[{DateTime.Now:HH:mm:ss}] [{level}] [{_name}] {value}";
		Trace.WriteLine(text);
		Console.Error.WriteLine(text);
		WriteToFile(text);
		_onLog?.Invoke(text);
	}

	private static void WriteToFile(string line)
	{
		try
		{
			lock (s_fileLock)
			{
				Directory.CreateDirectory(SettingsStore.AppDataRoot);
				File.AppendAllText(s_logPath, line + Environment.NewLine, Encoding.UTF8);
			}
		}
		catch
		{
		}
	}

	private static string SafeFormat(string template, object?[] args)
	{
		try
		{
			int idx = 0;
			string format = Regex.Replace(template, "\\{[^}]+\\}", (Match _) => "{" + idx++ + "}");
			return string.Format(format, args);
		}
		catch
		{
			return template;
		}
	}
}

