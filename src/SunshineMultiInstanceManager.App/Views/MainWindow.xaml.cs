using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using SunshineMultiInstanceManager.App.Services;
using SunshineMultiInstanceManager.App.ViewModels;
using SunshineMultiInstanceManager.Core.Display;
using SunshineMultiInstanceManager.Core.Storage;

namespace SunshineMultiInstanceManager.App.Views;

public partial class MainWindow
{
	private readonly SettingsStore _store;
	private readonly SettingsViewModel _settingsVm;
	private readonly DisplayWatcher? _displayWatcher;
	private SettingsWindow? _settingsWindow;

	private const int WM_DISPLAYCHANGE = 126;

	public MainWindow(MainViewModel vm, SettingsViewModel settingsVm, SettingsStore store, DisplayWatcher? displayWatcher = null)
	{
		InitializeComponent();
		DataContext = vm;
		_settingsVm = settingsVm;
		_store = store;
		_displayWatcher = displayWatcher;
		ApplyLocalizedCaptions();
		LocalizationService.LanguageChanged += ApplyLocalizedCaptions;
		LoadWindowState();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (App.AllowClose)
		{
			SaveWindowState();
			base.OnClosing(e);
		}
		else
		{
			e.Cancel = true;
			Hide();
		}
	}

	protected override void OnClosed(EventArgs e)
	{
		LocalizationService.LanguageChanged -= ApplyLocalizedCaptions;
		base.OnClosed(e);
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
		{
			hwndSource.AddHook(WndProc);
		}
	}

	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (msg == WM_DISPLAYCHANGE)
		{
			_displayWatcher?.OnDisplayChanged();
		}

		return IntPtr.Zero;
	}

	private void OpenSettings_Click(object sender, RoutedEventArgs e)
	{
		if (_settingsWindow != null && _settingsWindow.IsVisible)
		{
			_settingsWindow.Activate();
			return;
		}

		_settingsWindow = new SettingsWindow(_settingsVm)
		{
			Owner = this
		};
		_settingsWindow.Show();
	}

	private void ApplyLocalizedCaptions()
	{
		Title = LocalizationService.T("AppName");
		MainTitleBar.Title = LocalizationService.T("AppName");
		OpenSettingsButtonText.Text = LocalizationService.T("OpenSettings");
		OpenSettingsButton.ToolTip = LocalizationService.T("OpenSettings");
	}

	private void LoadWindowState()
	{
		var settings = _store.Settings;
		if (settings.RestoreWindowPosition && settings.WindowX.HasValue && settings.WindowY.HasValue)
		{
			Left = Math.Max(0, settings.WindowX.Value);
			Top = Math.Max(0, settings.WindowY.Value);
			Width = settings.WindowWidth ?? Width;
			Height = settings.WindowHeight ?? Height;
			WindowStartupLocation = WindowStartupLocation.Manual;
		}
	}

	private void SaveWindowState()
	{
		var settings = _store.Settings;
		settings.WindowX = Left;
		settings.WindowY = Top;
		settings.WindowWidth = ActualWidth;
		settings.WindowHeight = ActualHeight;
	}
}
