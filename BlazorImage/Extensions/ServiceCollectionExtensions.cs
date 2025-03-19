using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BlazorImage.Models;

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
            // Register required services
            services.AddMemoryCache();
            services.TryAddSingleton<IFileService, FileService>();
            services.TryAddScoped<ICacheService, CacheService>();
            //services.TryAddScoped<IImageElementService, ImageElementService>();
            //services.TryAddScoped<IImageProcessingService, ImageProcessingService>();
            //services.TryAddScoped<IImageOptimizationService, ImageOptimizationService>();

            //services.AddSingleton<DictionaryCacheDataService>();
            services.AddSingleton<DictionaryCacheDataService2>();
            services.AddSingleton<IHostedService, ImageOptimizationInitializer>();
            // Create and configure the options instance
            var config = new BlazorImageConfig();
            configureOptions?.Invoke(config);

            // Normalize directory path
            config.Dir = config.Dir.Trim('/');
             
            // Register configuration as a singleton
            services.AddSingleton(config);
             

            // Register configuration with IOptions<T>
            services.Configure<BlazorImageConfig>(options =>
            {
                options.Dir = config.Dir;
                options.DefaultFileFormat = config.DefaultFileFormat;
                options.DefaultQuality = config.DefaultQuality;
            });



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
            using (var scope = _scopeFactory.CreateScope())
            {
                var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                var config = scope.ServiceProvider.GetRequiredService<IOptions<BlazorImageConfig>>().Value;

                if (!string.IsNullOrWhiteSpace(config.Dir))
                {

                    fileService.EnsureDirectoriesExist(config.Dir.Trim('/'));
                }

                ImageInfo image = new("/asdasd/asdasd/asdimage 1", 100, 100, FileFormat.webp, 75, DateTime.Now);
                ImageInfo image2 = new("image/asd/2", 100, 100, FileFormat.webp, 75, DateTime.Now);
                ImageInfo image3 = new("imagasd/3/", 100, 100, FileFormat.webp, 75, DateTime.Now);

                Console.WriteLine(await cacheService.SaveToCacheAsync("c1", image));
                Console.WriteLine(await cacheService.SaveToCacheAsync("c2", image2));
                Console.WriteLine(await cacheService.SaveToCacheAsync("c3", image3));

                var cache=  await cacheService.GetFromCacheAsync("c1");
                  Console.WriteLine("From Cache:" + cache);
            }

        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
