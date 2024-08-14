using Microsoft.AspNetCore.Mvc;
using UploaderMVP.Models;
using UploaderMVP.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UploaderMVP.Controllers
{
    [ApiController]
    [Route("upload")]
    public class FileUploadController : Controller
    {
        private readonly IFileUploader _fileUploader;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(IFileUploader fileUploader, ILogger<FileUploadController> logger)
        {
            _fileUploader = fileUploader;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            var model = new FileUploadViewModel();
            ViewData["Title"] = "File Upload";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(FileUploadViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state.");
                ViewData["Message"] = "Invalid data submitted.";
                return View(model);
            }

            if (model.Files != null && model.Files.Any())
            {
                try
                {
                    _logger.LogInformation("Starting file upload process.");
                    await _fileUploader.UploadFilesAsync(model.Files, cancellationToken);
                    _logger.LogInformation("File upload process completed.");
                    ViewData["Message"] = "Files uploaded successfully!";
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("File upload process was canceled.");
                    ViewData["Message"] = "File upload was canceled.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during file upload.");
                    ViewData["Message"] = "An error occurred while uploading files.";
                }
            }
            else
            {
                _logger.LogWarning("No files selected for upload.");
                ViewData["Message"] = "No files selected.";
            }

            return View(model);
        }
    }
}