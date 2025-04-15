# <img src="banner.png" alt="BlazorImage library Banner" width="100%" height="auto">

[![NuGet version (BlazorImage)](https://img.shields.io/nuget/v/BlazorImage.svg?style=flat-square)](https://www.nuget.org/packages/BlazorImage/)  [![NuGet downloads (BlazorImage)](https://img.shields.io/nuget/dt/BlazorImage.svg?style=flat-square)](https://www.nuget.org/packages/BlazorImage/)
## BlazorImage: Image Optimization for Blazor

Automatically optimize images used in <a href='https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor'> Blazor </a> projects (jpeg, png, webp and avif).

## Features

* **🖼️ Optimized Static Assets**: Generates highly optimized images directly as static assets for blazing-fast loading.
* **🗜️ Image Compression:** Image sizes can often get reduced between 70-90%
* **📏 Responsive Images:** Automatically generates multiple image sizes for different screen widths, ensuring optimal loading times and performance.
* **📸 Image Formats:** Supports multiple image formats, including WebP, JPEG, PNG, and AVIF, allowing for flexibility in image delivery.
* **⚪ Placeholder:** Generates a low-quality placeholder image to enhance the user experience during loading.
* **🔄 Lazy Loading:** Improves initial page load times by loading images only when they are visible to the user
* **🗄️ Intelligent Caching:** Long-term caching with efficient revalidation ensures fresh content without re-processing.
* **⚡ Versatile Rendering:** Supports both Static and Interactive rendering modes for maximum flexibility in your Blazor apps.


## 🚀 Getting Started

### Prerequisites

* [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download) or higher

### Limitations

* Currently supports **.NET 9.0 or higher only.**
* **Local images only** are supported at this time. Support for remote images is planned for future releases.
* **Blazor WebAssembly is not yet supported.**

### Installation

Install BlazorImage via the .NET CLI or by adding a package reference to your project file.

**Using .NET CLI:**

```xml 
dotnet add package BlazorImage --version 1.0.3
```

Using NuGet Package Manager:

```xml
<PackageReference Include="BlazorImage" Version="1.0.3" />
```

Next, add the BlazorImage namespace to your _Imports.razor file:


```csharp
using BlazorImage;
```

### Register Services

To enable BlazorImage in your Blazor application, register the necessary services within your Program.cs file:


```csharp
builder.Services.AddBlazorImage();
```
You can further configure BlazorImage with the following options within the AddBlazorImage method:
```csharp
builder.Services.AddBlazorImage(options =>
{
    // Path for storing processed images. Default: "_optimized"
    options.OutputDir = "Path"; 

    // Array of sizes for image configuration. Default sizes: [480, 640, 768, 1024, 1280, 1536]
    options.Sizes = [640, 1024, 1280]; // Recommended [xs, sm, md, lg, xl, 2xl, ...] to Covers common screen widths for responsive design

    // Default quality for processed images (Range: 15-100). Default value: 75
    options.DefaultQuality = 70; // Recommended 70-80 (Good balance between quality and size)

    // Default file format for processed images (e.g., "webp", "jpeg"). Default: "webp"
    options.DefaultFileFormat = FileFormat.webp; // Recommended default: FileFormat.webp (Offers superior compression and quality where supported)
    
    // Aspect ratio width for images. Default: 1.0
    options.AspectWidth = 1.0; // Recommended default: 1.0 (Maintain original aspect ratio by default)

    // Aspect ratio height for images. Default: 1.0
    options.AspectHeight = 1.0; // Recommended default: 1.0 (Maintain original aspect ratio by default)

    // Absolute expiration time for cached images, relative to now. Default: 720 hours (30 days)
    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(720); 

    // Sliding expiration time for cached images. Default: null (disabled)
    options.SlidingExpiration = null;
});
```

### Map Runtime Endpoint 

Call the extension method to configure static file serving for the optimized images.

```csharp
app.MapBlazorImageRuntime();
```

example to use:
```csharp
// Add BlazorImage Dashboard
app.MapBlazorImageDashboard("/blazor/images");

// 👇 Add this line to serve optimized images
app.MapBlazorImageRuntime();

app.MapStaticAssets();
```

### Dashboard Endpoints 

The Blazor Image library provides dedicated endpoints to help you manage the cached optimized images. This allows for actions like clearing the cache if needed.


```csharp
app.MapBlazorImageDashboard("/endpoints/path");
```

### Blazor Integration
To finalize the integration, include the BlazorImage stylesheet and script within your App.razor file:

CSS:

```html
<link rel="stylesheet" href="@Assets["AssemblyName.styles.css"]" />
```
 JS:

 ```html
<script src="_content/BlazorImage/BlazorImage.min.js"></script>
```
 
 
## `Image` Component

To utilize the `<Image>` component in your Blazor application, simply reference it in your `.razor` files. Here's a basic example:

```razor
<Image Src="/images/my-image.jpg" Alt="A beautiful landscape" Width="200" Height="200" />
```

This will render an optimized versions of the image located at `/images/my-image.jpg`.

**Key Parameters:**

The `<Image>` component offers several parameters to customize its behavior:

* **`Src`** (required): The path to the original image file. BlazorImage will handle the optimization.
* **`Alt`** (required): Alternative text for the image, crucial for accessibility.
* **`Fill`** (optional, boolean): If `true`, the image will try to fill its parent container while maintaining its aspect ratio. Defaults to `false`.
* **`Width`, `Height`**: Required for fixed-size images (Fill="false"). Not used if Fill="true".
* **`Priority`** (optional, boolean): Enables or disables lazy loading for the image. Defaults to `false`. Set to `true` for images that are immediately visible on page load.
* **`Title`** (optional, string): The title attribute for the image.
* **`CssClass`** (optional, string): Apply custom CSS classes to the image.
* **`Style`** (optional, string): Apply inline styles to the image.
* **`Quality`** (optional, int): The desired quality of the optimized image (15-100). Defaults to the library's configured default.
* **`Format`** (optional, `FileFormat` enum): The desired output format for the optimized image (e.g., `FileFormat.webp`, `FileFormat.jpeg`, `FileFormat.png`, `FileFormat.avif`). Defaults to the library's configured default.
    * *Note:* Generating **FileFormat.avif** images might require a second build or processing step in some environments
* **`Sizes`** (optional, string): The sizes attribute for responsive images.	
* **`Caption`** (optional, string): Text to display as a caption below the image.
* **`CaptionClass`** (optional, string): Apply custom CSS classes to the image caption.
* **`DefaultSrc`** (optional, string): Path to a default image to display if the original image fails to load.
* **`EnableDeveloperMode`** (optional, boolean): Enables a developer information panel (likely for debugging).
* **`EnableInteractiveState`** (optional, boolean): Enables interactive state for the component.
* **`AdditionalAttributes`** (optional, Dictionary<string, object>): Allows you to pass any other HTML attributes directly to the underlying `<img>` tag.

### Usage Examples:

**Filling the container:**

```razor
<div style="width: 400px; height: 400px;">
    <Image Src="/images/my-wide-image.jpg" Alt="Wide image" Fill="true" />
</div>
```

**Specifying width and height:**

```razor
<Image Src="/images/my-logo.png" Alt="Company logo" Width="80" Height="80" />
```

**Disabling lazy loading for a hero image:**

```razor
<Image Src="/images/hero.jpg" Alt="Hero image" Priority="true" />
```

**Requesting a specific format and quality:**

```razor
<Image Src="/images/high-quality.png" Alt="High quality image" Format="FileFormat.png" Quality="80" />
```

**Sizes Attribute:**

```razor
<div className="relative">
    <div class="w-44 h-44 md:w-52 md:h-52">
        <Image Src="/images/avatar.jpg"
                Alt="Avatar image"
                Sizes="(max-width: 768px) 8rem, 13rem"
                CssClass="rounded-full object-cover"
                Fill="true"
                Priority="true" />
    </div>
</div>
```

**Adding a caption:**

```razor
<Image Src="/images/product.jpg" Alt="Product shot" Caption="The latest product" CaptionClass="product-caption" />
```

**Using a default image:**
```razor
<Image Src="/images/non-existent.jpg" Alt="Fallback image" DefaultSrc="/images/default.jpg" />
```

By utilizing these parameters, you can effectively manage and optimize images within your Blazor applications using the `<Image>` component.

## 📑 License
BlazorImage is licensed under the MIT License.  
