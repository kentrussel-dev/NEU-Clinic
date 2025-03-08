/*//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using SkiaSharp;
//using ZXing.SkiaSharp;
//using System.IO;

//namespace WebApp.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class QRScannerController : ControllerBase
//    {
//        [HttpPost("scan")]
//        public IActionResult ScanQRCode(IFormFile qrImage)
//        {
//            if (qrImage == null || qrImage.Length == 0)
//                return BadRequest("No image uploaded");

//            using (var stream = qrImage.OpenReadStream())
//            using (var bitmap = SKBitmap.Decode(stream))
//            {
//                var reader = new ZXing.SkiaSharp.BarcodeReader();
//                var result = reader.Decode(bitmap);

//                if (result == null)
//                    return BadRequest("QR Code could not be decoded.");

//                return Ok(new { decodedText = result.Text });
//            }
//        }
//    }
//}
*/