using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AuthenticatorTray.ViewModels;

namespace AuthenticatorTray.Views;

public partial class SettingsWindow : Window
{
    private SettingsViewModel? _viewModel;

    public event Action? AccountAdded;

    public SettingsWindow()
    {
        InitializeComponent();
    }

    public SettingsWindow(SettingsViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.PickFileRequested += OnPickFileRequested;
        viewModel.AccountAddedSuccessfully += OnAccountAdded;
    }

    private async Task<string?> OnPickFileRequested()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select QR Code Image",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Image files")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" }
                },
                FilePickerFileTypes.All
            }
        });

        if (files.Count > 0)
        {
            return files[0].TryGetLocalPath();
        }
        return null;
    }

    private void OnAccountAdded()
    {
        AccountAdded?.Invoke();
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (_viewModel != null)
        {
            _viewModel.PickFileRequested -= OnPickFileRequested;
            _viewModel.AccountAddedSuccessfully -= OnAccountAdded;
        }
    }
}
