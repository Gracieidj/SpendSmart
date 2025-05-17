using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;

public static class MessageBoxManager
{
    public static async Task ShowMessageBox(string title, string message)
    {
        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 80,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var textBlock = new TextBlock
        {
            Text = message,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(10),
            Children =
            {
                textBlock,
                okButton
            }
        };

        var messageBox = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            Content = panel
        };

        // Close the dialog when OK clicked
        okButton.Click += (_, _) => messageBox.Close();

        var lifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        if (lifetime?.MainWindow != null)
        {
            await messageBox.ShowDialog(lifetime.MainWindow);
        }
        else
        {
            // fallback - show without owner window (not modal)
            messageBox.Show();
        }
    }
}
