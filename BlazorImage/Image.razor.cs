using BlazorImage.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading;
using System.Threading.Channels;

namespace BlazorImage
{
    public partial class Image : IDisposable
    {
        #region Injected Services

        [Inject] IBlazorImageService BlazorImageService { get; set; } = default!;
        [Inject] private IImageElementService ImageElementService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        #endregion

        #region Parameters

        /// <summary>
        /// The source URL of the image to be displayed.
        /// </summary>
        [Parameter, EditorRequired]
        public required string Src { get; set; }

        /// <summary>
        /// Alternative text for the image, used for accessibility and SEO.
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
        /// Aspect ratio for the image when Fill is true. Defaults to 4:3.
        /// </summary>
        [Parameter]
        public (int AspectWidth, int AspectHeight)? AspectRatio { get; set; } = (4, 3);

        /// <summary>
        /// When true, the image will fill its container while maintaining aspect ratio.
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
        /// Unique identifier for the image element.
        /// </summary>
        [Parameter]
        public string? Id { get; set; }

        /// <summary>
        /// Additional attributes to apply to the image element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object>? AdditionalAttributes { get; set; }

        #endregion

        #region Internal Fields and State

        private (int? Width, int? Height, bool? Fill, (int, int)? AspectRatio) _lastLayoutParams;

        private ElementReference _imageRef;
        private bool _isLoadComplete;
        private string? _sizesAttr;
        private string? _srcset;
        private string? _dataSrcset;

        private string? _error;
        private string? _statusMessage;
        private int? _loadingPercentage;

        private string? FallbackImageSrc;
        private string? MimeType;

        private string? ContainerStyle;
        private string? ComputedCaptionClass;
        private bool HasCaption;

        private string? _imageId;


        private CancellationTokenSource _cts = new();

        private bool IsFillMode => Fill is true;
        private string FigureClass => IsFillMode ? "fill-mode" : "fixed-mode";
        private bool IsPreload => Priority && Id is not null;

        private (string Class, string Loading, string Decoding) GetLoadAttributes =>
            Priority ? ("", "eager", "auto") : ("_placeholder_lazy_load", "lazy", "async");

        #endregion

        #region Lifecycle Methods

        protected override void OnInitialized()
        {
            _imageId = string.IsNullOrWhiteSpace(Id) ? $"img_{Guid.NewGuid().ToString("N").Substring(0, 8)}" : Id.Trim();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();

            if (!ValidateParameters()) return;

            CalculateStylesAndAttributes();

            await LoadImageInfoAsync(_cts.Token);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (RendererInfo.IsInteractive &&
                EnableInteractiveState &&
                !Priority &&
                _imageRef.Context != null)
            {
                await JSRuntime.InvokeVoidAsync("BlazorLazyLoad", _imageRef);
            }
        }

        #endregion

        #region Image Processing & Rendering

        private async Task LoadImageInfoAsync(CancellationToken cancellationToken)
        {
            _isLoadComplete = false;
            _error = null;

            var result = await BlazorImageService.GetImageInfoAsync(Src, Quality, Format, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            if (result?.IsSuccess == true)
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

        private async Task ProcessImageWithProgressUpdates()
        {
            _loadingPercentage = 0;
            _statusMessage = "Processing image...";
            StateHasChanged();

            var channel = Channel.CreateBounded<ProgressUpdate>(new BoundedChannelOptions(15));
            await BlazorImageService.ProcessImageInBackgroundAsync(Src, Quality, Format, channel.Writer);

            try
            {
                await foreach (var update in channel.Reader.ReadAllAsync())
                {
                    _statusMessage = update.Message;
                    _loadingPercentage = update.Percentage;

                    if (update.Error != null)
                    {
                        HandleErrorResult(update.Error);
                        break;
                    }

                    StateHasChanged();
                }

                await LoadImageInfoAsync(_cts.Token);
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

            FallbackImageSrc ??= fallback;
            MimeType ??= imageInfo.Format?.ToMimeType();

            _error = null;
            _isLoadComplete = true;
        }

        #endregion

        #region Helpers


        private void CalculateStylesAndAttributes()
        {
            var currentParams = (Width, Height, Fill, AspectRatio);

            if (_lastLayoutParams == currentParams) return;
            _lastLayoutParams = currentParams;


            var hasAspectRatio = AspectRatio is { AspectWidth: > 0, AspectHeight: > 0 };
            double aspectRatioValue = hasAspectRatio
                ? (double)AspectRatio.Value.AspectWidth / AspectRatio.Value.AspectHeight
                : 4.0 / 3.0;

            if (AspectRatio != null)
            {
                Style = $"--img-aspect-ratio: {aspectRatioValue:0.##};";
                ContainerStyle = IsFillMode
                    ? $"aspect-ratio: {aspectRatioValue:0.##};"
                    : Width.HasValue ? $"--img-container-width: {Width}px;" : null;
            }

            _sizesAttr = !string.IsNullOrEmpty(Sizes)
                ? Sizes
                : IsFillMode ? "100vw"
                : Width.HasValue ? $"(max-width: {Width.Value}px) 100vw, {Width.Value}px"
                : "100vw";

            HasCaption = !string.IsNullOrEmpty(Caption);
            ComputedCaptionClass = string.IsNullOrWhiteSpace(CaptionClass) ? null : CaptionClass;
        }

        private bool ValidateParameters()
        {
            bool isFill = IsFillMode;

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

        private void HandleErrorResult(string errorMessage)
        {
            _error = errorMessage;
            _isLoadComplete = true;
            StateHasChanged();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _imageRef = default;
        }

        #endregion
    }
}
