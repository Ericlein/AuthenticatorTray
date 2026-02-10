using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AuthenticatorTray.ViewModels;

namespace AuthenticatorTray.Views;

public partial class PopupWindow : Window
{
    private PopupViewModel? _viewModel;
    private bool _settingsOpen;

    public PopupWindow()
    {
        InitializeComponent();
    }

    public PopupWindow(PopupViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.OpenSettingsRequested += OnOpenSettingsRequested;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ApplyResponsiveSize();
        PositionNearTray();

        // Close when user clicks outside
        Deactivated += OnWindowDeactivated;
    }

    private void ApplyResponsiveSize()
    {
        var screen = Screens.Primary;
        if (screen == null) return;

        var workArea = screen.WorkingArea;
        var scaling = screen.Scaling;

        // Width: ~22% of screen, clamped between 340-480
        double screenWidth = workArea.Width / scaling;
        double popupWidth = Math.Clamp(screenWidth * 0.22, 340, 480);
        Width = popupWidth;

        // ScrollViewer max height: ~60% of work area
        double screenHeight = workArea.Height / scaling;
        double maxScrollHeight = screenHeight * 0.60;
        var scroller = this.FindControl<ScrollViewer>("AccountScroller");
        if (scroller != null)
            scroller.MaxHeight = maxScrollHeight;
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (!_settingsOpen)
            Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (_viewModel != null)
        {
            _viewModel.OpenSettingsRequested -= OnOpenSettingsRequested;
            _viewModel.Dispose();
        }
    }

    private void PositionNearTray()
    {
        var screen = Screens.Primary;
        if (screen == null) return;

        var workArea = screen.WorkingArea;
        var scaling = screen.Scaling;

        double width = Width * scaling;
        double height = this.Bounds.Height * scaling;

        double x, y;

        if (OperatingSystem.IsMacOS())
        {
            // macOS: top-right, below menu bar
            x = workArea.Right - width - 16;
            y = workArea.Y + 8;
        }
        else
        {
            // Windows: bottom-right, above taskbar
            x = workArea.Right - width - 16;
            y = workArea.Bottom - height - 16;
        }

        Position = new PixelPoint((int)(x / scaling), (int)(y / scaling));
    }

    private async void OnOpenSettingsRequested()
    {
        _settingsOpen = true;

        var settingsVm = new SettingsViewModel();
        var settingsWindow = new SettingsWindow(settingsVm);
        settingsWindow.AccountAdded += () =>
        {
            _viewModel?.ReloadAccounts();
        };
        await settingsWindow.ShowDialog(this);

        _settingsOpen = false;
    }

    private void AccountCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is AccountCardViewModel vm)
        {
            vm.CopyCodeCommand.Execute(null);
        }
    }
}
