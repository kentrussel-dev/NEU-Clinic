using SkiaSharp;
using ZXing.SkiaSharp;
using Microsoft.Extensions.Configuration;

public class QRCodeService
{
    private readonly string _qrCodeDirectory = "wwwroot/qrcodes/";
    private readonly IConfiguration _configuration;

    public QRCodeService(IConfiguration configuration)
    {
        _configuration = configuration;

        if (!Directory.Exists(_qrCodeDirectory))
        {
            Directory.CreateDirectory(_qrCodeDirectory);
        }
    }

    public string GenerateQRCode(string userId, string fullName, string email)
    {
        string fileName = $"{userId}.png";
        string filePath = Path.Combine(_qrCodeDirectory, fileName);
        string relativePath = $"/qrcodes/{fileName}";

        // Get the base URL from configuration
        string baseUrl = _configuration["BaseUrl"];
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new InvalidOperationException("Base URL is not configured in appsettings.json.");
        }

        // Generate the profile URL for the user
        string profileUrl = $"{baseUrl}/profile/{userId}";

        if (File.Exists(filePath))
        {
            return relativePath;
        }

        var qrCodeGenerator = new ZXing.BarcodeWriter<SKBitmap>
        {
            Format = ZXing.BarcodeFormat.QR_CODE,
            Options = new ZXing.Common.EncodingOptions
            {
                Height = 256,
                Width = 256
            },
            Renderer = new ZXing.SkiaSharp.Rendering.SKBitmapRenderer()
        };

        // Use the profile URL as the QR code data
        using (var qrCodeImage = qrCodeGenerator.Write(profileUrl))
        using (var image = SKImage.FromBitmap(qrCodeImage))
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        using (var stream = File.OpenWrite(filePath))
        {
            data.SaveTo(stream);
        }

        return relativePath;
    }
}