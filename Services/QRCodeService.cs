using SkiaSharp;
using SkiaSharp.QrCode;
using System;
using System.IO;
using static QRCoder.PayloadGenerator;
using WebApp.Models;

public class QRCodeService
{
    private readonly string _qrCodeDirectory = "wwwroot/qrcodes/";

    public QRCodeService()
    {
        // Ensure QR Code directory exists
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

        string qrData = 
            $"UserID: {userId}" +
            $"\nFull Name: {fullName}" +
            $"\nEmail: {email}";
        if (File.Exists(filePath))
        {
            return relativePath;
        }

        // Generate QR Code
        var qrCodeGenerator = new QRCodeGenerator();
        var qrCodeData = qrCodeGenerator.CreateQrCode(qrData, ECCLevel.Q);

        int qrSize = 256;
        var qrCodeImage = new SKBitmap(qrSize, qrSize);
        using (var canvas = new SKCanvas(qrCodeImage))
        {
            canvas.Clear(SKColors.White);

            var renderer = new QRCodeRenderer();
            renderer.Render(
                canvas,
                new SKRect(0, 0, qrSize, qrSize),
                qrCodeData,
                SKColors.Black,
                SKColors.White
            );
        }

        // Save QR Code as PNG
        using (var image = SKImage.FromBitmap(qrCodeImage))
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        using (var stream = File.OpenWrite(filePath))
        {
            data.SaveTo(stream);
        }

        return relativePath; // Return file path for DB storage
    }

}