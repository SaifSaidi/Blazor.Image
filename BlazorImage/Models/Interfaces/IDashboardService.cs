
namespace BlazorImage.Models.Interfaces
{
    internal interface IDashboardService
    {
        ValueTask<string> DashboardDataAsync(string route);
    }
}
