using System;
using System.Windows;

namespace IvanovItog.App.Helpers;

public static class WindowSizeHelper
{
    public static readonly DependencyProperty BindMinimumToContentProperty = DependencyProperty.RegisterAttached(
        "BindMinimumToContent",
        typeof(bool),
        typeof(WindowSizeHelper),
        new PropertyMetadata(false, OnBindMinimumToContentChanged));

    public static void SetBindMinimumToContent(Window element, bool value) =>
        element.SetValue(BindMinimumToContentProperty, value);

    public static bool GetBindMinimumToContent(Window element) =>
        (bool)element.GetValue(BindMinimumToContentProperty);

    private static void OnBindMinimumToContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window)
        {
            return;
        }

        if (e.NewValue is true)
        {
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.ContentRendered -= OnWindowContentRendered;
            window.ContentRendered += OnWindowContentRendered;
        }
        else
        {
            window.ContentRendered -= OnWindowContentRendered;
        }
    }

    private static void OnWindowContentRendered(object? sender, EventArgs e)
    {
        if (sender is not Window window)
        {
            return;
        }

        window.MinWidth = window.ActualWidth;
        window.MinHeight = window.ActualHeight;
        window.SizeToContent = SizeToContent.Manual;
        window.ContentRendered -= OnWindowContentRendered;
    }
}
