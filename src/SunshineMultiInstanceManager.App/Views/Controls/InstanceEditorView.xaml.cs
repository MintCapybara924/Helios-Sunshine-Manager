using Microsoft.Win32;
using SunshineMultiInstanceManager.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SunshineMultiInstanceManager.App.Views.Controls;

public partial class InstanceEditorView : UserControl
{
	public InstanceEditorView()
	{
		InitializeComponent();
	}

	private void BrowseExePath_Click(object sender, RoutedEventArgs e)
	{
		if (sender is not FrameworkElement { DataContext: InstanceViewModel vm })
		{
			return;
		}

		var dialog = new OpenFileDialog
		{
			Title = "Select Sunshine executable",
			Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
			CheckFileExists = true,
			Multiselect = false
		};

		if (!string.IsNullOrWhiteSpace(vm.EditExecutablePath))
		{
			try
			{
				dialog.InitialDirectory = System.IO.Path.GetDirectoryName(vm.EditExecutablePath);
			}
			catch
			{
				// Keep default initial directory when existing path is invalid.
			}
		}

		if (dialog.ShowDialog() == true)
		{
			vm.EditExecutablePath = dialog.FileName;
		}
	}
}
