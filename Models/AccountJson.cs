using System.Text.Json.Serialization;
namespace AuthenticatorTray
{
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
        [JsonPropertyName("folder")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Folder { get; set; }
    }
}
