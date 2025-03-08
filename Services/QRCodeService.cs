using SkiaSharp;
using ZXing.SkiaSharp;

public class QRCodeService
{
    private readonly string _qrCodeDirectory = "wwwroot/qrcodes/";

    public QRCodeService()
    {
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

        string qrData = $"UserID: {userId}\nFull Name: {fullName}\nEmail: {email}";

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

        using (var qrCodeImage = qrCodeGenerator.Write(qrData))
        using (var image = SKImage.FromBitmap(qrCodeImage))
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        using (var stream = File.OpenWrite(filePath))
        {
            data.SaveTo(stream);
        }

        return relativePath;
    }
}
