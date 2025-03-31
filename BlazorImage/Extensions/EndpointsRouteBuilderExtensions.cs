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

            var routeGroup = app.MapGroup(route);


            routeGroup.MapGet("/", (HttpContext context, IDashboardService dashboardService) =>
            {
              return  TypedResults.Content(dashboardService.DashboardData(route), "text/html; charset=utf-8"); 
            });

            routeGroup.MapGet("/delete", async (HttpContext context, ICacheService cacheService) =>
            {

                var cache = context.Request.Query["cache"].ToString();
                await cacheService.DeleteFromCacheAsync(cache);
                    
                context.Response.Redirect(route); 
            });

            routeGroup.MapGet("/reset-all", async (HttpContext context, ICacheService cacheService) =>
            {

                await cacheService.ResetAllFromCacheAsync();
                context.Response.Redirect(route);
            });
            routeGroup.MapGet("hard-reset-all", (HttpContext context, ICacheService cacheService) =>
            {

                cacheService.HardResetAllFromCache();
                context.Response.Redirect(route);
            });
            routeGroup.MapGet("/refresh-all", (HttpContext context, ICacheService cacheService, DictionaryCacheDataService dictionaryCacheData) =>
            {
                dictionaryCacheData.ClearData();
                context.Response.Redirect(route);
            });

            return app;
        }


    }

}
