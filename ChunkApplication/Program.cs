using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using ChunkApplication.Data;
using ChunkApplication.Interfaces;
using ChunkApplication.Repositories;
using ChunkApplication.Services;
using ChunkApplication.Consumers;
using MassTransit;

namespace ChunkApplication;

class Program
{
    static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/chunk-application-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting Chunk Application (Consumer Mode)...");

            // Create host builder
            var host = CreateHostBuilder(args).Build();

            // Ensure database is created
            using (var scope = host.Services.CreateScope())
            {
                try
                {
                    var context = scope.ServiceProvider.GetRequiredService<ChunkDbContext>();
                    Log.Information("DbContext retrieved successfully");
                    
                    await context.Database.EnsureCreatedAsync();
                    Log.Information("Database initialized successfully");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to initialize database");
                    Console.WriteLine($"Database Error: {ex.Message}");
                    throw;
                }
            }

            // Run the application as a consumer
            await RunConsumerAsync(host.Services);

            Log.Information("Chunk Application completed successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Chunk Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((hostContext, services) =>
            {
                // Add DbContext
                var connectionString = hostContext.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Fallback connection string if appsettings.json is not found
                    connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=ChunkApplication;Trusted_Connection=true;TrustServerCertificate=true;";
                }
                services.AddDbContext<ChunkDbContext>(options =>
                    options.UseSqlServer(connectionString));

                // Add Repositories
                services.AddScoped<IFileRepository, FileRepository>();

                // Add Storage Providers
                services.AddScoped<IStorageProvider, FileSystemStorageProvider>();
                services.AddScoped<IStorageProvider, DatabaseStorageProvider>();

                // Add MassTransit
                services.AddMassTransit(x =>
                {
                    // Add consumers
                    x.AddConsumer<ChunkProcessingConsumer>();
                    x.AddConsumer<ChunkFileRequestConsumer>();
                    x.AddConsumer<ReconstructFileRequestConsumer>();
                    x.AddConsumer<ListFilesRequestConsumer>();
                    x.AddConsumer<GetFileInfoRequestConsumer>();
                    x.AddConsumer<DeleteFileRequestConsumer>();

                    // Always use RabbitMQ for now (both local and Docker)
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        // Check if we're running locally or in Docker
                        var isLocal = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == null;
                        
                        if (isLocal)
                        {
                            // Local development - try to connect to localhost
                            cfg.Host("localhost", "/", h =>
                            {
                                h.Username("admin");
                                h.Password("admin123");
                            });
                        }
                        else
                        {
                            // Docker - use service name
                            cfg.Host("rabbitmq", "/", h =>
                            {
                                h.Username("admin");
                                h.Password("admin123");
                            });
                        }

                        // Explicitly configure endpoints for each consumer
                        cfg.ReceiveEndpoint("ChunkFileRequest", e =>
                        {
                            e.ConfigureConsumer<ChunkFileRequestConsumer>(context);
                        });

                        cfg.ReceiveEndpoint("ListFilesRequest", e =>
                        {
                            e.ConfigureConsumer<ListFilesRequestConsumer>(context);
                        });

                        cfg.ReceiveEndpoint("ReconstructFileRequest", e =>
                        {
                            e.ConfigureConsumer<ReconstructFileRequestConsumer>(context);
                        });

                        cfg.ReceiveEndpoint("GetFileInfoRequest", e =>
                        {
                            e.ConfigureConsumer<GetFileInfoRequestConsumer>(context);
                        });

                        cfg.ReceiveEndpoint("DeleteFileRequest", e =>
                        {
                            e.ConfigureConsumer<DeleteFileRequestConsumer>(context);
                        });

                        cfg.ReceiveEndpoint("ChunkProcessing", e =>
                        {
                            e.ConfigureConsumer<ChunkProcessingConsumer>(context);
                        });
                    });
                });

                // Add Services
                services.AddScoped<IChunkService, ChunkService>();

                // Add Logging
                services.AddLogging();
            });

    static async Task RunConsumerAsync(IServiceProvider serviceProvider)
    {
        var busControl = serviceProvider.GetRequiredService<IBusControl>();
        var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();

        logger.LogInformation("Starting Chunk Application in Consumer Mode...");
        logger.LogInformation("Waiting for messages from RabbitMQ...");
        logger.LogInformation("Press Ctrl+C to exit");

        // Start the bus
        await busControl.StartAsync();
        
        // Debug: Consumer'ların register edildiğini kontrol et
        logger.LogInformation("Bus started successfully!");
        logger.LogInformation("Consumers registered: ChunkFileRequestConsumer, ListFilesRequestConsumer, etc.");
        
        // Console'a da yazdır
        Console.WriteLine("🚀 Bus started successfully!");
        Console.WriteLine("✅ Consumers registered: ChunkFileRequestConsumer, ListFilesRequestConsumer, etc.");

        // Keep the application running
        try
        {
            // Wait for cancellation
            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
            };

            // Wait until cancelled
            await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Shutting down...");
        }
        finally
        {
            await busControl.StopAsync();
        }
    }
}
