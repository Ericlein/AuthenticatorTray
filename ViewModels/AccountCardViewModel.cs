using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AuthenticatorTray.Services;

namespace AuthenticatorTray.ViewModels;

public partial class AccountCardViewModel : ObservableObject
{
    private readonly string _secret;
    private readonly int _digits;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private bool _isCopied;

    public string AccountKey { get; }

    public AccountCardViewModel(string name, string secret, int digits)
    {
        AccountKey = name;
        _secret = secret;
        _digits = digits;
        DisplayName = GetDisplayName(name);
        RefreshCode();
    }

    public void RefreshCode()
    {
        var raw = TotpService.GenerateCode(_secret, _digits);
        Code = TotpService.FormatCode(raw);
    }

    public string GetRawCode()
    {
        return TotpService.GenerateCode(_secret, _digits);
    }

    [RelayCommand]
    private async Task CopyCode()
    {
        if (IsCopied) return;

        var topLevel = Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow ?? Avalonia.Application.Current.GetTopLevel()
            : null;

        // Try to get clipboard from the popup window
        var clipboard = topLevel?.Clipboard;
        if (clipboard == null)
        {
            // Fallback: find any open window
            if (Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime dt)
            {
                foreach (var window in dt.Windows)
                {
                    clipboard = window.Clipboard;
                    if (clipboard != null) break;
                }
            }
        }

        if (clipboard != null)
        {
            await clipboard.SetTextAsync(GetRawCode());
        }

        IsCopied = true;
        _ = ResetCopiedState();
    }

    private async Task ResetCopiedState()
    {
        await Task.Delay(800);
        IsCopied = false;
    }

    private static string GetDisplayName(string fullName)
    {
        if (fullName.Contains('('))
            return fullName[..fullName.IndexOf('(')].Trim();
        return fullName.Length > 22 ? fullName[..19] + "..." : fullName;
    }
}

public static class ApplicationExtensions
{
    public static Avalonia.Controls.TopLevel? GetTopLevel(this Avalonia.Application app)
    {
        if (app.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }
}
