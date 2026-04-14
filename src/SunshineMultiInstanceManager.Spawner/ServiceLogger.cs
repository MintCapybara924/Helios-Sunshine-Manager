using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Helios.Core.Process;

namespace Helios.Spawner;

public sealed class ServiceLogger : Helios.Core.Process.ILogger
{
	private readonly string _name;

	private static readonly object FileLock = new();

	private static readonly string LogRoot = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
		"Helios");

	private static readonly string LogPath = Path.Combine(LogRoot, "service.log");

	public ServiceLogger(string name = "Service")
	{
		_name = name;
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
		string formatted = args != null && args.Length > 0 ? SafeFormat(message, args) : message;
		string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{_name}] {formatted}";
		Trace.WriteLine(line);
		WriteToFile(line);
	}

	private static void WriteToFile(string line)
	{
		try
		{
			lock (FileLock)
			{
				Directory.CreateDirectory(LogRoot);
				File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
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
			string format = Regex.Replace(template, "\\{[^}]+\\}", _ => "{" + idx++ + "}");
			return string.Format(format, args);
		}
		catch
		{
			return template;
		}
	}
}

