# Blazor.Image: Blazor Images Optimization

**Deliver lightning-fast Blazor applications with perfectly optimized images, automatically.**

Blazor.Image is a powerful library designed to seamlessly integrate image optimization into your Blazor Server and Static Web Apps.  Boost your website's performance, improve SEO, and enhance user experience with efficient, server-side image processing.

## ✨ Key Features

*   **Image Compression:**  Reduces image file sizes significantly without noticeable quality loss, ensuring faster page load times and reduced bandwidth consumption.
*   **Broad Format Support:**  Effortlessly optimize images in a variety of formats, including next-gen formats like **WebP** and **AVIF**, as well as common formats like **JPEG** and **PNG**. Serve the most efficient format based on browser capabilities.
*   **Convert Images to WebP:** Easily convert existing images to the WebP format for better compression and quality.
*   **Blazor-Native Integration:**  Designed specifically for Blazor, offering a smooth and intuitive development experience in both Interactive Server and Static Site Rendering (SSR) modes.
*   **Server-Side Power:**  Leverages server-side processing for image optimization, keeping client-side load light and ensuring consistent optimization across all browsers.
*   **Static Image Serving Ready:**  Optimized images are pre-processed and ready to be served statically, ideal for performance in static Blazor applications.
*   **Lazy Loading for Performance:**  Improves initial page load by deferring the loading of off-screen images, enhancing perceived speed and user engagement.
*   **Responsive Images Made Easy:**  Automatically generates and serves appropriately sized images for different screen sizes, ensuring optimal viewing experience on all devices.
*   **Built-in Caching:**  Implements intelligent caching mechanisms to avoid redundant optimization processes, further accelerating image delivery and reducing server load.
*   **SEO-Friendly:**  Optimized images contribute to improved page speed and user experience, both crucial factors for higher search engine rankings. Properly optimized images are also more easily indexed by search engines.

## 🚀 Installation

Install Blazor.Image quickly and easily via NuGet Package Manager.

```bash
Install-Package Blazor.Image
```

Alternatively, you can add the package directly within your .NET project file:

```bash
<ItemGroup>
  <PackageReference Include="Blazor.Image" Version="[Latest Version]" />
</ItemGroup>
```


## 🛠️ Usage

Get started with Blazor.Image in just a few simple steps:

- ### *1) Register Services*
    ```bash
    builder.Services.AddBlazorImage();
    ```

    - Configuration: The AddBlazorImage method allows you to configure the image optimization settings using the ImageOptimizationConfig class.

       - config.DefaultQuality: Sets the default image quality for optimized images. Value ranges from 15 to 100 (e.g., 75 the default for a good balance between quality and compression).
       - config.DefaultFileFormat: Sets the default image optimization format. (e.g, FileFormat.webp the default), recommended webp.
       - config.Dir: Specifies the directory where optimized images will be stored, relative to your web root (e.g., "optimized-images"). If not set, a default '_optimized' directory will be used.

    ```bash
    builder.Services.AddBlazorImage(config =>
    {
        config.DefaultQuality = 70; 
        config.DefaultFileFormat = FileFormat.jpeg;  
        config.Dir = "optimized-images"; 
    });
    ```

- ### *2) Add Endpoints (Optional)*

- Map Cache Management Endpoints : Blazor.Image provides endpoints to manage the image optimization cache.  

    ```bash
    app.MapBlazorImage("/path/to");
    ```

- ### *3) Import the Namespace*
    - Add the Blazor.Image namespace to your `_Imports.razor` file to easily access throughout your Blazor project.
    ```bash
    @using Blazor.Image
    ```

## 🖼️ `<Image>` Component

The `<Image>` component offers a wide range of parameters to control image optimization, display, and behavior. Below is a detailed description of each parameter:

*   **`Src` (string, **Required**):**
    *   Specifies the source path to the original image file (local).

*   **`Alt` (string, **Required**):**
    *   Defines the alternative text for the image. Crucial for accessibility and SEO, it describes the image content to screen readers and search engines when the image cannot be displayed.

*   **`Width`, `Height` (int, Optional, recommended):**
    *   Sets the desired width and height of the image in pixels. If not specified, the component will scale to the viewport width.

*   **`EnableInteractiveState` (bool, Optional):**
    *   Determines whether the image component should enable interactive state features. Defaults to `false`. When enabled in interactive render modes, the component can leverage Blazor's interactivity for enhanced features.

*   **`LazyLoading` (bool, Optional):**
    *   Enables or disables lazy loading for the image. Defaults to `true` (lazy loading enabled). Lazy loading defers image loading until the image is about to enter the viewport, improving initial page load performance.

*   **`Title` (string, Optional):**
    *   Sets the title attribute of the image. This text typically appears as a tooltip when a user hovers their mouse over the image.

*   **`CssClass` (string, Optional):**
    *   Allows you to apply one or more CSS classes directly to the `<img>` element for styling purposes.

*   **`WrapperClass` (string, Optional):**
    *   Adds CSS class(es) to the `<figure>` wrapper element that surrounds the `<picture>` and `<img>` tags.  This is useful for applying layout or styling to the image container.

*   **`Style` (string, Optional):**
    *   Provides a way to apply inline CSS styles directly to the `<img>` element. Use this for specific, one-off styling needs.

*   **`WrapperStyle` (string, Optional):**
    *   Allows you to apply inline CSS styles to the `<figure>` wrapper element. Useful for setting wrapper-specific styles like margins or positioning.

*   **`Quality` (int, Optional):**
    *   Controls the quality of the optimized image. Accepts values from 15 to 100, where 100 is the highest quality (and largest file size). If not set, the component uses the `DefaultQuality` configured in the service registration.

*   **`Format` (FileFormat enum, Optional):**
    *   Specifies the desired output image format for optimization.  Accepts values from the `FileFormat` enum (e.g., `Webp`, `Jpeg`, `Png`, `Avif`). If not set, defaults to `Webp` or the `DefaultFileFormat` configured during service registration.

*   **`Sizes` (string, Optional):**
    *   Sets the `sizes` attribute for the `<source>` and `<img>` tags, essential for responsive images.  Defaults to `"100vw"` to make the image responsive and scale to the viewport width.  Customize this for more complex responsive behavior.

*   **`CaptionClass` (string, Optional):**
    *   Applies CSS class(es) to the `<figcaption>` element, allowing you to style the image caption.

*   **`Caption` (string, Optional):**
    *   Provides the text content for the image caption.  The caption is displayed below the image within the `<figcaption>` element, offering descriptive text related to the image.

*   **`EnableDeveloperMode` (bool, Optional):**
    *   When set to `true`, enables a developer mode that displays additional debugging information directly on the image component. Defaults to `false`. Useful during development to inspect image optimization details.

*   **`SupportPreload` (bool, Optional):**
    *   Determines whether `<link rel="preload">` tags should be added to the page's `<head>` section for this image. Preloading hints the browser to download the image earlier, potentially improving loading performance. Defaults to `false`.

*   **`SupportLDJson` (bool, Optional):**
    *   Enables the generation and inclusion of LD+JSON structured data (Schema.org markup) for the image. This can improve how search engines understand and index your images, potentially enhancing SEO. Defaults to `false`.

*   **`AdditionalAttributes` (Dictionary<string, object>, Optional):**
    *   A dictionary that allows you to pass through any other standard HTML attributes to the `<img>` tag.  This provides flexibility to add attributes not explicitly defined as parameters, such as `data-*` attributes or ARIA attributes.

**Example Usage:**

```razor
<Image Src="images/landscape.jpg"
        Alt="Beautiful mountain landscape"
        Width="800"
        Quality="70"
        Format="Webp"
        LazyLoading="true"
        CssClass="img-responsive"
        Caption="A breathtaking view from the summit"
        EnableDeveloperMode="false" />
```
## 🤝 Contributing
We welcome contributions of all kinds!  If you're interested in helping to improve Blazor.Image, please consider the following:

- Bug Reports: If you encounter any issues, please open a new issue on GitHub detailing the problem, steps to reproduce, and your environment.
- Feature Requests: Have a great idea for a new feature? Submit a feature request issue to discuss it with the community.
- Pull Requests: Code contributions are highly appreciated! Fork the repository, create a feature branch, and submit a pull request with your changes.
** Please ensure your code adheres to project coding standards and includes relevant tests.
Before contributing, please review our Contribution Guidelines for more details.

## 📜 License
Blazor.Image is released under the MIT License.  Feel free to use it in your personal and commercial projects.