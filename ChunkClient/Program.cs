using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MassTransit;
using ChunkClient.Messages.Requests;
using ChunkClient.Messages.Responses;



namespace ChunkClient;

class Program
{
    private static List<FileInfo> _fileList = new List<FileInfo>();

    static async Task Main(string[] args)
    {
        // Create host builder
        var host = CreateHostBuilder(args).Build();

        // Run the application
        await RunApplicationAsync(host.Services);
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Add MassTransit for publishing messages
                services.AddMassTransit(x =>
                {
                    // Add consumer for responses
                    x.AddConsumer<FileProcessingResponseConsumer>();
                    x.AddConsumer<FileListMessageConsumer>();

                    // Configure RabbitMQ
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host(hostContext.Configuration["RabbitMQ:Host"] ?? "rabbitmq", "/", h =>
                        {
                            h.Username(hostContext.Configuration["RabbitMQ:Username"] ?? "admin");
                            h.Password(hostContext.Configuration["RabbitMQ:Password"] ?? "admin123");
                        });

                        // Configure endpoints
                        cfg.ConfigureEndpoints(context);
                    });
                });

                // Add Logging
                services.AddLogging();
            });

    static async Task RunApplicationAsync(IServiceProvider serviceProvider)
    {
        var busControl = serviceProvider.GetRequiredService<IBusControl>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // Start the bus
        await busControl.StartAsync();

        logger.LogInformation("Welcome to Chunk Client!");
        logger.LogInformation("This application sends chunking requests to the Chunk Service via RabbitMQ.");

        while (true)
        {
            try
            {
                Console.WriteLine("\n=== Chunk Client Menu ===");
                Console.WriteLine("1. Send chunk file request");
                Console.WriteLine("2. Send reconstruct file request");
                Console.WriteLine("3. Send list files request");
                Console.WriteLine("4. Send get file info request");
                Console.WriteLine("5. Send delete file request");
                Console.WriteLine("6. Show cached file list");
                Console.WriteLine("7. Exit");
                Console.Write("\nSelect an option (1-7): ");

                var choice = Console.ReadLine();
                
                if (string.IsNullOrEmpty(choice))
                {
                    Console.WriteLine("Please enter a valid option.");
                    continue;
                }

                    switch (choice)
                {
                    case "1":
                        await SendChunkFileRequestAsync(busControl, logger);
                        break;
                    case "2":
                        await SendReconstructFileRequestAsync(busControl, logger);
                        break;
                    case "3":
                        await SendListFilesRequestAsync(busControl, logger);
                        break;
                    case "4":
                        await SendGetFileInfoRequestAsync(busControl, logger);
                        break;
                    case "5":
                        await SendDeleteFileRequestAsync(busControl, logger);
                        break;
                    case "6":
                        ShowCachedFileList();
                        break;
                    case "7":
                        logger.LogInformation("Exiting application...");
                        await busControl.StopAsync();
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during operation");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    static void ShowCachedFileList()
    {
        if (_fileList.Count == 0)
        {
            Console.WriteLine("No files in cache. Please use option 3 to fetch files first.");
            return;
        }

        Console.WriteLine($"\n=== Cached File List ({_fileList.Count} files) ===");
        for (int i = 0; i < _fileList.Count; i++)
        {
            var file = _fileList[i];
            Console.WriteLine($"{i + 1}. File ID: {file.Id}");
            Console.WriteLine($"   Name: {file.FileName}");
            Console.WriteLine($"   Size: {FormatFileSize(file.FileSize)}");
            Console.WriteLine($"   Chunks: {file.TotalChunks}");
            Console.WriteLine($"   Created: {file.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("   ---");
        }
    }

    static async Task SendChunkFileRequestAsync(IBusControl busControl, ILogger logger)
    {
        Console.Write("Enter the path to the file you want to chunk: ");
        var filePath = Console.ReadLine()?.Trim('"');

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("Invalid file path or file does not exist.");
            return;
        }

        try
        {
            var message = new ChunkFileMessage
            {
                FilePath = filePath,
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            await busControl.Publish(message);

            Console.WriteLine($"\nChunk file request sent successfully!");
            Console.WriteLine($"Request ID: {message.RequestId}");
            Console.WriteLine($"File Path: {message.FilePath}");
            Console.WriteLine($"Timestamp: {message.Timestamp:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send chunk file request");
            Console.WriteLine($"Failed to send request: {ex.Message}");
        }
    }

    static async Task SendReconstructFileRequestAsync(IBusControl busControl, ILogger logger)
    {
        if (_fileList.Count == 0)
        {
            Console.WriteLine("No files available. Please use option 3 to fetch files first.");
            return;
        }

        Console.WriteLine("\nAvailable files:");
        for (int i = 0; i < _fileList.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {_fileList[i].FileName} (ID: {_fileList[i].Id})");
        }

        Console.Write("Select file number: ");
        if (!int.TryParse(Console.ReadLine(), out int fileIndex) || fileIndex < 1 || fileIndex > _fileList.Count)
        {
            Console.WriteLine("Invalid file selection.");
            return;
        }

        var selectedFile = _fileList[fileIndex - 1];

        Console.Write("Enter the output filename (without path): ");
        var fileName = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(fileName))
        {
            Console.WriteLine("Invalid filename.");
            return;
        }

        try
        {
            var message = new ReconstructFileMessage
            {
                FileId = selectedFile.Id,
                OutputFileName = fileName,
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            await busControl.Publish(message);

            Console.WriteLine($"\nReconstruct file request sent successfully!");
            Console.WriteLine($"Request ID: {message.RequestId}");
            Console.WriteLine($"File ID: {message.FileId}");
            Console.WriteLine($"Output File: {message.OutputFileName}");
            Console.WriteLine($"Timestamp: {message.Timestamp:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send reconstruct file request");
            Console.WriteLine($"Failed to send request: {ex.Message}");
        }
    }

    static async Task SendListFilesRequestAsync(IBusControl busControl, ILogger logger)
    {
        try
        {
            // Clear previous file list before requesting new one
            _fileList.Clear();
            
            var message = new ListFilesMessage
            {
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            var endpoint = await busControl.GetSendEndpoint(new Uri("queue:ListFilesRequest"));
            await endpoint.Send(message);

            Console.WriteLine($"\nList files request sent successfully!");
            Console.WriteLine($"Request ID: {message.RequestId}");
            Console.WriteLine($"Timestamp: {message.Timestamp:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("Waiting for response... (check option 6 to see results)");
            Console.WriteLine("Note: Files will be received asynchronously. Use option 6 to view them.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send list files request");
            Console.WriteLine($"Failed to send request: {ex.Message}");
        }
    }

    static async Task SendGetFileInfoRequestAsync(IBusControl busControl, ILogger logger)
    {
        if (_fileList.Count == 0)
        {
            Console.WriteLine("No files available. Please use option 3 to fetch files first.");
            return;
        }

        Console.WriteLine("\nAvailable files:");
        for (int i = 0; i < _fileList.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {_fileList[i].FileName} (ID: {_fileList[i].Id})");
        }

        Console.Write("Select file number: ");
        if (!int.TryParse(Console.ReadLine(), out int fileIndex) || fileIndex < 1 || fileIndex > _fileList.Count)
        {
            Console.WriteLine("Invalid file selection.");
            return;
        }

        var selectedFile = _fileList[fileIndex - 1];

        try
        {
            var message = new GetFileInfoMessage
            {
                FileId = selectedFile.Id,
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            await busControl.Publish(message);

            Console.WriteLine($"\nGet file info request sent successfully!");
            Console.WriteLine($"Request ID: {message.RequestId}");
            Console.WriteLine($"File ID: {message.FileId}");
            Console.WriteLine($"Timestamp: {message.Timestamp:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send get file info request");
            Console.WriteLine($"Failed to send request: {ex.Message}");
        }
    }

    static async Task SendDeleteFileRequestAsync(IBusControl busControl, ILogger logger)
    {
        if (_fileList.Count == 0)
        {
            Console.WriteLine("No files available. Please use option 3 to fetch files first.");
            return;
        }

        Console.WriteLine("\nAvailable files:");
        for (int i = 0; i < _fileList.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {_fileList[i].FileName} (ID: {_fileList[i].Id})");
        }

        Console.Write("Select file number: ");
        if (!int.TryParse(Console.ReadLine(), out int fileIndex) || fileIndex < 1 || fileIndex > _fileList.Count)
        {
            Console.WriteLine("Invalid file selection.");
            return;
        }

        var selectedFile = _fileList[fileIndex - 1];

        Console.Write($"Are you sure you want to delete '{selectedFile.FileName}'? (y/N): ");
        var confirmation = Console.ReadLine()?.ToLower();

        if (confirmation != "y")
        {
            Console.WriteLine("Deletion cancelled.");
            return;
        }

        try
        {
            var message = new DeleteFileMessage
            {
                FileId = selectedFile.Id,
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            await busControl.Publish(message);

            Console.WriteLine($"\nDelete file request sent successfully!");
            Console.WriteLine($"Request ID: {message.RequestId}");
            Console.WriteLine($"File ID: {message.FileId}");
            Console.WriteLine($"Timestamp: {message.Timestamp:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send delete file request");
            Console.WriteLine($"Failed to send request: {ex.Message}");
        }
    }

    /// <summary>
    /// Dosya boyutunu uygun birimde formatlar (B, KB, MB, GB)
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        if (order == 0) // Bytes
        {
            return $"{len} {sizes[order]}";
        }
        else if (order == 1) // KB
        {
            return $"{len:F1} {sizes[order]}";
        }
        else // MB, GB, TB
        {
            return $"{len:F2} {sizes[order]}";
        }
    }

    // Method to update cached file list
    public static void UpdateFileList(List<FileInfo> files)
    {
        _fileList = files;
    }

    // Method to get current cached file list
    public static List<FileInfo> GetCurrentFileList()
    {
        return _fileList;
    }
}


// File info class for caching
public class FileInfo
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int TotalChunks { get; set; }
    public DateTime CreatedAt { get; set; }
}
