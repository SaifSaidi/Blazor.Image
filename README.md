# Blazor Image [![NuGet version (BlazorImage)](https://img.shields.io/nuget/v/BlazorImage.svg?style=flat-square)](https://www.nuget.org/packages/BlazorImage/) [![NuGet downloads (BlazorImage)](https://img.shields.io/nuget/dt/BlazorImage.svg?logo=nuget&label=nuget%20downloads&color=ff5c9b)](https://www.nuget.org/packages/BlazorImage)

**Blazor Image** is a powerful image optimization library for [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) that automates compression, responsive sizing, and caching of static images (`.jpg`, `.png`, `.webp`, `.avif`)—all in one component.


Easily deliver optimized images with a single line:

```razor
<Image Src="/images/sample.jpg" Alt="Descriptive alt text" Width="300" Height="200" Priority="true"  />
```

> ⭐️ If **BlazorImage** helped you, please [star it on GitHub](https://github.com/SaifSaidi/Blazor.Image) to support visibility and development.


---

## 🚀 Features

* 📦 **Optimized Images** – Compress JPEG, PNG, WebP, and AVIF with up to 90% size reduction.
* 💤 **Lazy Loading + Placeholders** – Enhance performance and loading speed.
* 🖼 **Flexible Formats** – Output WebP, JPEG, PNG, or AVIF.
* ⚡ **Smart Caching** – Efficient long-term caching with auto revalidation.
* 📱 **Responsive Support** – Define sizes/aspect ratios for adaptive layouts.
* ♿ **Accessible & SEO-Friendly** – Supports alt text, captions, semantics.
* 🧠 **Interactive States** – Enable state-aware behavior via `EnableInteractiveState`.
 
---

## 🛠 Getting Started

### Requirements

* [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download) or later
* Blazor Server supported (Blazor WebAssembly not yet)

> 🔒 *Supports local images only. Remote support coming soon.*

### 📦 Install via NuGet

```bash
dotnet add package BlazorImage --version 1.0.5
```

Or add manually:

```xml
<PackageReference Include="BlazorImage" Version="1.0.5" />
```

### 🔧 Setup

**In \_Imports.razor:**

```razor
@using BlazorImage
```

**In Program.cs:**


```csharp
builder.Services.AddBlazorImage();
```

configure BlazorImage options within the AddBlazorImage method:

```csharp
builder.Services.AddBlazorImage(options =>
{
    // Output directory for optimized images (relative to wwwroot). Default: "_optimized"
    options.OutputDir = "Path";

    // Responsive widths for generated image variants.
    // Recommended: [xs, sm, md, lg, xl, 2xl, ...] to cover common breakpoints.
    // Default: [480, 640, 768, 1024, 1280, 1536]
    options.Sizes = [640, 1024, 1280];

    // Default image quality (15–100). Lower means more compression.
    // Default: 75 | Recommended: 70–80 (Good balance of quality and file size)
    options.DefaultQuality = 70;

    // Default image output format (jpeg, png, webp, avif).
    // Default: webp | Recommended: FileFormat.webp (widely supported and efficient)
    options.DefaultFileFormat = FileFormat.webp;
});

```

**Map required middleware in**:

```csharp
app.MapBlazorImageRuntime();
```

**In App.razor:**

```html
<link rel="stylesheet" href="AssemblyName.styles.css" />
<script src="_content/BlazorImage/BlazorImage.min.js"></script>
```

### 🧪 Optional: Dashboard UI

```csharp
#if DEBUG // Recommended to enable only in development
app.MapBlazorImageDashboard("/blazor-image-admin"); // Choose your preferred path
#endif
```

--- 
 
## `<Image>` Component

<div style="margin:1rem 0; padding: 1rem; border: 1px solid #f5c6cb; border-radius: 0.375rem;">

>**⚠️ Important:** Use `CssClass` instead of `class`, and `Style` instead of `style` for all styling.

</div>



Use the `<Image>` component to render optimized, responsive, and accessible images:

```razor
<Image Src="/images/sample.jpg" Alt="Descriptive alt text" Width="300" Height="200" />
```

### `<Image>` Component Parameters

- **`Src`** (required):  
  The path to the original image file. BlazorImage will handle the optimization.

- **`Alt`** (required):  
  Alternative text for the image, crucial for accessibility and SEO.

- **`Width`, `Height`** (optional, `int?`):  
  Set fixed image dimensions in pixels.

- **`Fill`** (optional, `bool`):  
  If `true`, the image fills its container while maintaining aspect ratio. Defaults to `false`.

    - **`AspectRatio`** (optional, `(int, int)`):        
       Used when `Fill="true"` to preserve image ratio.
- **`Priority`** Eager load image (optional, `bool`):  
  If `true`, the image loads eagerly. Defaults to `false`.

- **`DefaultSrc`** (optional, `string`):  
  Fallback image to override the error messages.

- **`Quality`** (optional, `int?`):  
  Sets optimized image quality (15–100). Defaults to library setting.

- **`Format`** (optional, `FileFormat?`):  
  Desired image output format (`webp`, `jpeg`, `png`, `avif`, etc.).  
  _Note: AVIF may require extra build steps in some environments._

- **`Sizes`** (optional, `string`):  
  Sets the `sizes` attribute for responsive behavior.

- **`CssClass`** (optional, `string`):  
  CSS classes to apply to the `<img>` tag.  
  _Use this instead of `class`._

- **`Style`** (optional, `string`):  
  Inline styles for the `<img>` tag.  
  _Use this instead of `style`._

- **`Caption`** (optional, `string`):  
  Text to display below the image.

- **`CaptionClass`** (optional, `string`):  
  CSS class for styling the caption.


- **`EnableInteractiveState`** (optional, `bool`):  
  Enables state interactivity for the image (e.g., loading, error state handling).

- **`Id`** (optional, `string`):  
  Custom ID for the image element. Used internally or for anchoring, preload.

- **`AdditionalAttributes`** (optional):  
  Add any additional HTML attributes via a `Dictionary<string, object>`.  
  These are applied directly to the `<img>` element.

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

**Eager loading with `preload`:**
```razor
<HeadContent>
    <SectionOutlet SectionName="hero-image-id" /> <!-- Set Image Id -->
</HeadContent>

<Image
    Id="hero-image-id" <!-- Add a unique image id -->
    Priority="true"    <!-- Enables priority loading and preload -->
    Src="/hero-image.jpg"
    Alt="Hero image"
    Width="1200"
    Height="600" />
```


**Custom format + quality:**

```razor
<Image Src="/images/preview.png" Alt="Preview" Format="FileFormat.png" Quality="85" />
```

**Sizes Attribute:**

```razor
 <Image Src="/images/avatar.jpg" Alt="Avatar image" Sizes="(max-width: 768px) 8rem, 13rem" CssClass="rounded-full object-cover"  Fill="true" Priority="true" />
```

**Aspect Ratio:**
```razor
<Image Src="/images/landscape.jpg" Alt="Landscape" AspectRatio="@(16,9)" Fill="true" />
```

**Adding a caption:**

```razor
<Image Src="/images/product.jpg" Alt="Product shot" Caption="The latest product" CaptionClass="product-caption" />
```

---

##  Demo Project

Run the `BlazorImage.Demo` app to see examples in action.

### Run Instructions

1. Clone the repository
2. Install [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download) or newer
3. Build the solution
4. Set `BlazorImage.Demo` as startup project
5. Press F5 or run

> Ensure `wwwroot` has sample images for demo display.

---

## 📄 License

MIT License. See [LICENSE.txt](LICENSE.txt) for details.

---

## 🔍 Keywords

blazor, image optimization, responsive images, lazy loading, webp, avif, jpeg, dotnet, blazor component, caching, middleware, seo, accessibility
