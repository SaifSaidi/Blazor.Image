# BlazorImage - Blazor Images Optimization

BlazorImage is a powerful image optimization library for Blazor applications, supporting both static rendering and interactive server-side scenarios. It provides seamless image optimization with lazy loading, multiple format support, and responsive image handling.

## Features

- 🚀 Automatic image optimization
- 🖼️ Multiple format support (WebP, JPEG, PNG, AVIF)
- 🔄 Lazy loading built-in
- 📱 Responsive images with art direction
- ⚡ Static/Interactive rendering support
- 🛠️ Developer mode for debugging
- 🎨 Customizable image quality
- 📏 Image dimensions control
- 🖌️ Custom CSS classes and styles
- 📦 Lightweight and easy to use
- 📊 Progress tracking for image processing

## Prerequisites

- [.NET 9.0](https://dotnet.microsoft.com/en-us/download)

## Limitations

- BlazorImage is new and may have some limitations.
- Local images only. Remote images are not supported yet.
- Only dotnet 9.0 is supported or higher.

## Installation

To install BlazorImage, add the following package reference to your project file:

```xml
<PackageReference Include="BlazorImage" Version="1.0.1" />
```
Then, add the following namespace to your `_Imports.razor` file:

```csharp
using BlazorImage;
```

## Setup Services

To use BlazorImage in your Blazor application, you need to register the required services:

```csharp
services.AddBlazorImage();
```

### Endpoints (Optional, Development Stage)

Images Cache Management Endpoints are available for managing cached images. To use these endpoints, add the following code:

```csharp
app.MapBlazorImage("/blazor/images");
```

### Configuration

configure the services with the following options:
```csharp
services.AddBlazorImage(options =>
{
    : Default quality for processed images (15-100). Default Value = 75
    options.DefaultQuality = 80;

    : Default file format for processed images (e.g., "webp", "jpeg"). Default is "webp
    options.DefaultFileFormat = FileFormat.webp;

    : Path for storing processed images. Default is "_blazor".
    options.Dir = "_output";

    : Array of sizes for image configuration. Default [480, 640, 768, 1024, 1280, 1536] sizes
    options.ConfigSizes = [640, 1024, 1280];

    : Aspect ratio width for images. Default is 1.0
    options.AspectWidth = 1.0'

    : Aspect ratio height for images. Default is 1.0
    options.AspectHeight = 1.0;

    : Absolute expiration time relative to now for cached images. Default is 720 hours (30 days).
    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(720);

    : Sliding expiration time for cached images. Defualt is null.
    options.SlidingExpiration = null;
});
```

## Usage

### Add script to App.razor
```html
<script src="_content/BlazorImage/BlazorImage.min.js"></script>
```

### CSS
```html
<link rel="stylesheet" href="@Assets["Assembly.styles.css"]" />
```

### Using `Image` Component

The `Image.razor` component is used to display optimized images.

#### Parameters

- `Src` (required): The source URL of the image (Local source only).
- `Alt` (required): The alt text for the image.
- `Width`: The width of the image.
- `Height`: The height of the image.
- `Fill`: A boolean indicating if the image should fill its container.
- `EnableInteractiveState`: A boolean indicating if interactive state should be enabled. (default is `false`)
- `LazyLoading`: A boolean indicating if lazy loading should be enabled (default is `true`).
- `Title`: The title of the image.
- `CssClass`: Additional CSS classes for the image.
- `WrapperClass`: CSS classes for the wrapper element.
- `Style`: Inline styles for the image.
- `WrapperStyle`: Inline styles for the wrapper element.
- `Quality`: The quality of the image.
- `Format`: The format of the image (`webp`, `jpeg`, `png`, `avif`).
- `Sizes`: The sizes attribute for responsive images.
- `EnableDeveloperMode`: A boolean indicating if developer mode should be enabled.
- `AdditionalAttributes`: Additional attributes for the image element.
- 
#### Example

```html
<Image Src=/image.jpg" Alt="Example Image" Width="800" Height="600" LazyLoading="true" Title="Example Image Title" CssClass="custom-image-class" WrapperClass="custom-wrapper-class" Style="border: 1px solid #ccc;" WrapperStyle="padding: 10px;" Quality="80" Format="FileFormat.jpeg" Sizes="(min-width: 1024px) 1024px, 100vw" EnableDeveloperMode="true" />
``` 
### Using `Picture` Component

The `Picture.razor` component is used to display optimized images with support for multiple sources with figure.

#### Parameters

- `Src` (required): The source URL of the image (Local source only).
- `Alt` (required): The alt text for the image.
- `Width`: The width of the image.
- `Height`: The height of the image.
- `Fill`: A boolean indicating if the image should fill its container.
- `EnableInteractiveState`: A boolean indicating if interactive state should be enabled. (default is `false`)
- `LazyLoading`: A boolean indicating if lazy loading should be enabled (default is `true`).
- `Title`: The title of the image.
- `CssClass`: Additional CSS classes for the image.
- `WrapperClass`: CSS classes for the wrapper element.
- `Style`: Inline styles for the image.
- `WrapperStyle`: Inline styles for the wrapper element.
- `Quality`: The quality of the image.
- `Format`: The format of the image (`webp`, `jpeg`, `png`, `avif`).
- `Sizes`: The sizes attribute for responsive images.
- `CaptionClass`: CSS classes for the caption element.
- `Caption`: The caption text for the image.
- `EnableDeveloperMode`: A boolean indicating if developer mode should be enabled.
- `AdditionalAttributes`: Additional attributes for the image element.

#### Example

```html
<Picture Src="image.jpg" Alt="Example Image" Width="800" Height="600" LazyLoading="true" Title="Example Image Title" CssClass="custom-image-class" WrapperClass="custom-wrapper-class" Style="border: 1px solid #ccc;" WrapperStyle="padding: 10px;" Quality="80" Format="FileFormat.jpeg" Sizes="(min-width: 1024px) 1024px, 100vw" CaptionClass="custom-caption-class" Caption="This is an example caption." EnableDeveloperMode="true" />
```


## License

BlazorImage is licensed under the MIT License.
