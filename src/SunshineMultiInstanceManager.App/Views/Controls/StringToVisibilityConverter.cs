using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SunshineMultiInstanceManager.App.Views.Controls;

[ValueConversion(typeof(string), typeof(Visibility))]
public sealed class StringToVisibilityConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
