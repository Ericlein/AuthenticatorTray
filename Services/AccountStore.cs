using System.Reflection;
using System.Text.Json;
namespace AuthenticatorTray
{
    static class AccountStore
    {
        static string GetAccountsJsonPath()
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDirectory, "accounts.json");
        }
        public static AccountData LoadData()
        {
            string accountsPath = GetAccountsJsonPath();
            bool fileUnreadable = false;
            if (File.Exists(accountsPath))
            {
                try
                {
                    return ParseAccounts(File.ReadAllText(accountsPath));
                }
                catch (Exception ex)
                {
                    fileUnreadable = true;
                    MessageBox.Show($"Error loading accounts from file: {ex.Message}\nTrying embedded resource...",
                        "Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("AuthenticatorTray.accounts.json"))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var data = ParseAccounts(reader.ReadToEnd());
                            data.ReadOnly = fileUnreadable;
                            return data;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading accounts: {ex.Message}\nUsing empty accounts.",
                    "Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return new AccountData { ReadOnly = fileUnreadable };
        }
        static AccountData ParseAccounts(string json)
        {
            var accountsData = JsonSerializer.Deserialize<AccountsRoot>(json);
            var data = new AccountData();
            foreach (string folder in accountsData?.Folders ?? new List<string>())
            {
                if (!string.IsNullOrWhiteSpace(folder)) data.AddFolder(folder);
            }
            foreach (var accountJson in accountsData?.Accounts ?? new List<AccountJson>())
            {
                data.Accounts[accountJson.Name] = new Account
                {
                    Secret = accountJson.Secret,
                    Digits = accountJson.Digits,
                    Algorithm = accountJson.Algorithm,
                    Folder = string.IsNullOrWhiteSpace(accountJson.Folder) ? null : data.AddFolder(accountJson.Folder)
                };
            }
            foreach (string collapsed in accountsData?.CollapsedFolders ?? new List<string>())
            {
                string? folder = data.FindFolder(collapsed ?? "");
                if (folder != null && !data.CollapsedFolders.Contains(folder)) data.CollapsedFolders.Add(folder);
            }
            return data;
        }
        public static bool SaveData(AccountData data)
        {
            if (data.ReadOnly)
            {
                MessageBox.Show("accounts.json could not be read, so it was not overwritten.\nFix the file and restart the app.",
                    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            try
            {
                string accountsPath = GetAccountsJsonPath();
                var accountsRoot = new AccountsRoot
                {
                    Accounts = data.Accounts.Select(kvp => new AccountJson
                    {
                        Name = kvp.Key,
                        Secret = kvp.Value.Secret,
                        Digits = kvp.Value.Digits,
                        Algorithm = kvp.Value.Algorithm,
                        Folder = kvp.Value.Folder
                    }).ToList(),
                    Folders = data.Folders.ToList(),
                    CollapsedFolders = data.CollapsedFolders.Where(data.Folders.Contains).ToList()
                };
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(accountsRoot, options);
                // Write then swap so a crash can't leave a half-written file
                string tempPath = accountsPath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, accountsPath, true);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving accounts: {ex.Message}",
                    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
