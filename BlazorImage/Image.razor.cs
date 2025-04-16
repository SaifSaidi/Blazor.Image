using BlazorImage.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Channels;

namespace BlazorImage
{
   
    public partial class Image : IDisposable
    {
        // Services
        [Inject] IBlazorImageService BlazorImageService { get; set; } = default!;
        [Inject] private IImageElementService ImageElementService { get; set; } = default!;
        [Inject]  private IJSRuntime JSRuntime { get; set; } = default!;

        // State Fields
        private string? _error;
        private string? _statusMessage;
        private int? _loadingPercentage;
        private string? _fallbackImageSrc;
        private string? _mimeType;
        private ElementReference _imageRef;
        private bool _isLoadComplete ;
        private string? _containerStyle;
        private string? _sizesAttr;
        private string? _srcset;
        private string? _dataSrcset;

        // Helper properties derived from parameters
        private bool _isFillMode => Fill is true;
        private string _figureClass => _isFillMode ? "fill-mode" : "fixed-mode";
        private string _loadingClass => Priority ? "" : "_plachoder_lazy_load";
        private string LoadingType => Priority ? "eager" : "lazy";
        private bool _isInteractive => RendererInfo.IsInteractive && 
            EnableInteractiveState;


        // Parameters

        /// <summary>
        /// The source URL of the image to be displayed.
        /// </summary>
        [Parameter, EditorRequired]
        public required string Src { get; set; }

        /// <summary>
        /// Alternative text for the image, used for accessibility and SEO.
        /// Should be descriptive of the image content.
        /// </summary>
        [Parameter, EditorRequired]
        public required string Alt { get; set; }

        /// <summary>
        /// The width of the image in pixels. Required when Fill is false.
        /// </summary>
        [Parameter]
        public int? Width { get; set; }

        /// <summary>
        /// The height of the image in pixels. Required when Fill is false.
        /// </summary>
        [Parameter]
        public int? Height { get; set; }

        /// <summary>
        /// When true, the image will fill its container while maintaining aspect ratio.
        /// Width and Height should not be provided when Fill is true.
        /// </summary>
        [Parameter]
        public bool? Fill { get; set; }

        /// <summary>
        /// Enables interactive state management for the image component.
        /// </summary>
        [Parameter]
        public bool EnableInteractiveState { get; set; }

        /// <summary>
        /// Fallback image source to display when the main image fails to load.
        /// </summary>
        [Parameter]
        public string? DefaultSrc { get; set; }

        /// <summary>
        /// When true, the image is loaded with high priority (eager loading).
        /// </summary>
        [Parameter]
        public bool Priority { get; set; }

        /// <summary>
        /// Title attribute for the image, displayed as a tooltip on hover.
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary>
        /// Additional CSS classes to apply to the image element.
        /// </summary>
        [Parameter]
        public string? CssClass { get; set; }

        /// <summary>
        /// Inline styles to apply to the image element.
        /// </summary>
        [Parameter]
        public string? Style { get; set; }

        /// <summary>
        /// Image quality setting (15-100) for optimization.
        /// </summary>
        [Parameter]
        public int? Quality { get; set; }

        /// <summary>
        /// The desired output format for the image.
        /// </summary>
        [Parameter]
        public FileFormat? Format { get; set; }

        /// <summary>
        /// The sizes attribute for responsive images, defining how image sizes map to viewport sizes.
        /// </summary>
        [Parameter]
        public string? Sizes { get; set; }

        /// <summary>
        /// When true, displays additional debugging information for developers.
        /// </summary>
        [Parameter]
        public bool EnableDeveloperMode { get; set; }

        /// <summary>
        /// CSS class to apply to the image caption.
        /// </summary>
        [Parameter]
        public string? CaptionClass { get; set; }

        /// <summary>
        /// Caption text to display below the image.
        /// </summary>
        [Parameter]
        public string? Caption { get; set; }

        /// <summary>
        /// Additional attributes to apply to the image element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object>? AdditionalAttributes { get; set; }


        private CancellationTokenSource _cts = new();

        protected override async Task OnParametersSetAsync()
        {
            if (_cts.IsCancellationRequested)
            {
                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }

            if (!ValidateParameters()) return;

            CalculateContainerStyle();
            CalculateSizesAttribute();
            await LoadImageInfoAsync();

            await base.OnParametersSetAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_isInteractive && !Priority && _imageRef.Context != null)
            {
                await JSRuntime.InvokeVoidAsync("BlazorLazyLoad", _imageRef);
            }
        }

        private async Task LoadImageInfoAsync()
        {
            _isLoadComplete = false;

            Result<ImageInfo>? result = await BlazorImageService.GetImageInfoAsync(Src, Quality, Format);

            if (result?.IsSuccess ?? false)
            {
                LoadImageSources(result.Value);
            }
            else if (result != null)
            {
                HandleErrorResult(result.Error ?? "Error loading image!");
            }
            else
            {
                await ProcessImageWithProgressUpdates();
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _imageRef = default;
        }

        private string GetSafeDefaultImage() => !string.IsNullOrEmpty(DefaultSrc)
              ? DefaultSrc!
              : "_content/BlazorImage/default.png";  
        private void CalculateContainerStyle()
        {
            if (_isFillMode)
            {
                var aspect = ImageElementService.GetAspectRatio();
                _containerStyle = $"--img-aspect-ratio: {aspect:F6}; aspect-ratio: {aspect:F2};";
            }
            else
            {
                var aspectRatio = (double)Width!.Value / Height!.Value;
                _containerStyle = $"aspect-ratio: {aspectRatio:F2}; max-width: {Width}px;";
            }
        }
        private bool ValidateParameters()
        {
            bool isFill = _isFillMode;

            if (!isFill && (Width == null || Height == null || Width <= 0 || Height <= 0))
            {
                HandleErrorResult("In fixed mode ('Fill' is false or unset), positive 'Width' and 'Height' must be provided.");
                return false;
            }

            if (isFill && (Width != null || Height != null))
            {
                HandleErrorResult("If 'Fill' is set to true, 'Width' and 'Height' should not be provided.");
                return false;
            }

            _error = null;
            return true;
        }

        private void CalculateSizesAttribute()
        {
            if (!string.IsNullOrEmpty(Sizes))
            {
                _sizesAttr = Sizes;
            }
            else if (_isFillMode)
            {
                _sizesAttr = "100vw"; 
            }
            else if (Width.HasValue)
            {
                _sizesAttr = $"{Width.Value}px";
            }
            else
            {
                _sizesAttr = "100vw"; // Fallback default
            }
        }

        private void HandleErrorResult(string errorMessage)
        {
            _error = errorMessage;
            _isLoadComplete = true;
        }

        private void LoadImageSources(ImageInfo imageInfo)
        {

            var (source, fallback, placeholder) = ImageElementService.GetStaticPictureSourceWithMetadataInfo(
                    imageInfo.SanitizedName,
                    imageInfo.Quality!.Value,
                    imageInfo.Format!.Value,
                    Width);


            if (EnableInteractiveState || Priority)
            {
                _srcset = source;
                _dataSrcset = null;
            }
            else
            {
                _srcset = placeholder;
                _dataSrcset = source;
            }

            _fallbackImageSrc = fallback;
            _mimeType = imageInfo.Format?.ToMimeType();
            _error = null;
            _isLoadComplete = true;
        }

        private async Task ProcessImageWithProgressUpdates()
        {
            _loadingPercentage = 0;
            _statusMessage = "Processing image...";
            StateHasChanged();
            var options = new BoundedChannelOptions(10)
            {
                FullMode = BoundedChannelFullMode.DropOldest,

            };

            var channel = Channel.CreateBounded<ProgressUpdate>(options);

            await BlazorImageService.ProcessImageInBackgroundAsync(Src, Quality, Format, channel.Writer);

            try
            {
                await foreach (var message in channel.Reader.ReadAllAsync())
                {
                    _statusMessage = message.Message;
                    _loadingPercentage = message.Percentage;
                    if (message.Error != null)
                    {
                        HandleErrorResult(message.Error);
                        break;
                    }

                    StateHasChanged();
                }

                await LoadImageInfoAsync();

            }
            catch (Exception ex)
            {
                HandleErrorResult($"Error during image processing: {ex.Message}");
            }
            finally
            {
                _statusMessage = null;
                _loadingPercentage = null;
                _isLoadComplete = true;
            }
        }

    }
}
