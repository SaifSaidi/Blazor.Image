using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BlazorImage.Extensions
{
     
    public static class EndpointsRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapBlazorImage(this IEndpointRouteBuilder app, string route)
        {
            if (!route.StartsWith('/'))
            {
                route = "/" + route;
            }

            if (string.IsNullOrWhiteSpace(route))
            {
                throw new ArgumentException("route is null or whitespace!");
            }
            app.MapGet(route, async (HttpContext context, ICacheService cacheService) =>
            {


                await context.Response.WriteAsync(cacheService.ReadData(route));
            });

            app.MapGet($"{route}/delete", async (HttpContext context, ICacheService cacheService) =>
            {

                var cache = context.Request.Query["cache"].ToString();
                await cacheService.DeleteFromCacheAsync(cache);
                context.Response.Redirect(route);
            });

            app.MapGet($"{route}/reset-all", async (HttpContext context, ICacheService cacheService) =>
            {

                await cacheService.ResetAllFromCacheAsync();
                context.Response.Redirect(route);
            });
            app.MapGet($"{route}/hard-reset-all", (HttpContext context, ICacheService cacheService) =>
            {

                cacheService.HardResetAllFromCache();
                context.Response.Redirect(route);
            });
            app.MapGet($"{route}/refresh-all", (HttpContext context, ICacheService cacheService, DictionaryCacheDataService2 dictionaryCacheData) =>
            {
                dictionaryCacheData.ClearData();
                context.Response.Redirect(route);
            });

            return app;
        }
    }

}
