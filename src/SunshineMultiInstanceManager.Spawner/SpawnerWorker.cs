using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Helios.Core.Process;
using Helios.Core.Scheduler;
using Helios.Core.Storage;
using Helios.Core.Storage.Models;

namespace Helios.Spawner;

/// <summary>
/// Spawner 鏈嶅嫏涓昏看鍦堬紙M2 灏囧浣滃闅涚殑 CreateProcessAsUser 閭忚集锛夈€?/// 鐩墠鐐轰綌浣嶉鏋讹紝纰轰繚 M1 鍙法璀€?/// </summary>
public sealed class SpawnerWorker : BackgroundService
{
    private readonly ILogger<SpawnerWorker> _logger;
    private readonly ServiceLogger _serviceLogger = new("ManagerService");
    private SettingsStore? _store;
    private ProcessManager? _processManager;
    private static readonly string[] ConflictingServiceNames = ["SunshineService", "ApolloService"];
    private readonly SemaphoreSlim _commandLock = new(1, 1);

    public SpawnerWorker(ILogger<SpawnerWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Helios service started.");
        _serviceLogger.LogInformation("Service boot sequence started.");

        _store = new SettingsStore();
        await _store.LoadAsync(stoppingToken);

        ProcessLauncher launcher = new(_serviceLogger);
        _processManager = new ProcessManager(_store, launcher, _serviceLogger);

        await EnforceConflictingServicesDisabledAsync(stoppingToken);
        await _processManager.StartAllAsync(stoppingToken);

        Task enforceTask = RunEnforcementLoopAsync(stoppingToken);
        Task pipeTask = RunPipeServerLoopAsync(stoppingToken);

        try
        {
            await Task.WhenAll(enforceTask, pipeTask);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_processManager != null)
            {
                await _processManager.StopAllAsync(CancellationToken.None);
                await _processManager.DisposeAsync();
            }
            _commandLock.Dispose();

            _serviceLogger.LogInformation("Service stopped.");
            _logger.LogInformation("Helios service stopped.");
        }
    }

    private Task EnforceConflictingServicesDisabledAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        foreach (string serviceName in ConflictingServiceNames)
        {
            try
            {
                if (!ServiceControllerHelper.IsInstalled(serviceName))
                {
                    continue;
                }

                ServiceControllerHelper.StopAndDisable(serviceName);
                _serviceLogger.LogInformation("Conflicting service disabled: {ServiceName}", serviceName);
            }
            catch (Exception ex)
            {
                _serviceLogger.LogWarning("Failed to disable conflicting service {ServiceName}: {Msg}", serviceName, ex.Message);
            }
        }

        return Task.CompletedTask;
    }

    private async Task RunEnforcementLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), ct);
            await EnforceConflictingServicesDisabledAsync(ct);
        }
    }

    private async Task RunPipeServerLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using NamedPipeServerStream pipe = new(
                ServiceControlConstants.PipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await pipe.WaitForConnectionAsync(ct);

            try
            {
                using StreamReader reader = new(pipe, Encoding.UTF8, leaveOpen: true);
                using StreamWriter writer = new(pipe, new UTF8Encoding(false), leaveOpen: true);

                string? requestJson = await reader.ReadLineAsync(ct);
                ServiceCommandResponse response;
                if (string.IsNullOrWhiteSpace(requestJson))
                {
                    response = new ServiceCommandResponse { Ok = false, Error = "Empty command." };
                }
                else
                {
                    response = await HandleCommandAsync(requestJson, ct);
                }

                await writer.WriteLineAsync(JsonSerializer.Serialize(response));
                await writer.FlushAsync();
            }
            catch (IOException)
            {
                // Client disconnected mid-request — safe to ignore and accept next connection.
            }
        }
    }

    private async Task<ServiceCommandResponse> HandleCommandAsync(string requestJson, CancellationToken ct)
    {
        try
        {
            ServiceCommandRequest? request = JsonSerializer.Deserialize<ServiceCommandRequest>(requestJson);
            if (request == null || string.IsNullOrWhiteSpace(request.Command))
            {
                return new ServiceCommandResponse { Ok = false, Error = "Invalid command." };
            }

            await _commandLock.WaitAsync(ct);
            try
            {
                await _store!.LoadAsync(ct);
                return await ExecuteCommandAsync(request, ct);
            }
            finally
            {
                _commandLock.Release();
            }
        }
        catch (Exception ex)
        {
            _serviceLogger.LogWarning("Pipe command failed: {Msg}", ex.Message);
            return new ServiceCommandResponse { Ok = false, Error = ex.Message };
        }
    }

    private async Task<ServiceCommandResponse> ExecuteCommandAsync(ServiceCommandRequest request, CancellationToken ct)
    {
        switch (request.Command.Trim().ToLowerInvariant())
        {
            case "start-all":
                await _processManager!.StartAllAsync(ct);
                return BuildStatusResponse();

            case "stop-all":
                await _processManager!.StopAllAsync(ct);
                return BuildStatusResponse();

            case "restart-instance":
            {
                InstanceConfig? instance = FindInstance(request.InstanceId);
                if (instance == null)
                {
                    return new ServiceCommandResponse { Ok = false, Error = "Instance not found." };
                }

                await _processManager!.RestartInstanceAsync(instance, ct);
                return BuildStatusResponse();
            }

            case "stop-instance":
            {
                InstanceConfig? instance = FindInstance(request.InstanceId);
                if (instance == null)
                {
                    return new ServiceCommandResponse { Ok = false, Error = "Instance not found." };
                }

                await _processManager!.StopInstanceAsync(instance, ct);
                return BuildStatusResponse();
            }

            case "status":
                return BuildStatusResponse();

            default:
                return new ServiceCommandResponse { Ok = false, Error = "Unknown command." };
        }
    }

    private ServiceCommandResponse BuildStatusResponse()
    {
        Dictionary<string, bool> statuses = new(StringComparer.OrdinalIgnoreCase);
        foreach (InstanceConfig instance in _store!.Settings.Instances)
        {
            statuses[instance.Id] = _processManager!.IsRunning(instance.Id);
        }

        return new ServiceCommandResponse
        {
            Ok = true,
            Statuses = statuses
        };
    }

    private InstanceConfig? FindInstance(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return _store!.Settings.Instances.Find(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}

