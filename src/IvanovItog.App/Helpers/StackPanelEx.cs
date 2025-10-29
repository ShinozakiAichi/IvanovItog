using System;
using System.Windows;
using System.Windows.Controls;

namespace IvanovItog.App.Helpers;

public static class StackPanelEx
{
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.RegisterAttached(
        "Spacing",
        typeof(double),
        typeof(StackPanelEx),
        new PropertyMetadata(0d, OnSpacingChanged));

    private static readonly DependencyProperty LastAppliedChildCountProperty = DependencyProperty.RegisterAttached(
        "LastAppliedChildCount",
        typeof(int),
        typeof(StackPanelEx),
        new PropertyMetadata(-1));

    private static readonly DependencyProperty OriginalMarginProperty = DependencyProperty.RegisterAttached(
        "OriginalMargin",
        typeof(Thickness),
        typeof(StackPanelEx));

    public static void SetSpacing(DependencyObject element, double value) => element.SetValue(SpacingProperty, value);

    public static double GetSpacing(DependencyObject element) => (double)element.GetValue(SpacingProperty);

    private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not StackPanel panel)
        {
            return;
        }

        panel.Loaded -= PanelOnLoaded;
        panel.Loaded += PanelOnLoaded;
        panel.LayoutUpdated -= PanelOnLayoutUpdated;
        panel.LayoutUpdated += PanelOnLayoutUpdated;

        ApplySpacing(panel, force: true);
    }

    private static void PanelOnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is StackPanel panel)
        {
            ApplySpacing(panel, force: true);
        }
    }

    private static void PanelOnLayoutUpdated(object? sender, EventArgs e)
    {
        if (sender is StackPanel panel)
        {
            ApplySpacing(panel);
        }
    }

    private static void ApplySpacing(StackPanel panel, bool force = false)
    {
        var childrenCount = panel.Children.Count;
        if (!force && childrenCount == GetLastAppliedChildCount(panel))
        {
            return;
        }

        var spacing = GetSpacing(panel);
        for (var i = 0; i < childrenCount; i++)
        {
            if (panel.Children[i] is not FrameworkElement child)
            {
                continue;
            }

            if (Equals(child.ReadLocalValue(OriginalMarginProperty), DependencyProperty.UnsetValue))
            {
                child.SetValue(OriginalMarginProperty, child.Margin);
            }

            var originalMargin = (Thickness)child.GetValue(OriginalMarginProperty);

            if (panel.Orientation == System.Windows.Controls.Orientation.Horizontal)
            {
                child.Margin = new Thickness(
                    i == 0 ? originalMargin.Left : originalMargin.Left + spacing,
                    originalMargin.Top,
                    originalMargin.Right,
                    originalMargin.Bottom);
            }
            else
            {
                child.Margin = new Thickness(
                    originalMargin.Left,
                    i == 0 ? originalMargin.Top : originalMargin.Top + spacing,
                    originalMargin.Right,
                    originalMargin.Bottom);
            }
        }

        SetLastAppliedChildCount(panel, childrenCount);
    }

    private static void SetLastAppliedChildCount(DependencyObject element, int value) =>
        element.SetValue(LastAppliedChildCountProperty, value);

    private static int GetLastAppliedChildCount(DependencyObject element) =>
        (int)element.GetValue(LastAppliedChildCountProperty);
}
