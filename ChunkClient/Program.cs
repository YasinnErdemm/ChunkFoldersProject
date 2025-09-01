using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using ChunkClient.Messages.Requests;
using ChunkClient.Messages.Responses;



namespace ChunkClient;

class Program
{
    private static List<FileInfo> _fileList = new List<FileInfo>();

    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await RunApplicationAsync(host.Services);
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IConnectionFactory>(provider =>
                {
                    var host = hostContext.Configuration["RabbitMQ:Host"] ?? "localhost";
                    var username = hostContext.Configuration["RabbitMQ:Username"] ?? "admin";
                    var password = hostContext.Configuration["RabbitMQ:Password"] ?? "admin123";
                    
                    return new ConnectionFactory
                    {
                        HostName = host,
                        UserName = username,
                        Password = password
                    };
                });
                services.AddLogging();
            });

    static async Task RunApplicationAsync(IServiceProvider serviceProvider)
    {
        var connectionFactory = serviceProvider.GetRequiredService<IConnectionFactory>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();

        var fileListConsumer = new FileListMessageConsumer(logger, channel);
        var fileProcessingConsumer = new FileProcessingResponseConsumer(logger, channel);
        
        fileListConsumer.StartConsuming();
        fileProcessingConsumer.StartConsuming();

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
                        await SendChunkFileRequestAsync(channel, logger);
                        break;
                    case "2":
                        await SendReconstructFileRequestAsync(channel, logger);
                        break;
                    case "3":
                        await SendListFilesRequestAsync(channel, logger);
                        break;
                    case "4":
                        await SendGetFileInfoRequestAsync(channel, logger);
                        break;
                    case "5":
                        await SendDeleteFileRequestAsync(channel, logger);
                        break;
                    case "6":
                        ShowCachedFileList();
                        break;
                    case "7":
                        logger.LogInformation("Exiting application...");
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

    static async Task SendChunkFileRequestAsync(IModel channel, ILogger logger)
    {
        Console.Write("Enter the path(s) to the file(s) you want to chunk (separate multiple files with comma): ");
        var input = Console.ReadLine()?.Trim('"');

        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine("Invalid input.");
            return;
        }
        var filePaths = input.Split(',')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (filePaths.Count == 0)
        {
            Console.WriteLine("No valid file paths provided.");
            return;
        }
        var validPaths = new List<string>();
        var invalidPaths = new List<string>();

        foreach (var path in filePaths)
        {
            if (File.Exists(path))
            {
                validPaths.Add(path);
            }
            else
            {
                invalidPaths.Add(path);
            }
        }

        if (invalidPaths.Count > 0)
        {
            Console.WriteLine($"\n⚠️  Warning: {invalidPaths.Count} file(s) not found:");
            foreach (var invalidPath in invalidPaths)
            {
                Console.WriteLine($"   ❌ {invalidPath}");
            }
        }

        if (validPaths.Count == 0)
        {
            Console.WriteLine("No valid files to process.");
            return;
        }

        Console.WriteLine($"\n📁 Processing {validPaths.Count} file(s)...");

        var successCount = 0;
        var failCount = 0;

        try
        {
            // Declare queue once
            channel.QueueDeclare("ChunkFileRequest", durable: true, exclusive: false, autoDelete: false);

            var allFilePaths = string.Join(",", validPaths);
            var message = new ChunkFileMessage
            {
                FilePath = allFilePaths, 
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            var messageJson = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageJson);

            Console.WriteLine($"🔄 Sending SINGLE message with {validPaths.Count} files:");
            Console.WriteLine($"   📁 Files: {allFilePaths}");
            Console.WriteLine($"   🆔 RequestId: {message.RequestId}");
            Console.WriteLine($"   📦 Message size: {body.Length} bytes");

            channel.BasicPublish("", "ChunkFileRequest", null, body);

            Console.WriteLine($"✅ Single message sent successfully with {validPaths.Count} files!");
            successCount = 1;

            Console.WriteLine($"\n📊 Batch Summary:");
            Console.WriteLine($"   ✅ Successfully sent: {successCount} request(s)");
            if (failCount > 0)
            {
                Console.WriteLine($"   ❌ Failed to send: {failCount} request(s)");
            }
            Console.WriteLine($"   📁 Total files: {validPaths.Count}");
            Console.WriteLine($"   ⏳ Processing started... (check responses asynchronously)");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send chunk file requests");
            Console.WriteLine($"Failed to send requests: {ex.Message}");
        }
    }

    static async Task SendReconstructFileRequestAsync(IModel channel, ILogger logger)
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

            channel.QueueDeclare("ReconstructFileRequest", durable: true, exclusive: false, autoDelete: false);
            var messageJson = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageJson);
            channel.BasicPublish("", "ReconstructFileRequest", null, body);

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

    static async Task SendListFilesRequestAsync(IModel channel, ILogger logger)
    {
        try
        {
            _fileList.Clear();
            
            var message = new ListFilesMessage
            {
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };
            channel.QueueDeclare("ListFilesRequest", durable: true, exclusive: false, autoDelete: false);
            var messageJson = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageJson);
            channel.BasicPublish("", "ListFilesRequest", null, body);

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

    static async Task SendGetFileInfoRequestAsync(IModel channel, ILogger logger)
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
            channel.QueueDeclare("GetFileInfoRequest", durable: true, exclusive: false, autoDelete: false);
            var messageJson = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageJson);
            channel.BasicPublish("", "GetFileInfoRequest", null, body);

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

    static async Task SendDeleteFileRequestAsync(IModel channel, ILogger logger)
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

            channel.QueueDeclare("DeleteFileRequest", durable: true, exclusive: false, autoDelete: false);
            var messageJson = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageJson);
            channel.BasicPublish("", "DeleteFileRequest", null, body);

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
        
        if (order == 0) 
        {
            return $"{len} {sizes[order]}";
        }
        else if (order == 1) 
        {
            return $"{len:F1} {sizes[order]}";
        }
        else 
        {
            return $"{len:F2} {sizes[order]}";
        }
    }
    public static void UpdateFileList(List<FileInfo> files)
    {
        _fileList = files;
    }
    public static List<FileInfo> GetCurrentFileList()
    {
        return _fileList;
    }
}

public class FileInfo
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int TotalChunks { get; set; }
    public DateTime CreatedAt { get; set; }
}
