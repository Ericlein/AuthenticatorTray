namespace AuthenticatorTray
{
    public class AccountData
    {
        public Dictionary<string, Account> Accounts { get; } = new Dictionary<string, Account>();
        public List<string> Folders { get; } = new List<string>();
        public List<string> CollapsedFolders { get; } = new List<string>();
        public bool ReadOnly { get; set; }
        public string? FindFolder(string name) => Folders.Find(f => string.Equals(f, name.Trim(), StringComparison.OrdinalIgnoreCase));
        // Reuses an existing folder when the name only differs in case
        public string AddFolder(string name)
        {
            string? existing = FindFolder(name);
            if (existing != null) return existing;
            Folders.Add(name.Trim());
            return name.Trim();
        }
        public void RenameFolder(string oldName, string newName)
        {
            int index = Folders.IndexOf(oldName);
            if (index < 0) return;
            Folders[index] = newName;
            foreach (var account in Accounts.Values)
            {
                if (account.Folder == oldName) account.Folder = newName;
            }
            int collapsedIndex = CollapsedFolders.IndexOf(oldName);
            if (collapsedIndex >= 0) CollapsedFolders[collapsedIndex] = newName;
        }
        public void DeleteFolder(string name)
        {
            Folders.Remove(name);
            CollapsedFolders.Remove(name);
            foreach (var account in Accounts.Values)
            {
                if (account.Folder == name) account.Folder = null;
            }
        }
    }
}
