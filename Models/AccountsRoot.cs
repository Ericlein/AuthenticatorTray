using System.Text.Json.Serialization;
namespace AuthenticatorTray
{
    public class AccountsRoot
    {
        [JsonPropertyName("accounts")]
        public List<AccountJson> Accounts { get; set; } = new List<AccountJson>();
        [JsonPropertyName("folders")]
        public List<string>? Folders { get; set; } = new List<string>();
        [JsonPropertyName("collapsedFolders")]
        public List<string>? CollapsedFolders { get; set; } = new List<string>();
    }
}
