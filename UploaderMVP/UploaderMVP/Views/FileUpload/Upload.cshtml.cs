using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UploaderMVP.Models;
using UploaderMVP.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UploaderMVP.Views.FileUpload
{
    public class FileUploadModel : PageModel
    {
        private readonly IFileUploader _serializedUploader;
        private readonly IFileUploader _parallelizedUploader;
        private readonly IFileUploader _asynchronousUploader;

        public FileUploadModel(
            IFileUploader serializedUploader,
            IFileUploader parallelizedUploader,
            IFileUploader asynchronousUploader)
        {
            _serializedUploader = serializedUploader;
            _parallelizedUploader = parallelizedUploader;
            _asynchronousUploader = asynchronousUploader;
        }

        [BindProperty]
        public FileUploadViewModel UploadViewModel { get; set; }

        public void OnGet()
        {
            UploadViewModel = new FileUploadViewModel
            {
                Files = Enumerable.Empty<IFormFile>(),
                Metadata = new UploadMetadata()
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Please correct the errors and try again.");
                return Page();
            }

            if (UploadViewModel.Files == null || !UploadViewModel.Files.Any())
            {
                ModelState.AddModelError(string.Empty, "Please select files to upload.");
                return Page();
            }

            IFileUploader uploader = UploadViewModel.UploadMethod switch
            {
                "serial" => _serializedUploader,
                "parallel" => _parallelizedUploader,
                "asynchronous" => _asynchronousUploader,
                _ => _serializedUploader,
            };

            await uploader.UploadFilesAsync(UploadViewModel.Files);

            UploadViewModel.Metadata.UploadResult = "Files uploaded successfully.";
            return Page();
        }
    }
}
