namespace UploaderMVP.Models
{
    public class FileUploadSettings
    {
        public IEnumerable<string> AllowedFileTypes { get; set; } = Enumerable.Empty<string>();
        public long MaxFileSizeInBytes { get; set; }
    }
}
