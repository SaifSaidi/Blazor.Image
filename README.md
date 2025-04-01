# <img src="banner.png" alt="BlazorImage library Banner" width="100%" height="auto">

[![NuGet version (BlazorImage)](https://img.shields.io/nuget/v/BlazorImage.svg?style=flat-square)](https://www.nuget.org/packages/BlazorImage/)  [![NuGet downloads (BlazorImage)](https://img.shields.io/nuget/dt/BlazorImage.svg?style=flat-square)](https://www.nuget.org/packages/BlazorImage/)
## BlazorImage: Effortless Image Optimization for Blazor

BlazorImage is a robust library designed to simplify image optimization in your Blazor applications. It seamlessly supports both static and interactive server-side rendering, offering drop-in replacements for standard HTML image elements.

* **`<Image>` Component:** A direct replacement for the native `<img>` element.
    * **Simple Usage:** Achieve instant optimization with a single line:
        ```html
        <Image Src="/image.jpg" Alt="Example Image" Fill="true" />
        ```
* **`<Picture>` Component:** A powerful alternative to the native `<picture>` element, complete with a `<figure>` wrapper for enhanced semantic structure and art direction.
    * **Easy Implementation:** Get started with optimized responsive images in just one line:
        ```html
        <Picture Src="/image.jpg" Alt="Example Image" Caption="Example Image Caption" Fill="true" />
        ```
## Features

* **🖼️ Optimized Static Assets**: Generates highly optimized images directly as static assets for blazing-fast loading.
* **⚡ Versatile Rendering:** Supports both Static and Interactive rendering modes for maximum flexibility in your Blazor apps.
* **🚀 Automatic image optimization**
* **🗜️ Compressing images to reduce size**
* **🖼️ Multiple format support (WebP, JPEG, PNG, AVIF)**
* **🔄 Lazy Loading:** Improves initial page load times by loading images only when they are visible to the user
* **🗄️ Intelligent Caching:** Implements Long Live Time Caching with LiteDb support for efficient revalidation of cached images, ensuring users always see the latest content without unnecessary re-processing.
* **🛠️ Developer mode**
* **📅 Cache Management:** Provides tools for easy management of processed images within the cache.
 
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
dotnet add package BlazorImage --version x.x.x
```

Using NuGet Package Manager:

```xml
<PackageReference Include="BlazorImage" Version="x.x.x" />
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
    // Default quality for processed images (Range: 15-100). Default value: 75
    options.DefaultQuality = 80; // Recommended 70-80 (Good balance between quality and size)

    // Default file format for processed images (e.g., "webp", "jpeg"). Default: "webp"
    options.DefaultFileFormat = FileFormat.webp; // Recommended default: FileFormat.webp (Offers superior compression and quality where supported)

    // Path for storing processed images. Default: "_optimized"
    options.Dir = "Path"; 

    // Array of sizes for image configuration. Default sizes: [480, 640, 768, 1024, 1280, 1536]
    options.ConfigSizes = [640, 1024, 1280]; // Recommended [xs, sm, md, lg, xl, 2xl, ...] to Covers common screen widths for responsive design

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

### Endpoints 

The Blazor Image library provides dedicated endpoints to help you manage the cached optimized images. This allows for actions like clearing the cache if needed.


```csharp
app.MapBlazorImage("/endpoints/path");
```

### Blazor Integration
To finalize the integration, include the BlazorImage stylesheet and script within your App.razor file:

```html
<link rel="stylesheet" href="@Assets["Assembly.styles.css"]" />
<script src="_content/BlazorImage/BlazorImage.min.js"></script>
```
 


## `Image` Component

The `<Image>` component serves as a direct replacement for the native `<img>` element, providing automatic image optimization.

#### Parameters

| Parameter              | Type                | Description                                                                                                | Default Value         |
|------------------------|---------------------|------------------------------------------------------------------------------------------------------------|-----------------------|
| `Src`                  | `string`            | **Required.** The local source URL of the image.                                                            |                       |
| `Alt`                  | `string`            | **Required.** The alternative text for the image.                                                        |                       |
| `Quality`              | `int?`              | The compression quality of the image (15-100).                                                               | `75`                  |
| `Format`               | `FileFormat?`       | The desired output format of the image (`webp`, `jpeg`, `png`, `avif`).                                     | `FileFormat.webp`     |
| `Width`                | `int?`              | The desired width of the image.                                                                            | `null`                |
| `Height`               | `int?`              | The desired height of the image.                                                                           | `null`                |
| `Fill`                 | `bool?` | Set multiple image sources for different screens | `null`                |
| `Sizes`                | `string`            | The `sizes` attribute for responsive images.                                                               | `"(min-width: 1024px) 1024px, 100vw"` |
| `LazyLoading`          | `bool`              | Indicates if lazy loading should be enabled.                                                                 | `true`                |
| `EnableInteractiveState`| `bool`              | Indicates if interactive state should be enabled.                                                           | `false`               |
| `Title`                | `string?`           | The title attribute of the image.                                                                            | `null`                |
| `CssClass`             | `string?`           | Additional CSS classes to apply to the image element.                                                        | `null`                |
| `WrapperClass`         | `string?`           | CSS classes to apply to the wrapping `<div>` element (if any).                                             | `null`                |
| `Style`                | `string?`           | Inline styles to apply to the image element.                                                              | `null`                |
| `WrapperStyle`         | `string?`           | Inline styles to apply to the wrapping `<div>` element (if any).                                             | `null`                |
| `EnableDeveloperMode`  | `bool`              | Enables developer mode, which might provide additional info. | `false`               |
| `AdditionalAttributes` | `Dictionary<string, object>?` | Additional HTML attributes to be applied to the `<img>` element.                                   | `null`                |


#### Example

```html
<Image Src=/image.jpg" Alt="Example Image" Fill="true" />
``` 

## `Picture` Component

The `<Picture>` component extends the functionality of the native `<picture>` element, providing optimized image delivery with support for multiple sources and art direction within a `<figure>` wrapper.

#### Example

```html
<Picture Src="/images/example.jpg" Alt="Example Image" Caption="Example Image Caption" Fill="true" />
```

## 📑 License
BlazorImage is licensed under the MIT License.  
