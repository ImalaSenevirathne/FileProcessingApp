using Microsoft.AspNetCore.Mvc;

namespace FileProcessingApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly FileProcessingService _fileProcessingService;

        public FileUploadController(FileProcessingService fileProcessingService)
        {
            _fileProcessingService = fileProcessingService;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(157286400)]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Save to temp folder
            //var filePath = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}_{file.FileName}");
            var filePath = Path.Combine(Path.GetTempPath(), file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Enqueue the file for processing
            _fileProcessingService.EnqueueFile(filePath);
            
            return Ok(new { Message = "File uploaded successfully.", FilePath = filePath });
        }

        [HttpGet("status")]
        public IActionResult Status([FromQuery] string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return BadRequest("File path is required.");
            }

            if (_fileProcessingService._fileProcessingProgress.TryGetValue(filepath, out int progress))
            {
                return Ok(new { filepath, progress });
            }
            else
            {
                return NotFound(new { Message = "File not found or not being processed." });
            }
        }
    }
}
