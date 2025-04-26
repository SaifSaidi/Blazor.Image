using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.ObjectPool;
using System.Text;
namespace BlazorImage.Extensions
{
    /// <summary>
    /// Registers the Blazor Image Optimization service in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to which the service is added.</param>
    /// <param name="configureOptions">
    /// A delegate for configuring the <see cref="BlazorImageConfig"/>.
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// Example usage:
    /// <code>
    /// <example>
    /// Example usage:
    /// <code>
    /// builder.Services.AddBlazorImage(config =>
    /// { 
    ///     config.OutputDir = "path/to";
    ///     config.ConfigSizes = [480, 640, 1024, 1200, ....];
    ///     config.DefaultQuality = 80; 
    ///     config.DefaultFileFormat = FileFormat.jpeg;
    /// });
    /// </code>
    /// </example>
    /// </code>
    /// </example>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorImage(
            this IServiceCollection services,
            Action<BlazorImageConfig>? configureOptions = default!)
        {

            ArgumentNullException.ThrowIfNull(services);

            var config = new BlazorImageConfig();
            configureOptions?.Invoke(config);

            // Normalize directory path
            config.OutputDir = config.OutputDir.Replace('\\', '/').Trim('/');

            services.AddMemoryCache();

            // Add LiteDB singleton
            services.AddSingleton<ILiteDatabase>(sp =>
            {
                var env = sp.GetRequiredService<IWebHostEnvironment>();
                var webRootPath = env.WebRootPath;
                var _liteDbPath = Path.Combine(webRootPath, config.OutputDir, Constants.LiteDbName);

                var _liteDbConnectoinString = $"Filename={_liteDbPath};Connection=shared";
                var _db = new LiteDatabase(_liteDbConnectoinString);

                return _db;
            });

            // Register the ObjectPool<StringBuilder> as a Singleton
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddSingleton<ObjectPool<StringBuilder>>(serviceProvider =>
            {
                var provider = serviceProvider.GetRequiredService<ObjectPoolProvider>();
                // Use the same policy as before or adjust
                var policy = new StringBuilderPooledObjectPolicy
                {
                    InitialCapacity = 256,
                    MaximumRetainedCapacity = 4096
                };
                return provider.Create(policy);
            });

            services.TryAddSingleton<IFileService, FileService>();
            services.TryAddSingleton<ICacheService, CacheService>();
            services.TryAddSingleton<IDashboardService, DashboardService>();
            services.TryAddSingleton<IImageProcessingService, ImageProcessingService>();
            services.TryAddSingleton<IImageElementService, ImageElementService>();
            services.TryAddSingleton<IBlazorImageService, BlazorImageService>();
            services.TryAddSingleton<IGenerateImageDataService, GenerateImageDataService>();

            services.AddSingleton<DictionaryCacheDataService>();

            // Register configuration as a singleton
            services.AddSingleton(config);

            // Register configuration with IOptions<T>
            services.Configure<BlazorImageConfig>(options =>
            {
                options.OutputDir = config.OutputDir;
                options.Sizes = [.. config.Sizes.Order()];
                options.DefaultQuality = config.DefaultQuality;
                options.DefaultFileFormat = config.DefaultFileFormat;
            });

            services.AddSingleton<IHostedService, ImageOptimizationInitializer>();

            return services;
        }
    }

    internal class ImageOptimizationInitializer : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ImageOptimizationInitializer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
            var config = scope.ServiceProvider.GetRequiredService<IOptions<BlazorImageConfig>>().Value;

            if (!string.IsNullOrWhiteSpace(config.OutputDir))
            {

                fileService.EnsureDirectoriesExist(config.OutputDir.Trim('/'));

            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
