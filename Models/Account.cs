using System.Text.Json.Serialization;

namespace AuthenticatorTray.Models;

public class Account
{
    public string Secret { get; set; } = string.Empty;
    public int Digits { get; set; } = 6;
    public string Algorithm { get; set; } = "SHA1";
}

public class AccountsRoot
{
    [JsonPropertyName("accounts")]
    public List<AccountJson> Accounts { get; set; } = new List<AccountJson>();
}

public class AccountJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("secret")]
    public string Secret { get; set; } = string.Empty;

    [JsonPropertyName("digits")]
    public int Digits { get; set; } = 6;

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = "SHA1";
}
