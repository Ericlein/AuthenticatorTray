using OtpNet;
namespace AuthenticatorTray
{
    static class TotpService
    {
        const int StepSeconds = 30;
        public static int SecondsRemaining() => StepSeconds - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % StepSeconds);
        public static string ComputeCode(Account acc) =>
            new Totp(Base32Encoding.ToBytes(NormalizeSecret(acc.Secret)), step: StepSeconds, mode: HashMode(acc.Algorithm), totpSize: acc.Digits).ComputeTotp();
        public static string FormatCode(string code) => code.Length == 6 ? $"{code[..3]} {code[3..]}" : code;
        // Authenticator apps often show secrets grouped with spaces or dashes, in lowercase
        static string NormalizeSecret(string secret) =>
            string.Concat(secret.Where(c => !char.IsWhiteSpace(c) && c != '-')).TrimEnd('=').ToUpperInvariant();
        // Otp.NET has no MD5 mode, so MD5 keeps the previous SHA1 behaviour
        static OtpHashMode HashMode(string? algorithm) => algorithm?.Trim().ToUpperInvariant() switch
        {
            "SHA256" => OtpHashMode.Sha256,
            "SHA512" => OtpHashMode.Sha512,
            _ => OtpHashMode.Sha1
        };
    }
}
