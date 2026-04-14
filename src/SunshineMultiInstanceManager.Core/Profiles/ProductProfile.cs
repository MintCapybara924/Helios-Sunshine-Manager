using System.IO;

namespace Helios.Core.Profiles;

/// <summary>
/// Vibeshine 鐢㈠搧甯告暩瑷畾妾斻€?/// 鎵€鏈夎垏鐗瑰畾 Sunshine 鍒嗘敮鐗堟湰鐩镐緷鐨勮矾寰戙€佹湇鍕欏悕绋便€佸彲鍩疯妾斿悕绋辩殕闆嗕腑鏂兼锛?/// 鏈締鑻ラ渶鏀彺鍘熺増 Sunshine 鎴?Apollo 鍙渶鏂板灏嶆噳鐨?Profile 椤炲垾锛屼富閭忚集涓嶅嫊。"///
/// 璩囨枡渚嗘簮锛?///   - Vibeshine GitHub: https://github.com/Nonary/vibeshine
///   - 瀹夎绋嬪紡妾斿悕: VibeshineSetup.exe锛堝緸 releases 闋侀潰纰鸿獚锛?///   - CMake 鍙煼琛岀洰妯? sunshine锛坈make/targets/common.cmake锛夆啋 sunshine.exe
///   - 鏈嶅嫏鍚嶇ū: 寰?src_assets/windows/misc/service/install-service.bat 纰鸿獚
///   - 瀹夎璺緫: 寰?cmake/packaging/windows.cmake 鐨?CPACK_PACKAGE_INSTALL_DIRECTORY 鍙栧緱
/// </summary>
public static class VibeshineProfile
{
    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
    // 鍙煼琛屾獢鑸囪矾寰?    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    /// <summary>
    /// Vibeshine 涓荤▼寮忓彲鍩疯妾斿悕绋便€?    /// CMake target 鍚嶇ū鐐?"sunshine"锛屾晠绶ㄨ寰岀敘鐢?sunshine.exe。"    /// // TODO: verify against Vibeshine 鈥?鑻ュ闅涘畨瑁濆緦 exe 鍚嶇ū宸叉敼鐐?vibeshine.exe锛岃珛鏇存柊姝ゅ€笺€?    /// </summary>
    public const string ExecutableName = "sunshine.exe";

    /// <summary>
    /// 鏈嶅嫏杓斿姪绋嬪紡鍙煼琛屾獢鍚嶇ū锛堜綅鏂煎畨瑁濈洰閷勭殑 tools\ 瀛愮洰閷勪笅锛夈€?    /// 寰?install-service.bat 涓殑 sc create 鎸囦护纰鸿獚鐐?sunshinesvc.exe。"    /// // TODO: verify against Vibeshine 鈥?纰鸿獚 Vibeshine 瀹夎寰?tools\ 鍏х殑鏈嶅嫏 exe 鍚嶇ū。"    /// </summary>
    public const string ServiceExecutableName = "sunshinesvc.exe";

    /// <summary>
    /// 闋愯ō瀹夎鏍圭洰閷勩€?    /// cmake/packaging/windows.cmake 涓?CPACK_PACKAGE_INSTALL_DIRECTORY = "Sunshine"锛?    /// 鏁呴爯瑷畨瑁濇柤姝よ矾寰戙€?    /// // TODO: verify against Vibeshine 鈥?纰鸿獚瀵﹂殯瀹夎寰岀洰閷勫悕绋辨槸鍚﹀凡鏀圭偤 Vibeshine。"    ///         鑻ュ畨瑁濆櫒灏囩洰閷勬敼鐐?"Vibeshine"锛岃珛鏀圭偤 @"C:\Program Files\Vibeshine"。"    /// </summary>
    public static readonly string DefaultInstallPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Sunshine"  // TODO: verify against Vibeshine
        );

    /// <summary>
    /// 瀹屾暣鐨勪富绋嬪紡璺緫锛坕nstall root + exe 鍚嶇ū锛夈€?    /// </summary>
    public static string DefaultExecutablePath =>
        Path.Combine(DefaultInstallPath, ExecutableName);

    /// <summary>
    /// 瀹屾暣鐨勬湇鍕欒紨鍔╃▼寮忚矾寰戯紙install root\tools\ + exe 鍚嶇ū锛夈€?    /// </summary>
    public static string DefaultServiceExecutablePath =>
        Path.Combine(DefaultInstallPath, "tools", ServiceExecutableName);

    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
    // Windows Service
    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    /// <summary>
    /// Windows Service 鍚嶇ū锛坰c.exe 鍙?SCM 涓娇鐢ㄧ殑鍏ч儴鍚嶇ū锛夈€?    /// 寰?install-service.bat锛歴c create SunshineService binPath= "%SERVICE_BIN%"
    /// // TODO: verify against Vibeshine 鈥?纰鸿獚鏈嶅嫏鍚嶇ū鏄惁宸叉敼鐐?VibeshineService。"    /// </summary>
    public const string WindowsServiceName = "SunshineService"; // TODO: verify against Vibeshine

    /// <summary>
    /// Windows Service 鐨勯’绀哄悕绋憋紙Services 绠＄悊鍝′腑鍙锛夈€?    /// // TODO: verify against Vibeshine
    /// </summary>
    public const string WindowsServiceDisplayName = "Sunshine Service"; // TODO: verify against Vibeshine

    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
    // 缍茶矾 Port
    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    /// <summary>
    /// Vibeshine/Sunshine 闋愯ō HTTPS Web UI port。"    /// 鑸囧師鐗?Sunshine 鐩稿悓锛?7989锛夈€?    /// // TODO: verify against Vibeshine 鈥?鑻?Vibeshine 淇敼浜嗛爯瑷?port锛岃珛鏇存柊。"    /// </summary>
    public const int DefaultHttpsPort = 47989; // TODO: verify against Vibeshine

    /// <summary>
    /// Vibeshine/Sunshine 闋愯ō HTTP Web UI port。"    /// </summary>
    public const int DefaultHttpPort = 47984; // TODO: verify against Vibeshine

    /// <summary>
    /// Manager instance default port base. The first instance uses this port.
    /// </summary>
    public const int InstanceBasePort = 48100;

    /// <summary>
    /// Port step between instances.
    /// </summary>
    public const int InstancePortStep = 100;

    /// <summary>Port 鏈夋晥绡勫湇涓嬮檺锛堝惈锛夈€?/summary>
    public const int PortMinimum = 1025;

    /// <summary>Port 鏈夋晥绡勫湇涓婇檺锛堝惈锛夈€?/summary>
    public const int PortMaximum = 65000;

    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
    // 瑷畾妾旀瑒浣嶅悕绋憋紙sunshine.conf key = value 鏍煎紡锛?    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    /// <summary>
    /// sunshine.conf 涓敤鏂兼寚瀹氬渚嬮’绀哄悕绋辩殑娆勪綅。"    /// 鑸囧師鐗?Sunshine 鐩稿悓。"    /// </summary>
    public const string ConfKeySunshineName = "sunshine_name";

    /// <summary>sunshine.conf 涓寚瀹氫富鐩ｈ伣 port 鐨勬瑒浣嶃€?/summary>
    public const string ConfKeyPort = "port";

    /// <summary>sunshine.conf 涓寚瀹?state 妾旀璺緫鐨勬瑒浣嶃€?/summary>
    public const string ConfKeyFileState = "file_state";

    /// <summary>sunshine.conf 涓寚瀹?apps.json 璺緫鐨勬瑒浣嶃€?/summary>
    public const string ConfKeyFileApps = "file_apps";

    /// <summary>sunshine.conf 涓寚瀹?Vibeshine 鐙€鎱嬫獢璺緫鐨勬瑒浣嶃€?/summary>
    public const string ConfKeyVibeshineFileState = "vibeshine_file_state";

    /// <summary>sunshine.conf 涓寚瀹?Web UI 甯冲瘑妾旇矾寰戠殑娆勪綅。"/summary>
    public const string ConfKeyCredentialsFile = "credentials_file";

    /// <summary>sunshine.conf 涓寚瀹?TLS 绉侀懓璺緫鐨勬瑒浣嶃€?/summary>
    public const string ConfKeyPkey = "pkey";

    /// <summary>sunshine.conf 涓寚瀹?TLS 鎲戣瓑璺緫鐨勬瑒浣嶃€?/summary>
    public const string ConfKeyCert = "cert";

    /// <summary>sunshine.conf 涓寚瀹氭棩瑾屾獢璺緫鐨勬瑒浣嶃€?/summary>
    public const string ConfKeyLogPath = "log_path";

    /// <summary>sunshine.conf 涓櫅鎿’绀烘ā寮忔瑒浣嶃€?/summary>
    public const string ConfKeyVirtualDisplayMode = "virtual_display_mode";

    /// <summary>sunshine.conf 涓?Display Device 閰嶇疆妯″紡娆勪綅。"/summary>
    public const string ConfKeyDdConfigurationOption = "dd_configuration_option";

    /// <summary>sunshine.conf 涓槸鍚﹀暉鐢ㄨ櫅鎿’绀哄暉鐢ㄦ祦绋嬫瑒浣嶃€?/summary>
    public const string ConfKeyDdActivateVirtualDisplay = "dd_activate_virtual_display";

    /// <summary>
    /// sunshine.conf 涓寚瀹氶煶瑷婅几鍑鸿缃殑娆勪綅锛堣櫅鎿?sink 妯″紡锛夈€?    /// // TODO: verify against Vibeshine 鈥?Vibeshine 鏄惁鏂板浜嗙崹鏈夌殑闊宠▕娆勪綅。"    /// </summary>
    public const string ConfKeyAudioSink = "audio_sink";

    /// <summary>sunshine.conf 涓寚瀹氳櫅鎿煶瑷?sink 鐨勬瑒浣嶃€?/summary>
    public const string ConfKeyVirtualSink = "virtual_sink";

    /// <summary>
    /// sunshine.conf 涓帶鍒舵槸鍚﹁嚜鍕曢伕鍙?capture sink 鐨勬瑒浣嶃€?    /// 鍊肩偤 "enabled" 鎴?"disabled"。"    /// </summary>
    public const string ConfKeyAutoCaptureSink = "auto_capture_sink";

    /// <summary>
    /// sunshine.conf 涓帶鍒剁劇闋ā寮忥紙headless mode锛夌殑娆勪綅。"    /// 鍊肩偤 "enabled" 鎴?"disabled"。"    /// // TODO: verify against Vibeshine 鈥?纰鸿獚 Vibeshine 姝ゆ瑒浣嶅悕绋辫垏琛岀偤鏄惁鑸?Sunshine 涓€鑷淬€?    /// </summary>
    public const string ConfKeyHeadlessMode = "headless_mode"; // TODO: verify against Vibeshine

    /// <summary>
    /// sunshine.conf 涓帶鍒跺鎴剁鏂风窔鏅傛槸鍚︾祩姝㈤€茬▼鐨勬瑒浣嶃€?    /// // TODO: verify against Vibeshine 鈥?纰鸿獚姝ゆ瑒浣嶅湪 Vibeshine 鏄惁瀛樺湪鍙婃瑒浣嶅悕绋便€?    /// </summary>
    public const string ConfKeyTerminateOnPause = "terminate_on_pause"; // TODO: verify against Vibeshine

    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
    // 鎳夌敤绋嬪紡璀樺垾
    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    /// <summary>Manager application display name used by system integrations.</summary>
    public const string ManagerAppName = "Helios";

    /// <summary>Task Scheduler 涓帓绋嬪伐浣滅殑鍚嶇ū。"/summary>
    public const string ScheduledTaskName = "Helios_AutoStart";

    /// <summary>Task Scheduler 涓帓绋嬪伐浣滄墍鍦ㄧ殑璩囨枡澶俱€?/summary>
    public const string ScheduledTaskFolder = "\\Helios";

    /// <summary>闁嬫鑷暉鐨勫欢閬叉檪闁擄紙绉掞級銆傚皪鎳?AHK 鐗堢殑 PT30S 瑷畾。"/summary>
    public const int AutoStartDelaySeconds = 30;

    /// <summary>Windows service name for the spawner component.</summary>
    public const string SpawnerServiceName = "HeliosSpawner";

    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
    // 閫茬▼瀹堣
    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    /// <summary>瀹堣杩村湀妾㈡煡闁撻殧锛堟绉掞級銆傚皪鎳?AHK 鐗堢殑 5000ms timer。"/summary>
    public const int GuardianIntervalMs = 5000;

    /// <summary>鍎泤闂滈枆閫炬檪锛堟绉掞級銆傝秴閬庢鏅傞枔寰屽挤鍒?Terminate。"/summary>
    public const int GracefulShutdownTimeoutMs = 8000;

    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
    // 瑷堢畻杓斿姪鏂规硶
    // 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    /// <summary>
    /// Compute default port by 1-based instance index.
    /// </summary>
    /// <param name="instanceIndex">瀵︿緥搴忚櫉锛屽緸 1 闁嬪。"/param>
    public static int GetDefaultPort(int instanceIndex) =>
        InstanceBasePort + (instanceIndex - 1) * InstancePortStep;
}

