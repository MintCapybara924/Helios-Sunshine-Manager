using Helios.App.ViewModels;

namespace Helios.App.Views;

public partial class SettingsWindow
{
	public SettingsWindow(SettingsViewModel vm)
	{
		InitializeComponent();
		DataContext = vm;
	}
}

