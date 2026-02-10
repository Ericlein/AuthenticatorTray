using System.Reflection;
using System.Text.Json;
using AuthenticatorTray.Models;

namespace AuthenticatorTray.Services;

public static class AccountService
{
    private static string GetAccountsJsonPath()
    {
        // Try next to executable first
        string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string localPath = Path.Combine(appDirectory, "accounts.json");
        if (File.Exists(localPath))
            return localPath;

        // Fall back to AppData for macOS/portable installs
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AuthenticatorTray");
        Directory.CreateDirectory(appDataDir);
        return Path.Combine(appDataDir, "accounts.json");
    }

    public static Dictionary<string, Account> LoadAccounts()
    {
        string accountsPath = GetAccountsJsonPath();
        if (File.Exists(accountsPath))
        {
            try
            {
                var json = File.ReadAllText(accountsPath);
                var accountsData = JsonSerializer.Deserialize<AccountsRoot>(json);
                return ParseAccountsRoot(accountsData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading accounts from file: {ex.Message}");
            }
        }

        // Try embedded resource as fallback
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("AuthenticatorTray.accounts.json");
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var accountsData = JsonSerializer.Deserialize<AccountsRoot>(json);
                return ParseAccountsRoot(accountsData);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading embedded accounts: {ex.Message}");
        }

        return new Dictionary<string, Account>();
    }

    public static void SaveAccounts(Dictionary<string, Account> accounts)
    {
        string accountsPath = GetAccountsJsonPath();
        var accountsList = new List<AccountJson>();
        foreach (var kvp in accounts)
        {
            accountsList.Add(new AccountJson
            {
                Name = kvp.Key,
                Secret = kvp.Value.Secret,
                Digits = kvp.Value.Digits,
                Algorithm = kvp.Value.Algorithm
            });
        }

        var accountsRoot = new AccountsRoot { Accounts = accountsList };
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(accountsRoot, options);
        File.WriteAllText(accountsPath, json);
    }

    public static AccountJson? ParseOtpAuthUrl(string url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith("otpauth://"))
            return null;

        try
        {
            var uri = new Uri(url);
            if (uri.Scheme != "otpauth")
                return null;

            string label = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

            var queryParams = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(uri.Query))
            {
                var query = uri.Query.TrimStart('?');
                foreach (var param in query.Split('&'))
                {
                    var parts = param.Split('=', 2);
                    if (parts.Length == 2)
                        queryParams[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                }
            }

            if (!queryParams.TryGetValue("secret", out string? secret) || string.IsNullOrEmpty(secret))
                return null;

            string? issuer = queryParams.TryGetValue("issuer", out string? issuerValue) ? issuerValue : null;
            string accountName = label;

            if (label.Contains(':'))
            {
                var parts = label.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    if (string.IsNullOrEmpty(issuer))
                        issuer = parts[0];
                    accountName = parts[1];
                }
            }

            string displayName;
            if (!string.IsNullOrEmpty(issuer) && !string.IsNullOrEmpty(accountName))
                displayName = $"{issuer} ({accountName})";
            else if (!string.IsNullOrEmpty(issuer))
                displayName = issuer;
            else if (!string.IsNullOrEmpty(accountName))
                displayName = accountName;
            else
                displayName = "Unknown";

            string algorithm = queryParams.TryGetValue("algorithm", out string? algValue) ? algValue.ToUpper() : "SHA1";
            if (algorithm != "SHA1" && algorithm != "SHA256" && algorithm != "SHA512" && algorithm != "MD5")
                algorithm = "SHA1";

            int digits = 6;
            if (queryParams.TryGetValue("digits", out string? digitsValue))
            {
                if (int.TryParse(digitsValue, out int parsedDigits) && parsedDigits >= 6 && parsedDigits <= 8)
                    digits = parsedDigits;
            }

            return new AccountJson
            {
                Name = displayName,
                Secret = secret,
                Digits = digits,
                Algorithm = algorithm
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse otpauth URL: {ex.Message}", ex);
        }
    }

    private static Dictionary<string, Account> ParseAccountsRoot(AccountsRoot? accountsData)
    {
        var accounts = new Dictionary<string, Account>();
        if (accountsData?.Accounts != null)
        {
            foreach (var accountJson in accountsData.Accounts)
            {
                accounts[accountJson.Name] = new Account
                {
                    Secret = accountJson.Secret,
                    Digits = accountJson.Digits,
                    Algorithm = accountJson.Algorithm
                };
            }
        }
        return accounts;
    }
}
