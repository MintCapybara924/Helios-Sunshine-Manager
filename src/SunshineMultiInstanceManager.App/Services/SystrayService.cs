using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using SunshineMultiInstanceManager.App.ViewModels;

namespace SunshineMultiInstanceManager.App.Services;

public sealed class SystrayService : IDisposable
{
	private TaskbarIcon? _icon;

	private Window? _mainWindow;
	private MainViewModel? _mainViewModel;

	private MenuItem? _showItem;
	private MenuItem? _instancesRootItem;
	private MenuItem? _restartItem;

	private MenuItem? _exitItem;

	private bool _disposed;

	public void Initialize(Window mainWindow)
	{
		_mainWindow = mainWindow;
		_mainViewModel = mainWindow.DataContext as MainViewModel;
		_icon = new TaskbarIcon
		{
			ToolTipText = LocalizationService.T("AppName"),
			IconSource = TryGetAppIcon(),
			ContextMenu = BuildContextMenu()
		};
		HookMainViewModelEvents();
		LocalizationService.LanguageChanged += ApplyLocalizedMenu;
		ApplyLocalizedMenu();
		_icon.TrayMouseDoubleClick += delegate
		{
			ShowOrRestoreWindow();
		};
	}

	public void ShowBalloon(string title, string message, BalloonIcon icon = BalloonIcon.Info)
	{
		_icon?.ShowBalloonTip(title, message, icon);
	}

	public void ShowOrRestoreWindow()
	{
		if (_mainWindow != null)
		{
			if (!_mainWindow.IsVisible)
			{
				_mainWindow.Show();
			}
			if (_mainWindow.WindowState == WindowState.Minimized)
			{
				_mainWindow.WindowState = WindowState.Normal;
			}
			_mainWindow.Activate();
			_mainWindow.Focus();
		}
	}

	private ContextMenu BuildContextMenu()
	{
		ContextMenu contextMenu = new ContextMenu();

		_showItem = new MenuItem
		{
			Header = LocalizationService.T("TrayShowMain")
		};
		_showItem.Click += delegate
		{
			ShowOrRestoreWindow();
		};
		contextMenu.Items.Add(_showItem);

		_instancesRootItem = new MenuItem
		{
			Header = LocalizationService.T("TrayInstances")
		};
		contextMenu.Items.Add(_instancesRootItem);
		RebuildInstanceMenuItems();

		contextMenu.Items.Add(new Separator());
		_restartItem = new MenuItem
		{
			Header = LocalizationService.T("TrayRestart")
		};
		_restartItem.Click += async delegate
		{
			try
			{
				string? exePath = Environment.ProcessPath;
				if (!string.IsNullOrWhiteSpace(exePath))
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = exePath,
						UseShellExecute = true
					});
				}
			}
			catch
			{
			}

			Application current = Application.Current;
			if (current is App app)
			{
				await app.RequestShutdownAsync(stopInstances: false);
			}
			else
			{
				Application.Current.Shutdown();
			}
		};
		contextMenu.Items.Add(_restartItem);

		_exitItem = new MenuItem
		{
			Header = LocalizationService.T("TrayExit")
		};
		_exitItem.Click += async delegate
		{
			Application current = Application.Current;
			if (current is App app)
			{
				await app.RequestShutdownAsync(stopInstances: true);
			}
			else
			{
				Application.Current.Shutdown();
			}
		};
		contextMenu.Items.Add(_exitItem);
		return contextMenu;
	}

		private void RebuildInstanceMenuItems()
		{
			if (_instancesRootItem == null)
			{
				return;
			}

			_instancesRootItem.Items.Clear();

			if (_mainViewModel == null || _mainViewModel.Instances.Count == 0)
			{
				_instancesRootItem.Items.Add(new MenuItem
				{
					Header = LocalizationService.T("EditorSelectPrompt"),
					IsEnabled = false
				});
				return;
			}

			foreach (InstanceViewModel instance in _mainViewModel.Instances)
			{
				string status = instance.IsRunning
					? LocalizationService.T("RunStatusRunning")
					: LocalizationService.T("RunStatusStopped");

				MenuItem instanceItem = new MenuItem
				{
					Header = $"{instance.EditName} ({status})"
				};

				MenuItem openWebUiItem = new MenuItem
				{
					Header = LocalizationService.T("EditorOpenWebUi")
				};
				openWebUiItem.Click += delegate
				{
					OpenInstanceWebUi(instance);
				};

				MenuItem startItem = new MenuItem
				{
					Header = LocalizationService.T("EditorStart")
				};
				startItem.Click += delegate
				{
					if (_mainViewModel == null)
					{
						return;
					}

					_mainViewModel.SelectedInstance = instance;
					if (_mainViewModel.StartInstanceCommand.CanExecute(null))
					{
						_mainViewModel.StartInstanceCommand.Execute(null);
					}
				};

				MenuItem stopItem = new MenuItem
				{
					Header = LocalizationService.T("EditorStop")
				};
				stopItem.Click += delegate
				{
					if (_mainViewModel == null)
					{
						return;
					}

					_mainViewModel.SelectedInstance = instance;
					if (_mainViewModel.StopInstanceCommand.CanExecute(null))
					{
						_mainViewModel.StopInstanceCommand.Execute(null);
					}
				};

				instanceItem.Items.Add(openWebUiItem);
				instanceItem.Items.Add(startItem);
				instanceItem.Items.Add(stopItem);
				_instancesRootItem.Items.Add(instanceItem);
			}
		}

		private static void OpenInstanceWebUi(InstanceViewModel instance)
		{
			int webUiPort = instance.Port + 1;
			string[] urls =
			[
				$"https://127.0.0.1:{webUiPort}",
				$"http://127.0.0.1:{webUiPort}"
			];

			foreach (string url in urls)
			{
				try
				{
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
					{
						FileName = url,
						UseShellExecute = true
					});
					return;
				}
				catch
				{
				}
			}
		}

		private void HookMainViewModelEvents()
		{
			if (_mainViewModel == null)
			{
				return;
			}

			_mainViewModel.Instances.CollectionChanged += OnInstancesCollectionChanged;
			foreach (InstanceViewModel instance in _mainViewModel.Instances)
			{
				instance.PropertyChanged += OnInstancePropertyChanged;
			}
		}

		private void UnhookMainViewModelEvents()
		{
			if (_mainViewModel == null)
			{
				return;
			}

			_mainViewModel.Instances.CollectionChanged -= OnInstancesCollectionChanged;
			foreach (InstanceViewModel instance in _mainViewModel.Instances)
			{
				instance.PropertyChanged -= OnInstancePropertyChanged;
			}
		}

		private void OnInstancesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (object item in e.NewItems)
				{
					if (item is InstanceViewModel instance)
					{
						instance.PropertyChanged += OnInstancePropertyChanged;
					}
				}
			}

			if (e.OldItems != null)
			{
				foreach (object item in e.OldItems)
				{
					if (item is InstanceViewModel instance)
					{
						instance.PropertyChanged -= OnInstancePropertyChanged;
					}
				}
			}

			RebuildInstanceMenuItems();
		}

		private void OnInstancePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(InstanceViewModel.EditName)
				|| e.PropertyName == nameof(InstanceViewModel.EditPort)
				|| e.PropertyName == nameof(InstanceViewModel.IsRunning))
			{
				RebuildInstanceMenuItems();
			}
		}

	private static ImageSource? TryGetAppIcon()
	{
		try
		{
			BitmapFrame frame = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/SMIM.ico", UriKind.Absolute));
			frame.Freeze();
			return frame;
		}
		catch
		{
			return null;
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			LocalizationService.LanguageChanged -= ApplyLocalizedMenu;
			UnhookMainViewModelEvents();
			_icon?.Dispose();
			_disposed = true;
		}
	}

	private void ApplyLocalizedMenu()
	{
		if (_icon != null)
		{
			_icon.ToolTipText = LocalizationService.T("AppName");
		}
		if (_showItem != null)
		{
			_showItem.Header = LocalizationService.T("TrayShowMain");
		}
		if (_instancesRootItem != null)
		{
			_instancesRootItem.Header = LocalizationService.T("TrayInstances");
			RebuildInstanceMenuItems();
		}
		if (_restartItem != null)
		{
			_restartItem.Header = LocalizationService.T("TrayRestart");
		}
		if (_exitItem != null)
		{
			_exitItem.Header = LocalizationService.T("TrayExit");
		}
	}
}
