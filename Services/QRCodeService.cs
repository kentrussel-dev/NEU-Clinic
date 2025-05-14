using SkiaSharp;

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

    // Existing method for user QR codes
    public string GenerateQRCode(string userId, string fullName, string email)
    {
        string fileName = $"{userId}.png";
        string filePath = Path.Combine(_qrCodeDirectory, fileName);
        string relativePath = $"/qrcodes/{fileName}";

        string profileUrl = $"profile/{userId}";

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

        using (var qrCodeImage = qrCodeGenerator.Write(profileUrl))
        using (var image = SKImage.FromBitmap(qrCodeImage))
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        using (var stream = File.OpenWrite(filePath))
        {
            data.SaveTo(stream);
        }

        return relativePath;
    }

    // New method for appointment QR codes
    public string GenerateAppointmentQRCode(int appointmentId)
    {
        string fileName = $"roomappointment_{appointmentId}.png";
        string filePath = Path.Combine(_qrCodeDirectory, fileName);
        string relativePath = $"/qrcodes/{fileName}";

        if (File.Exists(filePath))
        {
            return relativePath;
        }

        // Create QR code data with attendance endpoint
        string qrCodeData = $"appointment/{appointmentId}/checkin";

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

        using (var qrCodeImage = qrCodeGenerator.Write(qrCodeData))
        using (var image = SKImage.FromBitmap(qrCodeImage))
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        using (var stream = File.OpenWrite(filePath))
        {
            data.SaveTo(stream);
        }

        return relativePath;
    }
}