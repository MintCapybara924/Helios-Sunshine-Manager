using System.Runtime.InteropServices;

namespace Helios.Core.Process;

internal static class NativeMethods
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct STARTUPINFO
	{
		public int cb;

		public string? lpReserved;

		public string? lpDesktop;

		public string? lpTitle;

		public int dwX;

		public int dwY;

		public int dwXSize;

		public int dwYSize;

		public int dwXCountChars;

		public int dwYCountChars;

		public int dwFillAttribute;

		public int dwFlags;

		public short wShowWindow;

		public short cbReserved2;

		public nint lpReserved2;

		public nint hStdInput;

		public nint hStdOutput;

		public nint hStdError;
	}

	internal struct PROCESS_INFORMATION
	{
		public nint hProcess;

		public nint hThread;

		public int dwProcessId;

		public int dwThreadId;
	}

	internal struct SECURITY_ATTRIBUTES
	{
		public int nLength;

		public nint lpSecurityDescriptor;

		public bool bInheritHandle;
	}

	internal enum SECURITY_IMPERSONATION_LEVEL
	{
		SecurityAnonymous,
		SecurityIdentification,
		SecurityImpersonation,
		SecurityDelegation
	}

	internal enum TOKEN_TYPE
	{
		TokenPrimary = 1,
		TokenImpersonation
	}

	internal enum TOKEN_INFORMATION_CLASS
	{
		TokenUser = 1,
		TokenGroups,
		TokenPrivileges,
		TokenOwner,
		TokenPrimaryGroup,
		TokenDefaultDacl,
		TokenSource,
		TokenType,
		TokenImpersonationLevel,
		TokenStatistics,
		TokenRestrictedSids,
		TokenSessionId,
		TokenGroupsAndPrivileges,
		TokenSessionReference,
		TokenSandBoxInert,
		TokenAuditPolicy,
		TokenOrigin,
		TokenElevationType,
		TokenLinkedToken = 19,
		TokenElevation
	}

	internal enum TOKEN_ELEVATION_TYPE
	{
		TokenElevationTypeDefault = 1,
		TokenElevationTypeFull,
		TokenElevationTypeLimited
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct TOKEN_LINKED_TOKEN
	{
		public nint LinkedToken;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct TOKEN_ELEVATION
	{
		public int TokenIsElevated;
	}

	internal delegate bool EnumWindowsProc(nint hWnd, nint lParam);

	internal delegate bool ConsoleCtrlHandlerRoutine(uint ctrlType);

	internal const uint CREATE_NO_WINDOW = 134217728u;

	internal const uint STARTF_USESHOWWINDOW = 1u;

	internal const short SW_HIDE = 0;

	internal const uint CREATE_NEW_CONSOLE = 16u;

	internal const uint CREATE_NEW_PROCESS_GROUP = 512u;

	internal const uint NORMAL_PRIORITY_CLASS = 32u;

	internal const uint ABOVE_NORMAL_PRIORITY_CLASS = 32768u;

	internal const uint INFINITE = uint.MaxValue;

	internal const uint WAIT_OBJECT_0 = 0u;

	internal const uint WAIT_TIMEOUT = 258u;

	internal const uint WAIT_FAILED = uint.MaxValue;

	internal const uint WM_CLOSE = 16u;

	internal const uint CTRL_BREAK_EVENT = 1u;

	internal const uint CTRL_C_EVENT = 0u;

	internal const int TOKEN_DUPLICATE = 2;

	internal const int TOKEN_QUERY = 8;

	internal const int TOKEN_ASSIGN_PRIMARY = 1;

	internal const int TOKEN_ADJUST_PRIVILEGES = 32;

	internal const int TOKEN_ALL_ACCESS = 983551;

	internal const uint MAXIMUM_ALLOWED = 33554432u;

	internal const int ERROR_INVALID_HANDLE = 6;

	internal const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400u;

	internal const uint TOKEN_ADJUST_SESSIONID = 0x0100u;

	internal const int ATTACH_PARENT_PROCESS = -1;

	internal const uint PROCESS_TERMINATE = 1u;

	internal const uint PROCESS_QUERY_INFORMATION = 1024u;

	internal const uint PROCESS_SYNCHRONIZE = 1048576u;

	internal const uint PROCESS_ALL_ACCESS = 2035711u;

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool CreateProcessAsUser(nint hToken, string? lpApplicationName, string? lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles, uint dwCreationFlags, nint lpEnvironment, string? lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

	[DllImport("kernel32.dll", SetLastError = true)]
	internal static extern uint WTSGetActiveConsoleSessionId();

	[DllImport("wtsapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool WTSQueryUserToken(uint sessionId, out nint phToken);

	[DllImport("advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

	[DllImport("kernel32.dll")]
	internal static extern nint GetCurrentProcess();

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool DuplicateTokenEx(nint hExistingToken, uint dwDesiredAccess, ref SECURITY_ATTRIBUTES lpTokenAttributes, SECURITY_IMPERSONATION_LEVEL impersonationLevel, TOKEN_TYPE tokenType, out nint phNewToken);

	[DllImport("advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool GetTokenInformation(nint tokenHandle, TOKEN_INFORMATION_CLASS tokenInformationClass, nint tokenInformation, int tokenInformationLength, out int returnLength);

	[DllImport("advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SetTokenInformation(nint tokenHandle, TOKEN_INFORMATION_CLASS tokenInformationClass, ref uint tokenInformation, int tokenInformationLength);

	[DllImport("userenv.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool CreateEnvironmentBlock(out nint lpEnvironment, nint hToken, [MarshalAs(UnmanagedType.Bool)] bool bInherit);

	[DllImport("userenv.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool DestroyEnvironmentBlock(nint lpEnvironment);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool CloseHandle(nint hObject);

	[DllImport("kernel32.dll", SetLastError = true)]
	internal static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool TerminateProcess(nint hProcess, uint uExitCode);

	[DllImport("kernel32.dll", SetLastError = true)]
	internal static extern nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool AttachConsole(int dwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool FreeConsole();

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine? handler, [MarshalAs(UnmanagedType.Bool)] bool add);
}

