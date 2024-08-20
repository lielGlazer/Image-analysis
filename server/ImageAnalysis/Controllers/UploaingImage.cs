
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Security.Cryptography;
namespace ImageAnalysis.Controllers
{
    public class UploaingImage : Controller
    {
        [HttpPost]
        [Route("upload")]
        //פונקציה שטוענת את התמונה 
        public async Task<IActionResult> UploadImage(IFormFile file, [FromForm] string checksum)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (string.IsNullOrEmpty(checksum))
            {
                return BadRequest("No checksum provided.");
            }

            var filePath = Path.Combine("Uploads", file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

          
            using (var sha256 = SHA256.Create())
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var computedHash = sha256.ComputeHash(stream);
                var computedChecksum = BitConverter.ToString(computedHash).Replace("-", "").ToLower();

                if (computedChecksum != checksum.ToLower())
                {
                    return BadRequest("Checksum mismatch. The file may be corrupted.");
                }
            }

            var popularColors = GetPopularColors(filePath);

            return Ok(new { filePath, popularColors });
        }

        [HttpGet("uploads")]
        public IActionResult GetUploadedImages()
        {
            if (!Directory.Exists("Uploads"))
            {
                return NotFound("No images found.");
            }

            var files = Directory.GetFiles("Uploads")
                .Select(f => Path.GetFileName(f))
                .ToList();

            return Ok(files);
        }

        //פונקציה שמחשבת את הצבעים הפופלארים בתמונה
        private string[] GetPopularColors(string filePath)
        {
            //מילון של צבעים - המפתח הוא הצבע והערך הוא הכמות 
            var colorCounts = new Dictionary<string, int>();
            int totalPixels = 0;
            //קריאת קובץ התמונה
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fileStream))
            {
                reader.BaseStream.Seek(18, SeekOrigin.Begin); // מעבר לרוחב
                int imageWidth = reader.ReadInt32();
                int imageHeight = reader.ReadInt32();

                reader.BaseStream.Seek(28, SeekOrigin.Begin); 
                short bitsPerPixel = reader.ReadInt16();
                //בדיקה האם הקובץ מסוג BPM 32-24 
                if (bitsPerPixel != 24 && bitsPerPixel != 32)
                {
                    return new string[] { "Unsupported image format. Only 24-bit or 32-bit BMP images are supported." };
                }

                reader.BaseStream.Seek(54, SeekOrigin.Begin); 

                int rowPadding = (4 - ((imageWidth * (bitsPerPixel / 8)) % 4)) % 4;
                //מילוי המילון בצבעים והכמות שלהם 
               
                for (int y = imageHeight - 1; y >= 0; y--)
                {
                    for (int x = 0; x < imageWidth; x++)
                    {
                        byte blue = reader.ReadByte();
                        byte green = reader.ReadByte();
                        byte red = reader.ReadByte();
                        byte alpha = bitsPerPixel == 32 ? reader.ReadByte() : (byte)255; 

                       
                        Color color = Color.FromArgb(alpha, red, green, blue);

                        string colorHex = ColorToHex(color);

                        // עדכון ספירת צבעים במילון
                        if (colorCounts.ContainsKey(colorHex))
                        {
                            colorCounts[colorHex]++;
                        }
                        else
                        {
                            colorCounts[colorHex] = 1;
                        }
                        totalPixels++;
                    }

                    reader.BaseStream.Seek(rowPadding, SeekOrigin.Current);
                }
            }

            var popularColors = colorCounts.OrderByDescending(pair => pair.Value)
                                           .Take(5)
                                           .Select(pair => $"{pair.Key}: {((double)pair.Value / totalPixels * 100):F2}%")
                                           .ToArray();

            return popularColors;
        }
        
        private string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
