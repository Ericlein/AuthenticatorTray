using ZXing;
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
                // Load the image
                using (var originalBitmap = new Bitmap(imagePath))
                {
                    // Ensure we have valid dimensions
                    if (originalBitmap.Width <= 0 || originalBitmap.Height <= 0)
                    {
                        throw new Exception($"Invalid image dimensions: {originalBitmap.Width}x{originalBitmap.Height}");
                    }
                    // Try multiple scales if the image is small
                    List<int> scalesToTry = new List<int>();
                    if (originalBitmap.Width < 300 || originalBitmap.Height < 300)
                    {
                        scalesToTry.AddRange(new[] { 3, 2, 1 }); // Try 3x, 2x, then original
                    }
                    else
                    {
                        scalesToTry.Add(1); // Just try original size
                    }
                    foreach (int scale in scalesToTry)
                    {
                        int width = originalBitmap.Width * scale;
                        int height = originalBitmap.Height * scale;
                        // Convert to RGB24 format for consistent processing
                        using (var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                        {
                            using (var graphics = Graphics.FromImage(bitmap))
                            {
                                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                                graphics.DrawImage(originalBitmap, 0, 0, width, height);
                            }
                        var reader = new BarcodeReaderGeneric();
                        var options = new ZXing.Common.DecodingOptions
                        {
                            TryHarder = true,
                            TryInverted = true,
                            PossibleFormats = new List<ZXing.BarcodeFormat>
                            {
                                ZXing.BarcodeFormat.QR_CODE
                            }
                        };
                        reader.Options = options;
                        // Method 1: Try with RGB24 format
                        var bitmapData = bitmap.LockBits(
                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly,
                            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        try
                        {
                            int stride = Math.Abs(bitmapData.Stride);
                            int bytes = stride * bitmap.Height;
                            byte[] rgbValues = new byte[bytes];
                            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, rgbValues, 0, bytes);
                            var luminanceSource = new ZXing.RGBLuminanceSource(
                                rgbValues,
                                bitmap.Width,
                                bitmap.Height,
                                ZXing.RGBLuminanceSource.BitmapFormat.RGB24);
                            var result = reader.Decode(luminanceSource);
                            if (result != null)
                            {
                                return result.Text;
                            }
                            // Try inverted
                            var invertedSource = new ZXing.InvertedLuminanceSource(luminanceSource);
                            result = reader.Decode(invertedSource);
                            if (result != null)
                            {
                                return result.Text;
                            }
                        }
                        finally
                        {
                            bitmap.UnlockBits(bitmapData);
                        }
                        // Method 2: Try with grayscale conversion
                        // Re-lock bits to get RGB data for grayscale conversion
                        var bitmapData2 = bitmap.LockBits(
                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly,
                            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        try
                        {
                            int stride2 = Math.Abs(bitmapData2.Stride);
                            int bytes2 = stride2 * bitmap.Height;
                            byte[] rgbValues2 = new byte[bytes2];
                            System.Runtime.InteropServices.Marshal.Copy(bitmapData2.Scan0, rgbValues2, 0, bytes2);
                            // Convert RGB24 to grayscale manually
                            int grayWidth = bitmap.Width;
                            int grayHeight = bitmap.Height;
                            byte[] grayValues = new byte[grayWidth * grayHeight];
                            // Convert RGB to grayscale using luminance formula
                            for (int y = 0; y < grayHeight; y++)
                            {
                                for (int x = 0; x < grayWidth; x++)
                                {
                                    int rgbIndex = (y * stride2) + (x * 3);
                                    if (rgbIndex + 2 < rgbValues2.Length)
                                    {
                                        byte r = rgbValues2[rgbIndex + 2];     // BGR order
                                        byte g = rgbValues2[rgbIndex + 1];
                                        byte b = rgbValues2[rgbIndex];
                                        // Luminance formula: 0.299*R + 0.587*G + 0.114*B
                                        byte gray = (byte)((r * 77 + g * 150 + b * 29) >> 8);
                                        grayValues[y * grayWidth + x] = gray;
                                    }
                                }
                            }
                            var grayLuminanceSource = new ZXing.RGBLuminanceSource(
                                grayValues,
                                grayWidth,
                                grayHeight,
                                ZXing.RGBLuminanceSource.BitmapFormat.Gray8);
                            var grayResult = reader.Decode(grayLuminanceSource);
                            if (grayResult != null)
                            {
                                return grayResult.Text;
                            }
                            // Try inverted grayscale
                            var invertedGraySource = new ZXing.InvertedLuminanceSource(grayLuminanceSource);
                            grayResult = reader.Decode(invertedGraySource);
                            if (grayResult != null)
                            {
                                return grayResult.Text;
                            }
                        }
                        finally
                        {
                            bitmap.UnlockBits(bitmapData2);
                        }
                    }
                    } // End of foreach scale
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to decode QR code: {ex.Message}", ex);
            }
            return null;
        }
        public static AccountJson? ParseOtpAuthUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return null;
                }
                if (!url.StartsWith("otpauth://"))
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
                if (label.Contains(":"))
                {
                    var parts = label.Split(new[] { ':' }, 2);
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
                string algorithm = queryParams.TryGetValue("algorithm", out string? algValue) ? algValue.ToUpper() : "SHA1";
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
