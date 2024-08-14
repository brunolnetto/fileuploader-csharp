using Microsoft.AspNetCore.Mvc;
using UploaderMVP.Models;
using UploaderMVP.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UploaderMVP.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly ILogger<FileUploadController> _logger;
        private readonly IFileUploader _fileUploader;
        private const string Directory = "/uploads";


        public FileUploadController()
        {
        }

        public FileUploadController(IFileUploader fileUploader, ILogger<FileUploadController> logger)
        {
            _fileUploader = fileUploader;
            _logger = logger;
        }

        private static ILogger<FileUploadController> CreateDefaultLogger()
        {
            // Note: In a real application, you might want to use a logging framework configured in your DI container.
            return LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<FileUploadController>();
        }

        public IActionResult Upload()
        {
            var model = new FileUploadViewModel
            {
                Files = Enumerable.Empty<IFormFile>(), // Initialize empty file collection
            }; 

            ViewData["Title"] = "File Upload";
            return View(model);
        }

        public async Task<IActionResult> UploadPost(FileUploadViewModel model, CancellationToken cancellationToken)
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