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
    /// <example>
    /// Example usage:
    /// <code>
    /// builder.Services.AddBlazorImage(config =>
    /// {
    ///     config.DefaultQuality = 80; 
    ///     config.Dir = "path/to";
    ///     config.DefaultFileFormat = FileFormat.jpeg;
    ///     config.ConfigSizes = new int[] { 100, 200, 300 };
    ///     config.AspectWidth = 16;
    ///     config.AspectHeigth = 9;
    ///     config.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
    ///     config.SlidingExpiration = TimeSpan.FromHours(1);
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
            config.Dir = config.Dir.Trim('/');

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
            services.TryAddSingleton<IDashboardService, DashboardService>();
            services.TryAddSingleton<IImageProcessingService, ImageProcessingService>();
            services.TryAddSingleton<IImageElementService, ImageElementService>();
            services.TryAddSingleton<IBlazorImageService, BlazorImageService>();

             services.AddSingleton<DictionaryCacheDataService>();

            // Register configuration as a singleton
            services.AddSingleton(config);

            // Register configuration with IOptions<T>
            services.Configure<BlazorImageConfig>(options =>
            {
                options.Dir = config.Dir;
                options.DefaultFileFormat = config.DefaultFileFormat;
                options.DefaultQuality = config.DefaultQuality;
                options.ConfigSizes = config.ConfigSizes;
                options.AspectHeigth = config.AspectHeigth;
                options.AspectWidth = config.AspectWidth;
                options.AbsoluteExpirationRelativeToNow = config.AbsoluteExpirationRelativeToNow;
                options.SlidingExpiration = config.SlidingExpiration;
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

            if (!string.IsNullOrWhiteSpace(config.Dir))
            {

                fileService.EnsureDirectoriesExist(config.Dir.Trim('/'));
               
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
