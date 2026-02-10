using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing.ImageSharp;

namespace AuthenticatorTray.Services;

public static class QrCodeService
{
    public static string? DecodeQrCodeFromImage(string imagePath)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"Image file not found: {imagePath}");

        try
        {
            using var image = Image.Load<Rgba32>(imagePath);

            var reader = new BarcodeReader<Rgba32>
            {
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE }
                }
            };

            var result = reader.Decode(image);
            return result?.Text;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to decode QR code: {ex.Message}", ex);
        }
    }
}
