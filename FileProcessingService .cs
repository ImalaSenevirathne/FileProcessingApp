
using System.Collections.Concurrent;

namespace FileProcessingApp
{
    public class FileProcessingService : BackgroundService
    {
        private readonly ILogger<FileProcessingService> _logger;

        //Thread safe collection of files to process
        private readonly ConcurrentQueue<string> _filesToProcess = new ConcurrentQueue<string>();

        //Thread safe dictionary to track file processing progress %
        public readonly ConcurrentDictionary<string, int> _fileProcessingProgress= new ConcurrentDictionary<string, int>();

        public FileProcessingService(ILogger<FileProcessingService> logger)
        {
            _logger = logger;
        }

        // Method to add files to the processing queue
        public void EnqueueFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogWarning("Attempted to enqueue an empty file path.");
                return;
            }

            _filesToProcess.Enqueue(filePath);
            _fileProcessingProgress[filePath] = 0; // Initialize progress to 0%

            _logger.LogInformation($"File {filePath} added to the processing queue.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File Processing Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_filesToProcess.TryDequeue(out string filePath))
                {
                    _logger.LogInformation($"Processing file: {filePath}");

                    try
                    {
                        //Process files with multithreading
                        await ProcessFilesAsync(filePath, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing file {filePath}");
                        _fileProcessingProgress[filePath] = -1; // Indicate an error occurred
                    }
                    finally
                    {
                        // Remove the file from progress tracking
                        _fileProcessingProgress.TryRemove(filePath, out _);
                        _logger.LogInformation($"Finished processing file: {filePath}");
                        try { File.Delete(filePath); } catch { /* Ignore errors on delete */ }
                    }
                }
                else
                {
                    // No files to process, wait for a while before checking again
                    await Task.Delay(1000, stoppingToken);
                }
            }

            _logger.LogInformation("File Processing Service is stopping.");
        }

        private async Task ProcessFilesAsync(string filePath, CancellationToken cancellationToken)
        {
            // Read all lines (assuming small files for demo; for larger files, stream line by line)
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

            int totalLines = lines.Length;
            int processedLines = 0;

            // Process lines in parallel
            Parallel.ForEach(lines, new ParallelOptions { CancellationToken = cancellationToken }, line =>
            {
                // Simulate processing each line
                Thread.Sleep(10); // Simulate some work

                // Increment processed lines count in a thread-safe manner
                Interlocked.Increment(ref processedLines);

                // Update progress percentage in a thread-safe manner
                _fileProcessingProgress[filePath] = (int)((processedLines / (double)totalLines) * 100);
            });

            _fileProcessingProgress[filePath] = 100; // Mark as complete
            _logger.LogInformation($"File {filePath} processed successfully. Progress: 100%");
        }
    }
}
