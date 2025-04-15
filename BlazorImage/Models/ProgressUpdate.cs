namespace BlazorImage.Models
{
    internal sealed record ProgressUpdate(int Percentage, string Message, bool IsFinal = false, bool Success = false, string? Error = null);
}
