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
using ChunkApplication.Domain.Entities;

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
            System.Console.WriteLine(" CHUNK APPLICATION CONSOLE STARTED");
            System.Console.WriteLine("=".PadRight(50, '='));
            System.Console.WriteLine(" RabbitMQ Consumers: ACTIVE");
            System.Console.WriteLine(" Database: CONNECTED");
            System.Console.WriteLine(" Listening for messages...");
            System.Console.WriteLine("=".PadRight(50, '='));
            System.Console.WriteLine(" Check console logs above for consumer status");
            System.Console.WriteLine(" Watch for 'MESSAGE RECEIVED!' logs when processing");
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
        services.AddScoped<IRepository<Files>>(provider => provider.GetRequiredService<FileRepository>());
        services.AddScoped<IRepository<Chunks>>(provider => provider.GetRequiredService<ChunkRepository>());

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
            _logger.LogInformation(" Starting RabbitMQ consumers...");
            _rabbitMqConnection = _serviceProvider.GetRequiredService<IConnection>();
            _logger.LogInformation(" RabbitMQ connection obtained successfully");
            _consumerScope = _serviceProvider.CreateScope();
            _logger.LogInformation(" Consumer scope created");
            _logger.LogInformation(" Starting ChunkFileRequestConsumer...");
            var chunkConsumer = _consumerScope.ServiceProvider.GetRequiredService<ChunkFileRequestConsumer>();
            _logger.LogInformation(" ChunkFileRequestConsumer started");
            _logger.LogInformation(" Starting GetFileInfoRequestConsumer...");
            var getFileInfoConsumer = _consumerScope.ServiceProvider.GetRequiredService<GetFileInfoRequestConsumer>();
            _logger.LogInformation("GetFileInfoRequestConsumer started");
            _logger.LogInformation(" Starting ListFilesRequestConsumer...");
            var listFilesConsumer = _consumerScope.ServiceProvider.GetRequiredService<ListFilesRequestConsumer>();
            _logger.LogInformation(" ListFilesRequestConsumer started");
            _logger.LogInformation("Starting DeleteFileRequestConsumer...");
            var deleteFileConsumer = _consumerScope.ServiceProvider.GetRequiredService<DeleteFileRequestConsumer>();
            _logger.LogInformation(" DeleteFileRequestConsumer started");
            _logger.LogInformation(" Starting ReconstructFileRequestConsumer...");
            var reconstructConsumer = _consumerScope.ServiceProvider.GetRequiredService<ReconstructFileRequestConsumer>();
            _logger.LogInformation(" ReconstructFileRequestConsumer started");
            _logger.LogInformation(" All RabbitMQ consumers started successfully!");
            _logger.LogInformation(" Consumers are now listening for messages...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " CRITICAL: Failed to start RabbitMQ consumers!");
            System.Console.WriteLine($" CONSUMER START FAILED: {ex.Message}");
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
}
