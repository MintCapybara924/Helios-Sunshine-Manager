using System;

namespace SunshineMultiInstanceManager.Core.Process;

public interface ILogger
{
	void LogInformation(string message, params object?[] args);

	void LogDebug(string message, params object?[] args);

	void LogWarning(string message, params object?[] args);

	void LogError(string message, params object?[] args);

	void LogError(Exception ex, string message, params object?[] args);
}
