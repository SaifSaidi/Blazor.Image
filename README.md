# BlazorImage - Blazor Images Optimization
![BlazorImage](https://github.com/user-attachments/assets/355bdc29-2f02-41c9-8ba6-9095bb9d7961)

BlazorImage is a powerful image optimization library for Blazor applications, supporting both static rendering and interactive server-side scenarios. It provides seamless image optimization with lazy loading, multiple format support, and responsive image handling.

## Features


- ⚡ Static/Interactive rendering support
- 🚀 Automatic image optimization
- 🗜️ Compressing images to reduce size
- 🖼️ Multiple format support (WebP, JPEG, PNG, AVIF)
- 🔄 Lazy loading built-in
- 📱 Responsive images with art direction
- 🗄️ Long Live Time Cache with LiteDb support for revalidating cached images
- 🛠️ Developer mode for debugging
- 🎨 Customizable image quality 
- 🖌️ Custom CSS classes and styles
- 📦 Lightweight and easy to use
- 📊 Progress tracking for image processing
- 📅 Cache management for processed images
- 📈 Image configuration for different sizes
- 📝 Image aspect ratio control

## Prerequisites

- [.NET 9.0](https://dotnet.microsoft.com/en-us/download)

## Limitations

- Only dotnet 9.0 is supported or higher.
- Local images only. Remote images are not supported yet.
- No support for Blazor WebAssembly yet.

---

## Installation

To install BlazorImage, add the following package reference to your project file:

```xml 
dotnet add package BlazorImage --version 1.0.2
```
or
```xml
<PackageReference Include="BlazorImage" Version="1.0.2" />
```
Then, add the following namespace to your `_Imports.razor` file:

```csharp
using BlazorImage;
```

## Setup Services

To use BlazorImage in your Blazor application, you need to register the required services:

```csharp
builder.Services.AddBlazorImage();
```


configure the services with the following options:
```csharp
builder.Services.AddBlazorImage(options =>
{
    : Default quality for processed images (15-100). Default Value = 75
    options.DefaultQuality = 80;

    : Default file format for processed images (e.g., "webp", "jpeg"). Default is "webp
    options.DefaultFileFormat = FileFormat.webp;

    : Path for storing processed images. Default is "_optimized".
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

### Endpoints (Optional)

Images Cache Management Endpoints are available for managing cached images. To use these endpoints, add the following code:

```csharp
app.MapBlazorImage("/blazor/images");
```


### Add BlazorImage script to App.razor
```html
<script src="_content/BlazorImage/BlazorImage.min.js"></script>
```

### CSS Scoped
```html
<link rel="stylesheet" href="@Assets["Assembly.styles.css"]" />
```

---

## Using `Image` Component

The `Image.razor` component is used to display optimized images.

#### Parameters

- `Src` (required): The source URL of the image (Local source only).
- `Alt` (required): The alt text for the image.
- `Width?`: The width of the image.
- `Height?`: The height of the image.
- `Fill?`: Set multiple image sources for different display conditions
- `EnableInteractiveState`: A boolean indicating if interactive state should be enabled **(default is `false`)**.
- `LazyLoading`: A boolean indicating if lazy loading should be enabled **(default is `true`)**.
- `Title?`: The title of the image.
- `CssClass?`: Additional CSS classes for the image.
- `WrapperClass?`: CSS classes for the wrapper element.
- `Style?`: Inline styles for the image.
- `WrapperStyle?`: Inline styles for the wrapper element.
- `Quality?`: The quality of the image **(default is 75)**.
- `Format?`: The format of the image (`webp`, `jpeg`, `png`, `avif`), **(default is `FileFormat.webp`)**.
- `Sizes`: The sizes attribute for responsive images, **(default is `"(min-width: 1024px) 1024px, 100vw"`)**.
- `EnableDeveloperMode`: A boolean indicating if developer mode should be enabled **(default is `false`)**.
- `AdditionalAttributes`: Additional attributes for the image element.
 
#### Example

```html
<Image Src=/image.jpg"
        Alt="Example Image"
        Width="800"
        Height="600"
        LazyLoading="true"
        Title="Example Image Title"
        CssClass="custom-image-class"
        WrapperClass="custom-wrapper-class"
        Style="border: 1px solid #ccc;"
        WrapperStyle="padding: 10px;"
        Quality="75"
        Format="FileFormat.webp"
        Sizes="(min-width: 1024px) 1024px, 100vw" 
        EnableDeveloperMode="true" />
``` 

## Using `Picture` Component

The `Picture.razor` component is used to display optimized images with support for multiple sources with figure.

#### Parameters

- `Src` (required): The source URL of the image (Local source only).
- `Alt` (required): The alt text for the image.
- `Width?`: The width of the image.
- `Height?`: The height of the image.
- `Fill?`: Set multiple image sources for different display conditions
- `EnableInteractiveState`: A boolean indicating if interactive state should be enabled **(default is `false`)**.
- `LazyLoading`: A boolean indicating if lazy loading should be enabled **(default is `true`)**.
- `Title?`: The title of the image.
- `CssClass?`: Additional CSS classes for the image.
- `WrapperClass?`: CSS classes for the wrapper element.
- `Style?`: Inline styles for the image.
- `WrapperStyle?`: Inline styles for the wrapper element.
- `Quality?`: The quality of the image **(default is 75)**.
- `Format?`: The format of the image (`webp`, `jpeg`, `png`, `avif`), **(default is `FileFormat.webp`)**.
- `Sizes`: The sizes attribute for responsive images, **(default is `"(min-width: 1024px) 1024px, 100vw"`)**.
- `EnableDeveloperMode`: A boolean indicating if developer mode should be enabled **(default is `false`)**.
- `AdditionalAttributes`: Additional attributes for the image element.
- `Caption`: The caption for the image.
- `CaptionClass?`: Additional CSS classes for the caption.
 
#### Example

```html

<Picture Src="/image.jpg"
        Alt="Example Image"
        Caption="Example Image Caption"
        Width="800"
        Height="600"
        LazyLoading="true"
        Title="Example Image Title"
        CssClass="custom-image-class"
        WrapperClass="custom-wrapper-class"
        Style="border: 1px solid #ccc;"
        WrapperStyle="padding: 10px;"
        Quality="75"
        Format="FileFormat.webp"
        Sizes="(min-width: 1024px) 1024px, 100vw"
        CaptionClass="custom-caption-class" 
        EnableDeveloperMode="true" />
```

## License
 <img width="36" height="auto" src="https://github.com/user-attachments/assets/09c77990-a1ed-4564-849c-1dc92ba8e64d" /> 
BlazorImage is licensed under the MIT License.
