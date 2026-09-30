using OtpNet;
namespace AuthenticatorTray
{
    static class TotpService
    {
        public static int SecondsRemaining() => 30 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 30);
        public static string ComputeCode(Account acc) => new Totp(Base32Encoding.ToBytes(acc.Secret), step: 30, totpSize: acc.Digits).ComputeTotp();
        public static string FormatCode(string code) => code.Length == 6 ? $"{code.Substring(0, 3)} {code.Substring(3, 3)}" : code;
    }
}
