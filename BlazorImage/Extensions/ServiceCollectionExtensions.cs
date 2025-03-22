using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BlazorImage.Models;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using BlazorImage.Models.Interfaces;

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
    /// builder.Services.AddBlazorImage(config =>
    /// {
    ///     config.DefaultQuality = 80; 
    ///     config.Dir = "path/to" 
    /// });
    /// </code>
    /// </example>
    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection AddBlazorImage(
            this IServiceCollection services,
            Action<BlazorImageConfig>? configureOptions = default!)
        {
            // Register your services here

            ArgumentNullException.ThrowIfNull(services);

            // Create and configure the options instance
            var config = new BlazorImageConfig();
            configureOptions?.Invoke(config);

            // Normalize directory path
            config.Dir = config.Dir.Trim('/');

            // Register required services
            services.AddMemoryCache();

            // Add LiteDB singleton
            services.AddSingleton<ILiteDatabase>(sp =>
            {
                var env = sp.GetRequiredService<IWebHostEnvironment>();
                var webRootPath = env.WebRootPath;
                var _liteDbPath = Path.Combine(webRootPath, config.Dir, Constants.LiteDbName);
   
                var _liteDbConnectoinString = $"Filename={_liteDbPath};Connection=shared";
                var _db = new LiteDatabase(_liteDbConnectoinString);
               
                return _db;
            });
            services.TryAddSingleton<IFileService, FileService>();
            services.TryAddSingleton<ICacheService, CacheService>();
            services.TryAddSingleton<IImageProcessingService, ImageProcessingService>();
            services.TryAddSingleton<IImageElementService, ImageElementService>();
            services.TryAddScoped<IBlazorImageService, BlazorImageService>();

             services.AddSingleton<DictionaryCacheDataService>();

            // Register configuration as a singleton
            services.AddSingleton(config);

            // Register configuration with IOptions<T>
            services.Configure<BlazorImageConfig>(options =>
            {
                options.Dir = config.Dir;
                options.DefaultFileFormat = config.DefaultFileFormat;
                options.DefaultQuality = config.DefaultQuality;
            });

            services.AddSingleton<IHostedService, ImageOptimizationInitializer>();

            return services;
        }
    }

    // Hosted service to handle directory initialization after DI is built
    public class ImageOptimizationInitializer : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ImageOptimizationInitializer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
             var config = scope.ServiceProvider.GetRequiredService<IOptions<BlazorImageConfig>>().Value;

            if (!string.IsNullOrWhiteSpace(config.Dir))
            {

                fileService.EnsureDirectoriesExist(config.Dir.Trim('/'));
                await Task.CompletedTask;
            }
            
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
