using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace Api.Modules.Barcodes.Controllers
{
    /// <summary>
    /// Controller for doing things with barcodes and QR codes.
    /// </summary>
    [Route("api/v3/[controller]")]
    [ApiController]
    public class BarcodesController : ControllerBase
    {
        /// <summary>
        /// Generate an image for an QR code.
        /// </summary>
        /// <param name="text">The text to create an QR code of.</param>
        /// <param name="size">Optional: The size (in pixels) of the image. Default is 500px.</param>
        /// <param name="downloadFileName">Optional: If this should be downloaded by the browser instead of just shown, you can enter a file name here.</param>
        /// <returns>The generated QR code image.</returns>
        [HttpGet]
        [Route("qr")]
        public IActionResult GetQrCode(string text, int size = 500, string downloadFileName = null)
        {
            if (size <= 0)
            {
                size = 500;
            }

            using var qrGenerator = new QRCodeGenerator();
            
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrCodeData);
            using var qrCodeImage = new Bitmap(qrCode.GetGraphic(20), new Size(size, size));

            // The FileResult will close the stream when it is finished using it.
            var outputStream = new MemoryStream();
            qrCodeImage.Save(outputStream, ImageFormat.Png);
            outputStream.Seek(0, SeekOrigin.Begin);
            
            return String.IsNullOrWhiteSpace(downloadFileName) ? File(outputStream, "image/png") : File(outputStream, "image/png", downloadFileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? downloadFileName : $"{downloadFileName}.png");
        }
    }
}
