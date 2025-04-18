# ![BlazorImage – Blazor Image Optimization Library](banner.png)

[![NuGet version (BlazorImage)](https://img.shields.io/nuget/v/BlazorImage.svg?style=flat-square)](https://www.nuget.org/packages/BlazorImage/)
[![NuGet downloads (BlazorImage)](https://img.shields.io/nuget/dt/BlazorImage.svg?logo=nuget&label=nuget%20downloads&color=ff5c9b)](https://www.nuget.org/packages/BlazorImage)

## BlazorImage – Image Optimization for Blazor (.NET)
**BlazorImage** is a powerful image optimization library for [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) that automates compression, responsive sizing, and caching of static images (`.jpg`, `.png`, `.webp`, `.avif`)—all in one component.


Easily deliver optimized images with a single line:

```razor
<Image Src="/images/sample.jpg" Alt="Descriptive alt text" Width="300" Height="200" />
```

## Features

- **Optimized Images:** Compress JPEG, PNG, WebP, and AVIF images with 70–90% size reduction.
- **Responsive Support:** Auto-generate sizes for all screen widths.
- **Lazy Loading & Placeholders:** Improve page load speed and UX.
- **Format Flexibility:** Choose output formats (WebP, JPEG, PNG, AVIF).
- **Caching & Revalidation:** Long-term caching with efficient revalidation ensures fresh content without re-processing.
- add more

## Getting Started

### ✅ Requirements

- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download) or later
- Blazor Server App (Blazor WebAssembly is **not yet supported**)

> ✅ *Currently supports local images only. Remote image support is coming soon.*

---

### 📦 Installation

**Using .NET CLI:**

```bash
dotnet add package BlazorImage --version 1.0.3
```

**Or add a reference manually:**
```bash
<PackageReference Include="BlazorImage" Version="1.0.3" />
```


### 🧩 Setup

In **_Imports.razor**:

```csharp
@using BlazorImage
```

In **Program.cs**:

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

    // Absolute expiration time for cached images, relative to now. Default: 720 hours (30 days)
    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(720); 

    // Sliding expiration time for cached images. Default: null (disabled)
    options.SlidingExpiration = null;
});
```

Map required middleware in **Program.cs**:

```csharp
app.MapBlazorImageRuntime();
```

example to use:
```csharp
// 👇 Add this line to serve optimized images
app.MapBlazorImageRuntime();
app.MapStaticAssets(); // For .NET 9
```

Include required assets in **App.razor**:

CSS:

```html
<link rel="stylesheet" href="@Assets["AssemblyName.styles.css"]" />
```

 JS:
 ```html
<script src="_content/BlazorImage/BlazorImage.min.js"></script>
```

 
### Dashboard Endpoint

Expose the image cache management UI:

Enable with:

```csharp
app.MapBlazorImageDashboard("/endpoints/path");
```
 
## 🖼️ `<Image>` Component

Use the `<Image>` component to render optimized, responsive, and accessible images:

```razor
<Image Src="/images/sample.jpg" Alt="Descriptive alt text" Width="300" Height="200" />
```
###  `<Image>` Component Parameters

> **Note:** Use `CssClass` instead of `class`, and `Style` instead of `style` for all styling.

| Parameter              | Type       | Description                                                                                   |
|------------------------|------------|-----------------------------------------------------------------------------------------------|
| `Src`                  | `string`   | **Required.** Path to the original image (local only).                                        |
| `Alt`                  | `string`   | **Required.** Descriptive alt text for accessibility and SEO.                                |
| `Width` / `Height`     | `int`      | Fixed dimensions (required unless `Fill=true`).                                               |
| `Fill`                 | `bool`     | If `true`, fills container size while maintaining aspect ratio.                              |
| `Priority`             | `bool`     | If `true`, disables lazy loading and loads image eagerly.                                     |
| `Quality`              | `int`      | Overrides compression quality (15–100).                                                       |
| `Format`               | `FileFormat` | Output format: `webp`, `jpeg`, `png`, or `avif`.                                           |
| `Sizes`                | `string`   | Defines responsive image behavior for different screen widths.                                |
| `DefaultSrc`           | `string`   | Fallback image path if `Src` fails to load.                                                   |
| `Caption`              | `string`   | Optional caption below the image.                                                             |
| `CaptionClass`         | `string`   | Applies a CSS class to the caption.                                                           |
| `CssClass`             | `string`   | Adds a class to the `<img>` element. (**Do not use `class`**)                                 |
| `Style`                | `string`   | Inline styles for the `<img>` element. (**Do not use `style`**)                               |
| `EnableDeveloperMode`  | `bool`     | Enables dev/debug output (size info, load mode, etc.).                                        |
| `AdditionalAttributes` | `Dictionary<string, object>` | Pass custom HTML attributes to the `<img>` element.                     |

---

### 📘 Usage Examples

**Responsive with fill:**

```razor
<div style="width: 400px; height: 400px;">
    <Image Src="/images/banner.jpg" Alt="Banner" Fill="true" />
</div>
```

**Static dimensions:**

```razor
<Image Src="/images/logo.png" Alt="Logo" Width="100" Height="100" />
```

**Eager loading (hero image):**

```razor
<Image Src="/images/hero.jpg" Alt="Hero image" Priority="true" />
```

**Custom format + quality:**

```razor
<Image Src="/images/preview.png" Alt="Preview" Format="FileFormat.png" Quality="85" />
```

**Sizes Attribute:**

```razor
 <Image Src="/images/avatar.jpg" Alt="Avatar image" Sizes="(max-width: 768px) 8rem, 13rem" CssClass="rounded-full object-cover"  Fill="true" Priority="true" />
```

**Adding a caption:**

```razor
<Image Src="/images/product.jpg" Alt="Product shot" Caption="The latest product" CaptionClass="product-caption" />
```

**Using a default image:**
```razor
<Image Src="/images/non-existent.jpg" Alt="Fallback image" DefaultSrc="/images/default.jpg" />
```

##  License
This project is licensed under the MIT License. See the [LICENSE.md](LICENSE.md) for full details.

## Keywords

`blazor image optimization`, `blazor responsive images`, `blazor webp`, `dotnet image compression`, `blazor image component`, `blazor image resizing`, `static image optimization`, `blazor lazy loading images`, `blazor avif`, `aspnet image optimization`, `razor image library`
