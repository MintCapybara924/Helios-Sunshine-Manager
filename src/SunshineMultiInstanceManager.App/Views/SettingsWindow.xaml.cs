using SunshineMultiInstanceManager.App.ViewModels;

namespace SunshineMultiInstanceManager.App.Views;

public partial class SettingsWindow
{
	public SettingsWindow(SettingsViewModel vm)
	{
		InitializeComponent();
		DataContext = vm;
	}
}
