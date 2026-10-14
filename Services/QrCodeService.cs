using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;
namespace AuthenticatorTray
{
    static class QrCodeService
    {
        public static string? DecodeQrCodeFromImage(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException($"Image file not found: {imagePath}");
            }
            try
            {
                using var originalBitmap = new Bitmap(imagePath);
                var reader = new BarcodeReaderGeneric
                {
                    Options = new DecodingOptions
                    {
                        TryHarder = true,
                        TryInverted = true,
                        PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
                    }
                };
                // Small images decode better when upscaled first
                int[] scales = originalBitmap.Width < 300 || originalBitmap.Height < 300 ? new[] { 3, 2, 1 } : new[] { 1 };
                foreach (int scale in scales)
                {
                    int width = originalBitmap.Width * scale;
                    int height = originalBitmap.Height * scale;
                    using var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                        graphics.PixelOffsetMode = PixelOffsetMode.Half;
                        graphics.DrawImage(originalBitmap, 0, 0, width, height);
                    }
                    var result = reader.Decode(new RGBLuminanceSource(ToGrayscale(bitmap), width, height, RGBLuminanceSource.BitmapFormat.Gray8));
                    if (result != null)
                    {
                        return result.Text;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to decode QR code: {ex.Message}", ex);
            }
            return null;
        }
        static byte[] ToGrayscale(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = Math.Abs(bitmapData.Stride);
                byte[] bgr = new byte[stride * height];
                Marshal.Copy(bitmapData.Scan0, bgr, 0, bgr.Length);
                byte[] gray = new byte[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int i = y * stride + x * 3;
                        gray[y * width + x] = (byte)((bgr[i + 2] * 77 + bgr[i + 1] * 150 + bgr[i] * 29) >> 8);
                    }
                }
                return gray;
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
        public static AccountJson? ParseOtpAuthUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return null;
                }
                if (!url.StartsWith("otpauth://", StringComparison.Ordinal))
                {
                    return null;
                }
                var uri = new Uri(url);
                if (uri.Scheme != "otpauth")
                {
                    return null;
                }
                // Extract label (path without leading /)
                string label = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
                // Parse query parameters
                var queryParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(uri.Query))
                {
                    var query = uri.Query.TrimStart('?');
                    foreach (var param in query.Split('&'))
                    {
                        var parts = param.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            queryParams[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                        }
                    }
                }
                // Get secret (required)
                if (!queryParams.TryGetValue("secret", out string? secret) || string.IsNullOrEmpty(secret))
                {
                    return null;
                }
                // Get issuer and account name
                string? issuer = queryParams.TryGetValue("issuer", out string? issuerValue) ? issuerValue : null;
                string accountName = label;
                // Parse label format: "Issuer:AccountName" or just "AccountName"
                if (label.Contains(':'))
                {
                    var parts = label.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        if (string.IsNullOrEmpty(issuer))
                        {
                            issuer = parts[0];
                        }
                        accountName = parts[1];
                    }
                }
                string displayName;
                if (!string.IsNullOrEmpty(issuer) && !string.IsNullOrEmpty(accountName))
                {
                    displayName = $"{issuer} ({accountName})";
                }
                else if (!string.IsNullOrEmpty(issuer))
                {
                    displayName = issuer;
                }
                else if (!string.IsNullOrEmpty(accountName))
                {
                    displayName = accountName;
                }
                else
                {
                    displayName = "Unknown";
                }
                string algorithm = queryParams.TryGetValue("algorithm", out string? algValue) ? algValue.ToUpperInvariant() : "SHA1";
                if (algorithm != "SHA1" && algorithm != "SHA256" && algorithm != "SHA512" && algorithm != "MD5")
                {
                    algorithm = "SHA1";
                }
                int digits = 6;
                if (queryParams.TryGetValue("digits", out string? digitsValue))
                {
                    if (int.TryParse(digitsValue, out int parsedDigits) && (parsedDigits == 6 || parsedDigits == 7 || parsedDigits == 8))
                    {
                        digits = parsedDigits;
                    }
                }
                return new AccountJson
                {
                    Name = displayName,
                    Secret = secret,
                    Digits = digits,
                    Algorithm = algorithm
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse otpauth URL: {ex.Message}", ex);
            }
        }
    }
}
