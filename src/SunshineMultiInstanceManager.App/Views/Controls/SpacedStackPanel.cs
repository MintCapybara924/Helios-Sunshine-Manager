using System.Windows;
using System.Windows.Controls;

namespace Helios.App.Views.Controls;

public sealed class SpacedStackPanel : StackPanel
{
	public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register("Spacing", typeof(double), typeof(SpacedStackPanel), (PropertyMetadata)(object)new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure, new PropertyChangedCallback(OnSpacingChanged)));

	public double Spacing
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(SpacingProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(SpacingProperty, (object)value);
		}
	}

	private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is SpacedStackPanel spacedStackPanel)
		{
			spacedStackPanel.ApplySpacing();
		}
	}

	protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
	{
		base.OnVisualChildrenChanged(visualAdded, visualRemoved);
		ApplySpacing();
	}

	private void ApplySpacing()
	{
		double spacing = Spacing;
		bool flag = base.Orientation == Orientation.Horizontal;
		bool flag2 = true;
		foreach (UIElement child in base.Children)
		{
			if (child is FrameworkElement frameworkElement)
			{
				double left = ((!flag2 && flag) ? spacing : 0.0);
				double top = ((!flag2 && !flag) ? spacing : 0.0);
				Thickness margin = frameworkElement.Margin;
				frameworkElement.Margin = new Thickness(left, top, margin.Right, margin.Bottom);
			}
			flag2 = false;
		}
	}
}

