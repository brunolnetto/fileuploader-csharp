using System.Xml.Linq;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Fallback;
using Microsoft.Extensions.Logging;

namespace UploaderMVP.Services
{
    public class UploadOptions
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public int MaxConcurrency { get; set; } = 5;
    }
    public class RetryHelper
    {
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ILogger<RetryHelper> _logger;

        public RetryHelper(UploadOptions options, ILogger<RetryHelper> logger)
        {
            _logger = logger;

            // Define a retry policy: retry up to 3 times with exponential backoff
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    options.MaxRetryAttempts,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, $"Retry {retryCount} encountered an error. Waiting {timeSpan} before next retry.");
                    });
        }

        public async Task ExecuteWithRetryAsync(Func<Task> action, CancellationToken cancellationToken)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await action();
            });
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
        protected readonly RetryHelper _retryHelper;

        public BaseUploader(IFileService fileService, ILogger<BaseUploader> logger, RetryHelper retryHelper)
        {
            _fileService = fileService;
            _logger = logger;
            _retryHelper = retryHelper;
        }

        public abstract Task UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default);

        protected async Task UploadFileWithRetryAsync(IFormFile file, CancellationToken cancellationToken)
        {
            await _retryHelper.ExecuteWithRetryAsync(() => UploadFileWithLoggingAsync(file, cancellationToken), cancellationToken);
        }

        protected async Task UploadFileWithLoggingAsync(IFormFile file, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        public SerializedUploader(IFileService fileService, ILogger<SerializedUploader> logger, RetryHelper retryHelper)
            : base(fileService, logger, retryHelper) { }

        public override async Task UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default)
        {
            foreach (var file in files)
            {
                await UploadFileWithRetryAsync(file, cancellationToken);
            }
        }
    }

    public class ParallelizedUploader : BaseUploader
    {
        private readonly SemaphoreSlim _semaphore;

        public ParallelizedUploader(IFileService fileService, ILogger<ParallelizedUploader> logger, UploadOptions options, RetryHelper retryHelper)
            : base(fileService, logger, retryHelper)
        {
            _semaphore = new SemaphoreSlim(options.MaxConcurrency);
        }

        private async Task UploadWithRetryAsync(IFormFile file, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await _retryHelper.ExecuteWithRetryAsync(() => UploadFileWithLoggingAsync(file, cancellationToken), cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default)
        {
            var uploadTasks = files.Select(file => UploadWithRetryAsync(file, cancellationToken));
            await Task.WhenAll(uploadTasks);

        }
    }

    public class AsynchronousUploader : BaseUploader
    {
        public AsynchronousUploader(IFileService fileService, ILogger<AsynchronousUploader> logger, RetryHelper retryHelper)
            : base(fileService, logger, retryHelper) { }

        public override async Task UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>();

            foreach (var file in files)
            {
                tasks.Add(UploadFileWithRetryAsync(file, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }

}
