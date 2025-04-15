using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> 
/// to configure endpoints related to the Blazor Image optimization system.
/// </summary>

namespace BlazorImage.Extensions
{
    public static class EndpointsBlazorImageRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapBlazorImageRuntime(this IEndpointRouteBuilder endpoints)
        {
            var app = endpoints as WebApplication
                      ?? throw new InvalidOperationException("This method must be used with WebApplication.");

            using var scope = app.Services.CreateScope();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
            var config = scope.ServiceProvider.GetRequiredService<IOptions<BlazorImageConfig>>().Value;

            if (string.IsNullOrWhiteSpace(config.OutputDir))
            {
                throw new InvalidOperationException("BlazorImageConfig.OutputDir is required.");
            }

            var outputDir = config.OutputDir;
            fileService.EnsureDirectoriesExist(outputDir);

            var physicalPath = fileService.GetRootPath(outputDir);


            var requestPath = $"/{outputDir}";
            var provider = new FileExtensionContentTypeProvider();

            provider.Mappings[".avif"] = "image/avif";
            provider.Mappings[".webp"] = "image/webp";
            provider.Mappings[".jpeg"] = "image/jpeg";
            provider.Mappings[".jpg"] = "image/jpeg";
            provider.Mappings[".png"] = "image/png";

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(physicalPath),
                RequestPath = requestPath,
                ContentTypeProvider = provider
            });


            return endpoints;
        }
    }
}