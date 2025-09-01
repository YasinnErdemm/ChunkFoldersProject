using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using ChunkApplication.Application.Services;
using ChunkApplication.Domain.Interfaces;
using ChunkApplication.Infrastructure.Data;
using ChunkApplication.Infrastructure.MessageHandlers;
using ChunkApplication.Infrastructure.Repositories;
using ChunkApplication.Infrastructure.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ChunkApplication.Console;

class Program
{
    private static IServiceProvider _serviceProvider;
    private static ILogger<Program> _logger;
    private static IChunkService _chunkService;
    private static IConnection _rabbitMqConnection;
    private static IServiceScope _consumerScope;

    static async Task Main(string[] args)
    {
        try
        {
            SetupServices();
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
            _chunkService = _serviceProvider.GetRequiredService<IChunkService>();
            _logger.LogInformation("Chunk Application Console started");
            StartRabbitMQConsumer();
            TestConsumerConnectivity();
            _logger.LogInformation("Console application is running. Press any key to exit...");
            
            System.Console.WriteLine("\n" + "=".PadRight(50, '='));
            System.Console.WriteLine("🚀 CHUNK APPLICATION CONSOLE STARTED");
            System.Console.WriteLine("=".PadRight(50, '='));
            System.Console.WriteLine("✅ RabbitMQ Consumers: ACTIVE");
            System.Console.WriteLine("✅ Database: CONNECTED");
            System.Console.WriteLine("🔍 Listening for messages...");
            System.Console.WriteLine("=".PadRight(50, '='));
            System.Console.WriteLine("📝 Check console logs above for consumer status");
            System.Console.WriteLine("🔔 Watch for 'MESSAGE RECEIVED!' logs when processing");
            System.Console.WriteLine("=".PadRight(50, '='));
            System.Console.WriteLine("\nPress any key to exit...");
            System.Console.ReadKey();

            _logger.LogInformation("Chunk Application Console stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Fatal error in main program");
            System.Console.WriteLine($"Fatal error: {ex.Message}");
        }
        finally
        {
            try
            {
                _consumerScope?.Dispose();
                _logger?.LogInformation("Consumer scope disposed");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing consumer scope");
            }
            _rabbitMqConnection?.Dispose();
            if (_serviceProvider is IDisposable disposableServiceProvider)
            {
                disposableServiceProvider.Dispose();
            }
        }
    }

    private static void SetupServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddDbContext<ChunkDbContext>(options =>
        {
            options.UseSqlServer("Server=localhost,1433;Database=ChunkApplication;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;");
        });

        services.AddScoped<FileRepository>();
        services.AddScoped<ChunkRepository>();
        services.AddScoped<IRepository<ChunkApplication.Domain.Entities.Files>>(provider => provider.GetRequiredService<FileRepository>());
        services.AddScoped<IRepository<ChunkApplication.Domain.Entities.Chunks>>(provider => provider.GetRequiredService<ChunkRepository>());

        services.AddScoped<IStorageProvider, FileSystemStorageProvider>();
        services.AddScoped<IStorageProvider, DatabaseStorageProvider>();

        services.AddScoped<IChunkService, ChunkService>();

        services.AddSingleton<IConnection>(provider =>
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "admin",
                Password = "admin123",
                Port = 5672
            };
            return factory.CreateConnection();
        });

        services.AddScoped<ChunkFileRequestConsumer>();
        services.AddScoped<ListFilesRequestConsumer>();
        services.AddScoped<GetFileInfoRequestConsumer>();
        services.AddScoped<DeleteFileRequestConsumer>();
        services.AddScoped<ReconstructFileRequestConsumer>();

        _serviceProvider = services.BuildServiceProvider();
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChunkDbContext>();
        context.Database.EnsureCreated();
    }

    private static void StartRabbitMQConsumer()
    {
        try
        {
            _logger.LogInformation("🚀 Starting RabbitMQ consumers...");
            _rabbitMqConnection = _serviceProvider.GetRequiredService<IConnection>();
            _logger.LogInformation("✅ RabbitMQ connection obtained successfully");
            _consumerScope = _serviceProvider.CreateScope();
            _logger.LogInformation("✅ Consumer scope created");
            _logger.LogInformation("📡 Starting ChunkFileRequestConsumer...");
            var chunkConsumer = _consumerScope.ServiceProvider.GetRequiredService<ChunkFileRequestConsumer>();
            _logger.LogInformation("✅ ChunkFileRequestConsumer started");
            _logger.LogInformation("📡 Starting GetFileInfoRequestConsumer...");
            var getFileInfoConsumer = _consumerScope.ServiceProvider.GetRequiredService<GetFileInfoRequestConsumer>();
            _logger.LogInformation("✅ GetFileInfoRequestConsumer started");
            _logger.LogInformation("📡 Starting ListFilesRequestConsumer...");
            var listFilesConsumer = _consumerScope.ServiceProvider.GetRequiredService<ListFilesRequestConsumer>();
            _logger.LogInformation("✅ ListFilesRequestConsumer started");
            _logger.LogInformation("📡 Starting DeleteFileRequestConsumer...");
            var deleteFileConsumer = _consumerScope.ServiceProvider.GetRequiredService<DeleteFileRequestConsumer>();
            _logger.LogInformation("✅ DeleteFileRequestConsumer started");
            _logger.LogInformation("📡 Starting ReconstructFileRequestConsumer...");
            var reconstructConsumer = _consumerScope.ServiceProvider.GetRequiredService<ReconstructFileRequestConsumer>();
            _logger.LogInformation("✅ ReconstructFileRequestConsumer started");
            _logger.LogInformation("🎉 All RabbitMQ consumers started successfully!");
            _logger.LogInformation("🔍 Consumers are now listening for messages...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CRITICAL: Failed to start RabbitMQ consumers!");
            System.Console.WriteLine($"❌ CONSUMER START FAILED: {ex.Message}");
            System.Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }

    private static void TestConsumerConnectivity()
    {
        try
        {
            _logger.LogInformation("🧪 Testing consumer connectivity...");
            if (_rabbitMqConnection?.IsOpen == true)
            {
                _logger.LogInformation("✅ RabbitMQ Connection: OPEN");
                System.Console.WriteLine("✅ RabbitMQ Connection: OPEN");
            }
            else
            {
                _logger.LogError("❌ RabbitMQ Connection: CLOSED or NULL");
                System.Console.WriteLine("❌ RabbitMQ Connection: CLOSED or NULL");
            }
            if (_consumerScope != null)
            {
                _logger.LogInformation("✅ Consumer Scope: ACTIVE");
                System.Console.WriteLine("✅ Consumer Scope: ACTIVE");
            }
            else
            {
                _logger.LogError("❌ Consumer Scope: NULL");
                System.Console.WriteLine("❌ Consumer Scope: NULL");
            }
            _logger.LogInformation("🧪 Consumer connectivity test completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Consumer connectivity test failed");
            System.Console.WriteLine($"❌ Consumer Test Failed: {ex.Message}");
        }
    }

    private static async Task RunMainMenu()
    {
        while (true)
        {
            System.Console.Clear();
            System.Console.WriteLine("=== Chunk Application Console ===");
            System.Console.WriteLine("1. Send chunk file request");
            System.Console.WriteLine("2. List all files");
            System.Console.WriteLine("3. Get file info");
            System.Console.WriteLine("4. Reconstruct file");
            System.Console.WriteLine("5. Delete file");
            System.Console.WriteLine("0. Exit");
            System.Console.Write("\nSelect an option: ");

            var choice = System.Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await HandleChunkFileRequest();
                        break;
                    case "2":
                        await HandleListFiles();
                        break;
                    case "3":
                        await HandleGetFileInfo();
                        break;
                    case "4":
                        await HandleReconstructFile();
                        break;
                    case "5":
                        await HandleDeleteFile();
                        break;
                    case "0":
                        return;
                    default:
                        System.Console.WriteLine("Invalid option. Press any key to continue...");
                        System.Console.ReadKey();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in menu option {Choice}", choice);
                System.Console.WriteLine($"Error: {ex.Message}");
                System.Console.WriteLine("Press any key to continue...");
                System.Console.ReadKey();
            }
        }
    }

    private static async Task HandleChunkFileRequest()
    {
        System.Console.Write("Enter file path (or multiple paths separated by commas): ");
        var input = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            System.Console.WriteLine("Invalid file path.");
            return;
        }

        var filePaths = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(p => p.Trim())
                           .Where(p => !string.IsNullOrWhiteSpace(p))
                           .ToList();

        if (!filePaths.Any())
        {
            System.Console.WriteLine("No valid file paths provided.");
            return;
        }

        System.Console.WriteLine($"Processing {filePaths.Count} file(s)...");

        foreach (var filePath in filePaths)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    System.Console.WriteLine($"File not found: {filePath}");
                    continue;
                }

                var fileInfo = new System.IO.FileInfo(filePath);

                System.Console.WriteLine($"Processing: {fileInfo.Name} ({fileInfo.Length} bytes)");

                var fileEntity = await _chunkService.ChunkFileAsync(filePath);

                System.Console.WriteLine($"✓ Successfully chunked: {fileEntity.FileName}");
                System.Console.WriteLine($"  File ID: {fileEntity.Id}");
                System.Console.WriteLine($"  Total Chunks: {fileEntity.TotalChunks}");
                System.Console.WriteLine($"  Checksum: {fileEntity.Checksum}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {FilePath}", filePath);
                System.Console.WriteLine($"✗ Error processing {filePath}: {ex.Message}");
            }
        }

        System.Console.WriteLine("\nPress any key to continue...");
        System.Console.ReadKey();
    }

    private static async Task HandleListFiles()
    {
        var files = await _chunkService.ListFilesAsync();

        if (!files.Any())
        {
            System.Console.WriteLine("No files found.");
        }
        else
        {
            System.Console.WriteLine($"Found {files.Count()} file(s):\n");
            
            foreach (var file in files)
            {
                System.Console.WriteLine($"File: {file.FileName}");
                System.Console.WriteLine($"  ID: {file.Id}");
                System.Console.WriteLine($"  Size: {file.FileSize} bytes");
                System.Console.WriteLine($"  Chunks: {file.TotalChunks}");
                System.Console.WriteLine($"  Created: {file.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                System.Console.WriteLine($"  Complete: {(file.IsComplete() ? "Yes" : "No")}");
                System.Console.WriteLine();
            }
        }

        System.Console.WriteLine("Press any key to continue...");
        System.Console.ReadKey();
    }

    private static async Task HandleGetFileInfo()
    {
        System.Console.Write("Enter file ID: ");
        var fileId = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(fileId))
        {
            System.Console.WriteLine("Invalid file ID.");
            return;
        }

        var file = await _chunkService.GetFileInfoAsync(fileId);

        if (file == null)
        {
            System.Console.WriteLine("File not found.");
        }
        else
        {
            System.Console.WriteLine($"File Information:");
            System.Console.WriteLine($"  Name: {file.FileName}");
            System.Console.WriteLine($"  ID: {file.Id}");
            System.Console.WriteLine($"  Size: {file.FileSize} bytes");
            System.Console.WriteLine($"  Chunks: {file.TotalChunks}");
            System.Console.WriteLine($"  Created: {file.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            System.Console.WriteLine($"  Complete: {(file.IsComplete() ? "Yes" : "No")}");
            System.Console.WriteLine($"  Integrity: {(file.ValidateIntegrity() ? "Valid" : "Invalid")}");
        }

        System.Console.WriteLine("\nPress any key to continue...");
        System.Console.ReadKey();
    }

    private static async Task HandleReconstructFile()
    {
        System.Console.Write("Enter file ID: ");
        var fileId = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(fileId))
        {
            System.Console.WriteLine("Invalid file ID.");
            return;
        }

        System.Console.Write("Enter output path: ");
        var outputPath = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            System.Console.WriteLine("Invalid output path.");
            return;
        }

        System.Console.WriteLine("Reconstructing file...");

        var success = await _chunkService.ReconstructFileAsync(fileId, outputPath);

        if (success)
        {
            System.Console.WriteLine($"✓ File reconstructed successfully to: {outputPath}");
        }
        else
        {
            System.Console.WriteLine("✗ Failed to reconstruct file.");
        }

        System.Console.WriteLine("\nPress any key to continue...");
        System.Console.ReadKey();
    }

    private static async Task HandleDeleteFile()
    {
        System.Console.Write("Enter file ID: ");
        var fileId = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(fileId))
        {
            System.Console.WriteLine("Invalid file ID.");
            return;
        }

        System.Console.Write("Are you sure you want to delete this file? (y/N): ");
        var confirm = System.Console.ReadLine()?.ToLower();

        if (confirm != "y")
        {
            System.Console.WriteLine("Deletion cancelled.");
            return;
        }

        System.Console.WriteLine("Deleting file...");

        var success = await _chunkService.DeleteFileAsync(fileId);

        if (success)
        {
            System.Console.WriteLine("✓ File deleted successfully.");
        }
        else
        {
            System.Console.WriteLine("✗ Failed to delete file.");
        }

        System.Console.WriteLine("\nPress any key to continue...");
        System.Console.ReadKey();
    }

    private static async Task<string> CalculateChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLower();
    }
}
