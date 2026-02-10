using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AuthenticatorTray.Models;
using AuthenticatorTray.Services;

namespace AuthenticatorTray.ViewModels;

public partial class PopupViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private string _timerText = string.Empty;

    [ObservableProperty]
    private string _timerColor = "#007AFF";

    public ObservableCollection<AccountCardViewModel> Accounts { get; } = new();

    public event Action? OpenSettingsRequested;

    public PopupViewModel()
    {
        LoadAccountCards();
        UpdateTimer();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) =>
        {
            UpdateTimer();
            RefreshCodes();
        };
        _timer.Start();
    }

    private void LoadAccountCards()
    {
        var accounts = AccountService.LoadAccounts();
        Accounts.Clear();
        foreach (var kvp in accounts)
        {
            Accounts.Add(new AccountCardViewModel(kvp.Key, kvp.Value.Secret, kvp.Value.Digits));
        }
    }

    public void ReloadAccounts()
    {
        LoadAccountCards();
    }

    private void UpdateTimer()
    {
        int remaining = TotpService.GetRemainingSeconds();
        TimerText = $"{remaining}s";

        if (remaining <= 5)
            TimerColor = "#FF3B30";
        else if (remaining <= 10)
            TimerColor = "#FF9500";
        else
            TimerColor = "#007AFF";
    }

    private void RefreshCodes()
    {
        foreach (var account in Accounts)
        {
            account.RefreshCode();
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        OpenSettingsRequested?.Invoke();
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}
