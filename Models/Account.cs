namespace AuthenticatorTray
{
    public class Account
    {
        public string Secret { get; set; } = string.Empty;
        public int Digits { get; set; } = 6;
        public string Algorithm { get; set; } = "SHA1";
        public string? Folder { get; set; }
    }
}
