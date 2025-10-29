using System.Windows;

using MessageBox = System.Windows.MessageBox;

namespace IvanovItog.App.Services;

public class DialogService
{
    public void ShowInfo(string message, string caption = "Информация") =>
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string message, string caption = "Ошибка") =>
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
}
