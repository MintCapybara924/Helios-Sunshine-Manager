using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Helios.Core.Process;
using Helios.Spawner;

// Helios.Spawner锛氬彲閬哥殑 SYSTEM 娆婇檺 spawner 鏈嶅嫏
// 浠?Windows Service 鏂瑰紡閬嬭锛屾帴鏀朵締鑷富绋嬪紡鐨勫暉鍕曡珛姹傦紝
// 閫忛亷 CreateProcessAsUser + WTSQueryUserToken 鍦ㄤ娇鐢ㄨ€?session 涓暉鍕?Vibeshine銆?// M2 灏囧浣?ProcessSpawnerService 鐨勬牳蹇冮倧杓€?
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = ServiceControlConstants.ServiceName;
});

builder.Services.AddHostedService<SpawnerWorker>();

var host = builder.Build();
host.Run();

