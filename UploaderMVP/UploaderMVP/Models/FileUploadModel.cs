using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace UploaderMVP.Models
{

    public class UploadMetadata
    {
        public string UploadResult { get; set; } = string.Empty;
    }

    public class FileUploadModel
    {
        /// <summary>
        /// The file being uploaded.
        /// </summary>
        [Required(ErrorMessage = "Please select a file to upload.")]
        public IFormFile File { get; set; }

        /// <summary>
        /// An optional message or description associated with the file upload.
        /// </summary>
        public string? Message { get; set; }
    }

    public class FileUploadViewModel
    {
        /// <summary>
        /// A collection of files being uploaded.
        /// </summary>
        [Required(ErrorMessage = "Please select at least one file to upload.")]
        public IEnumerable<IFormFile> Files { get; set; } = Enumerable.Empty<IFormFile>();

        /// <summary>
        /// Specifies the upload method to be used (e.g., Serialized, Parallelized, Asynchronous).
        /// </summary>
        [Required(ErrorMessage = "Please select an upload method.")]
        public string UploadMethod { get; set; } = string.Empty;

        /// <summary>
        /// Optional metadata or additional information associated with the upload.
        /// </summary>
        public UploadMetadata Metadata { get; set; } = new UploadMetadata();
    }
}
