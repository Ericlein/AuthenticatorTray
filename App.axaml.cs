using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AuthenticatorTray.ViewModels;
using AuthenticatorTray.Views;

namespace AuthenticatorTray;

public partial class App : Application
{
    private static Mutex? _mutex;
    private PopupWindow? _popupWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Single-instance check
            _mutex = new Mutex(true, "AuthenticatorTrayApp", out bool createdNew);
            if (!createdNew)
            {
                desktop.Shutdown();
                return;
            }

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = null;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void TrayIcon_Clicked(object? sender, EventArgs e)
    {
        ShowPopup();
    }

    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            desktop.Shutdown();
        }
    }

    private void ShowPopup()
    {
        // Close existing popup if open
        if (_popupWindow is { IsVisible: true })
        {
            _popupWindow.Close();
            _popupWindow = null;
            return;
        }

        var viewModel = new PopupViewModel();
        _popupWindow = new PopupWindow(viewModel);
        _popupWindow.Show();
    }
}
