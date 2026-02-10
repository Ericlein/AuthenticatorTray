using OtpNet;

namespace AuthenticatorTray.Services;

public static class TotpService
{
    private const int TotpPeriod = 30;

    public static string GenerateCode(string secret, int digits = 6)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret), step: TotpPeriod, totpSize: digits);
        return totp.ComputeTotp();
    }

    public static string FormatCode(string code)
    {
        if (code.Length == 6)
            return $"{code[..3]} {code[3..]}";
        if (code.Length == 8)
            return $"{code[..4]} {code[4..]}";
        return code;
    }

    public static int GetRemainingSeconds()
    {
        long elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % TotpPeriod;
        return TotpPeriod - (int)elapsed;
    }
}
