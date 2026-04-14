# Helios

Multi-instance manager for Sunshine and its forks.

A modern WPF application for managing multiple [Sunshine](https://github.com/LizardByte/Sunshine) (and its forks) streaming instances on a single Windows machine.

**Language / README**: English | [繁體中文](README.zh-TW.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

## Features

- **Multi-Instance Management** - Create, edit, clone, and delete independent streaming instances, each with its own port and configuration directory.
- **Supported Branches** - Manage instances for [Sunshine](https://github.com/LizardByte/Sunshine), [Apollo](https://github.com/ClassicOldSong/Apollo), [Vibeshine](https://github.com/Nonary/vibeshine), and [Vibepollo](https://github.com/Nonary/Vibepollo).
- **Service-Based Runtime** - Instances are controlled through the background Windows service (LocalSystem), designed for secure desktop scenarios (UAC and sign-in screen handling).
- **Per-Instance Runtime Controls** - Start/Stop/Open Web UI per instance, plus Start All / Stop All for batch operations.
- **Per-Instance Audio Routing** - Assign a specific output device for each instance.
- **Volume Synchronization** - Optionally sync system volume to managed instances.
- **In-App Release Fetch & Install** - Check GitHub Releases and download/install the latest installer for any supported branch (stable or pre-release selection).  
	This is a **manual, user-triggered update flow**, not background automatic updating.
- **Modern Desktop UX** - Fluent-style UI, light/dark theme, system tray integration, and runtime log viewer.

## Requirements

- **OS**: Windows 10 / 11 (x64)
- **Runtime**: [.NET 8.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Privileges**: Administrator (required for service registration and instance launching)
- **Sunshine**: At least one supported branch installed (Sunshine, Apollo, Vibeshine, or Vibepollo)

## Installation

1. Download `HeliosSetup.exe` from the latest release.
2. Run the installer and follow the on-screen instructions.
3. Launch `Helios.exe` as administrator.

On first launch, the application will automatically:
- Register the **Spawner Service** as a Windows Service (LocalSystem).
- Start the service and begin managing instances through it.

No manual service setup is required.

## Usage

1. **Add an instance** - Click the add button, select the product branch, set a name and port.
2. **Configure** - Adjust audio device, headless mode, extra arguments, and other options per instance.
3. **Start** - Enable the instance and click Start (or Start All). The Spawner Service launches each instance with full system privileges.
4. **Connect** - Use [Moonlight](https://moonlight-stream.org/) or any compatible client to connect to the configured port.
5. **Web UI** - Click the link icon next to an instance to open its web configuration panel.

## Build from Source

```bash
# Build the application
dotnet build src/SunshineMultiInstanceManager.App/Helios.App.csproj

# Publish (includes Spawner Service automatically)
dotnet publish src/SunshineMultiInstanceManager.App/Helios.App.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -o publish/win-x64-fd
```

The publish output at `publish/win-x64-fd/` includes the main application and the `service/` subdirectory containing the Spawner Service.

## Inno Setup Packaging Notes (Helios)

- Main publish command:

```bash
dotnet publish src/SunshineMultiInstanceManager.App/Helios.App.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -o publish/win-x64-fd
```

- Required payload:
	- `publish/win-x64-fd/Helios.exe`
	- full `publish/win-x64-fd/service/` directory (do not change folder layout)
	- optional icon `src/SunshineMultiInstanceManager.App/Assets/SMIM.ico`
- Inno setup essentials:
	- `PrivilegesRequired=admin`
	- `ArchitecturesInstallIn64BitMode=x64`
	- `ArchitecturesAllowed=x64`
	- `DefaultDirName={autopf}\Helios`
	- `UninstallDisplayIcon={app}\Helios.exe`
- Runtime dependency: .NET 8 Desktop Runtime (x64) is required (`--no-self-contained`).
- Service lifecycle:
	- Do not create service in installer; app first-run registers it.
	- On uninstall, stop and delete `HeliosService`.
- Conflict services (`SunshineService` / `ApolloService`) are handled by app startup logic, not installer.
- Keep `%ProgramData%\Helios` data on uninstall.
- During upgrade, stop service and close app (`Helios.exe`) before file replacement.

## Architecture

```
Helios.App      WPF desktop application (UI + local control)
Helios.Core     Shared library (process management, config, audio, display, updates)
Helios.Spawner  Windows Service (runs as SYSTEM, launches instances via Named Pipe commands)
```

The App communicates with the Spawner Service over a Named Pipe. The Service launches Sunshine instances using a SYSTEM token assigned to the user's interactive session, which allows capturing the secure desktop (UAC and login screen) - the same capability as a standard Sunshine service installation.

## Known Limitations

> **Vibeshine / Vibepollo installer conflict**: While this manager is designed to let multiple Sunshine-based branches coexist, the Vibeshine and Vibepollo installers will require you to uninstall any other Sunshine-based branch before proceeding - installation cannot continue unless you agree. If you install a Vibe-series branch first and then install Sunshine or Apollo afterward, they can temporarily coexist. However, the next time you update the Vibe-series branch, the installer will once again require removal of the other branches.

## Disclaimer

This project was built primarily for personal use. Functionality is not guaranteed to work in all environments or configurations. Use at your own risk.

## Inspiration

This project was inspired by [Apollo Fleet Launcher](https://github.com/drajabr/Apollo-Fleet-Launcher), a multi-instance launcher for Apollo. Helios expands on the concept with support for a broader range of Sunshine-based branches.

## AI Disclosure

This project was developed with the assistance of AI, including OpenAI Codex and Anthropic Claude.

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
