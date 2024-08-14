namespace UploaderMVP.Services
{
    public static class RetryHelper
    {
        public static async Task ExecuteWithRetryAsync(Func<Task> action, int maxRetryAttempts, ILogger logger, CancellationToken cancellationToken)
        {
            int retryAttempts = 0;
            while (retryAttempts < maxRetryAttempts)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    retryAttempts++;
                    if (retryAttempts >= maxRetryAttempts)
                    {
                        logger.LogError(ex, "Max retry attempts reached. Operation failed.");
                        throw;
                    }

                    logger.LogWarning(ex, "Operation failed, retrying...");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
    }

    public interface IFileUploader
    {
        Task UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default);
    }

    public interface IFileService
    {
        Task UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    }

    public abstract class BaseUploader : IFileUploader
    {
        protected readonly IFileService _fileService;
        protected readonly ILogger<BaseUploader> _logger;

        public BaseUploader(IFileService fileService, ILogger<BaseUploader> logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        public abstract Task UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default);

        protected async Task UploadFileWithLoggingAsync(IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Starting upload of file: {file.FileName}");
                await _fileService.UploadFileAsync(file, cancellationToken);
                _logger.LogInformation($"Successfully uploaded file: {file.FileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file: {file.FileName}");
                throw;
            }
        }
    }

    public class FileService : IFileService
    {
        private readonly string _uploadsFolder;

        public FileService(string uploadsFolder)
        {
            _uploadsFolder = uploadsFolder;
            Directory.CreateDirectory(_uploadsFolder);
        }

        public async Task UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            var filePath = Path.Combine(_uploadsFolder, file.FileName);
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await file.CopyToAsync(stream, cancellationToken);
        }
    }

    public class SerializedUploader : BaseUploader
    {
        public SerializedUploader(IFileService fileService, ILogger<SerializedUploader> logger)
            : base(fileService, logger) { }

        public override async Task UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default)
        {
            foreach (var file in files)
            {
                await UploadFileWithLoggingAsync(file, cancellationToken);
            }
        }
    }

    public class ParallelizedUploader : BaseUploader
    {
        public ParallelizedUploader(IFileService fileService, ILogger<ParallelizedUploader> logger)
            : base(fileService, logger) { }

        public override async Task UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default)
        {
            var tasks = files.Select(file => UploadWithRetryAsync(file, cancellationToken));
            await Task.WhenAll(tasks);
        }

        private async Task UploadWithRetryAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var retry_handler = () => UploadFileWithLoggingAsync(file, cancellationToken);
            await RetryHelper.ExecuteWithRetryAsync(retry_handler, maxRetryAttempts: 3, _logger, cancellationToken);
        }
    }

    public class AsynchronousUploader : BaseUploader
    {
        public AsynchronousUploader(IFileService fileService, ILogger<AsynchronousUploader> logger)
            : base(fileService, logger) { }

        public override async Task UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default)
        {
            var tasks = files.Select(file => UploadFileWithLoggingAsync(file, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }

}
