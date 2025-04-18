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
-  and more ...

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

* **`Src`** (required): The path to the original image file. BlazorImage will handle the optimization.
* **`Alt`** (required): Alternative text for the image, crucial for accessibility.
* **`Fill`** (optional, boolean): If `true`, the image will try to fill its parent container while maintaining its aspect ratio. Defaults to `false`.
* **`Width`, `Height`**: Required for fixed-size images (Fill="false"). Not used if Fill="true".
* **`Priority`** (optional, boolean): Enables or disables lazy loading for the image. Defaults to `false`. Set to `true` for images that are immediately visible on page load.
* **`Title`** (optional, string): The title attribute for the image.
* **`CssClass`** (optional, string): Apply custom CSS classes to the image **(Do not use class attr)**.
* **`Style`** (optional, string): Apply inline styles to the image **(Do not use style attr)**.
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
This project is licensed under the MIT License. See the [LICENSE](LICENSE.txt) for full details.

## Keywords

`blazor image optimization`, `blazor responsive images`, `blazor webp`, `dotnet image compression`, `blazor image component`, `blazor image resizing`, `static image optimization`, `blazor lazy loading images`, `blazor avif`, `aspnet image optimization`, `razor image library`
