using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AuthenticatorTray.Models;
using AuthenticatorTray.Services;

namespace AuthenticatorTray.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _secret = string.Empty;

    [ObservableProperty]
    private string _digits = "6";

    [ObservableProperty]
    private string _algorithm = "SHA1";

    [ObservableProperty]
    private bool _fieldsEnabled;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isError;

    public event Action? AccountAddedSuccessfully;
    public event Func<Task<string?>>? PickFileRequested;

    [RelayCommand]
    private async Task ScanQrCode()
    {
        StatusMessage = string.Empty;

        var filePath = PickFileRequested != null ? await PickFileRequested.Invoke() : null;
        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            var qrText = QrCodeService.DecodeQrCodeFromImage(filePath);
            if (string.IsNullOrEmpty(qrText))
            {
                StatusMessage = "No QR code found in the image.";
                IsError = true;
                return;
            }

            var account = AccountService.ParseOtpAuthUrl(qrText);
            if (account == null)
            {
                StatusMessage = "Invalid QR code format. Expected otpauth:// URL.";
                IsError = true;
                return;
            }

            Name = account.Name;
            Secret = account.Secret;
            Digits = account.Digits.ToString();
            Algorithm = account.Algorithm;
            FieldsEnabled = true;
            StatusMessage = "QR code scanned successfully!";
            IsError = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsError = true;
        }
    }

    [RelayCommand]
    private void AddAccount()
    {
        StatusMessage = string.Empty;

        string name = Name.Trim();
        string secret = Secret.Trim();
        string digitsStr = Digits.Trim();
        string algorithm = Algorithm.Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Please enter an account name.";
            IsError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            StatusMessage = "Please enter a secret key.";
            IsError = true;
            return;
        }

        if (!int.TryParse(digitsStr, out int digits) || digits < 6 || digits > 8)
        {
            StatusMessage = "Digits must be between 6 and 8.";
            IsError = true;
            return;
        }

        if (algorithm != "SHA1" && algorithm != "SHA256" && algorithm != "SHA512" && algorithm != "MD5")
        {
            StatusMessage = "Algorithm must be SHA1, SHA256, SHA512, or MD5.";
            IsError = true;
            return;
        }

        try
        {
            var accounts = AccountService.LoadAccounts();
            accounts[name] = new Account
            {
                Secret = secret,
                Digits = digits,
                Algorithm = algorithm
            };
            AccountService.SaveAccounts(accounts);

            StatusMessage = $"Account '{name}' added!";
            IsError = false;
            AccountAddedSuccessfully?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
            IsError = true;
        }
    }
}
