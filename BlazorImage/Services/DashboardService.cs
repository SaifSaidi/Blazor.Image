using System.Collections.Immutable;
using System.Text;
using BlazorImage.Models;
using Microsoft.Extensions.Options;

namespace BlazorImage.Services
{
    internal sealed class DashboardService : IDashboardService
    {
        private readonly ILiteDatabase _db;
        private readonly DictionaryCacheDataService _dictionaryCacheData;
        private readonly string _outputDir;

        private static readonly string HtmlHead = @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta name='description' content='BlazorImage Dashboard - Monitor and manage your image cache'>
    <title>BlazorImage Dashboard</title>
    <link rel='icon' type='image/png' href='/_content/BlazorImage/icon.png' />
    <script src='https://cdn.jsdelivr.net/npm/@tailwindcss/browser@4'></script> 
    <script> 
document.documentElement.classList.toggle(
  ""dark"",
  localStorage.theme === ""dark"" ||
    (!('theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches),
);

function toggleTheme() {
  if (localStorage.theme === 'dark') {
    localStorage.theme = 'light';
    document.documentElement.classList.remove('dark');
  } else {
    localStorage.theme = 'dark';
    document.documentElement.classList.add('dark');
  }
}
    </script>
    <style type='text/tailwindcss'>
         
        :root {
          --background: 0 0% 100%;
          --foreground: 222.2 84% 4.9%;
          --card: 0 0% 100%;
          --card-foreground: 222.2 84% 4.9%;
          --popover: 0 0% 100%;
          --popover-foreground: 222.2 84% 4.9%;
          --primary: 262.1 83.3% 57.8%;
          --primary-foreground: 210 40% 98%;
          --secondary: 210 40% 96.1%;
          --secondary-foreground: 222.2 47.4% 11.2%;
          --muted: 210 40% 96.1%;
          --muted-foreground: 215.4 16.3% 46.9%;
          --accent: 210 40% 96.1%;
          --accent-foreground: 222.2 47.4% 11.2%;
          --destructive: 0 84.2% 60.2%;
          --destructive-foreground: 210 40% 98%;
          --border: 214.3 31.8% 91.4%;
          --input: 214.3 31.8% 91.4%;
          --ring: 262.1 83.3% 57.8%;
          --radius: 0.5rem;
        }
       
        .dark {
          --background: 222.2 84% 4.9%;
          --foreground: 210 40% 98%;
          --card: 222.2 84% 4.9%;
          --card-foreground: 210 40% 98%;
          --popover: 222.2 84% 4.9%;
          --popover-foreground: 210 40% 98%;
          --primary: 263.4 70% 50.4%;
          --primary-foreground: 210 40% 98%;
          --secondary: 217.2 32.6% 17.5%;
          --secondary-foreground: 210 40% 98%;
          --muted: 217.2 32.6% 17.5%;
          --muted-foreground: 215 20.2% 65.1%;
          --accent: 217.2 32.6% 17.5%;
          --accent-foreground: 210 40% 98%;
          --destructive: 0 62.8% 30.6%;
          --destructive-foreground: 210 40% 98%;
          --border: 217.2 32.6% 17.5%;
          --input: 217.2 32.6% 17.5%;
          --ring: 263.4 70% 50.4%;
        }
        
        body {
          @apply bg-gradient-to-b from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-950 text-gray-900 dark:text-gray-100 min-h-screen;
        }
        .btn {
          @apply inline-flex items-center justify-center rounded-md text-sm font-medium  transition-colors   disabled:pointer-events-none disabled:opacity-50;
        }
        
        .btn-primary {
          @apply bg-purple-600 text-white hover:bg-purple-700 dark:bg-purple-700 dark:hover:bg-purple-600;
        }
        
        .btn-outline {
          @apply border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-200;
        }
        
        .btn-sm {
          @apply h-9 px-3 rounded-md;
        }
        
        .btn-icon {
          @apply h-9 w-9 p-0;
        }
        
        .card {
          @apply bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden;
        }
        
        .badge {
          @apply inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium;
        }
        
        .badge-outline {
          @apply bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-200;
        }
        
        .badge-purple {
          @apply bg-purple-100 dark:bg-purple-900 text-purple-800 dark:text-purple-200;
        }
        
        .badge-green {
          @apply bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200;
        }
        
        .badge-amber {
          @apply bg-amber-100 dark:bg-amber-900 text-amber-800 dark:text-amber-200;
        }
        
        .badge-red {
          @apply bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200;
        }
       
    </style>
</head>";

        private static readonly string BodyStart = @"<body>
    <div class='container mx-auto px-4 py-8'>";

        private static readonly string Footer = @"<footer class='mt-12 text-center text-gray-500 text-sm'>
        <p>&copy; " + DateTime.Now.Year + @" BlazorImage. All rights reserved.</p>
    </footer>";

        private static readonly string Scripts = @"<script src='/_content/BlazorImage/dashboard.min.js'></script>";

        private static readonly string BodyEnd = @"</div>
</body>
</html>";

        public DashboardService(ILiteDatabase db, DictionaryCacheDataService dictionaryCacheData,
            IOptions<BlazorImageConfig> options)
        {
            _db = db;
            _dictionaryCacheData = dictionaryCacheData;
            _outputDir = options.Value.OutputDir.TrimStart('/');
        }

        public async ValueTask<string> DashboardDataAsync(string route)
        {
            try
            {
                var collection = _db.GetCollection<ImageInfo>(Constants.LiteDbCollection);
                collection.EnsureIndex(x => x.SanitizedName); 

                var imageInfos = collection.FindAll();
                StringBuilder sb = BlazorImageHtml(route, imageInfos);
                return await Task.FromResult(sb.ToString());
            }
            catch
            {
                return "Error loading the dashboard!";
            }
        }

        private StringBuilder BlazorImageHtml(string route, IEnumerable<ImageInfo> imageInfos)
        {
            if (string.IsNullOrEmpty(route))
            {
                throw new ArgumentException("Route cannot be null or empty.", nameof(route));
            }
            if (imageInfos == null)
            {
                throw new ArgumentNullException(nameof(imageInfos), "ImageInfos cannot be null.");
            }

            var imageInfosList = imageInfos.ToList();
            var sb = new StringBuilder(32768);
            sb.Append(HtmlHead)
              .Append(BodyStart);

            AppendHeader(sb);
            AppendTabbedInterface(sb, route, imageInfosList);

            sb.Append(Footer)
              .Append(Scripts)
              .Append(BodyEnd);

            return sb;
        }

        private static void AppendHeader(StringBuilder sb)
        {
            sb.AppendLine("        <header class='flex flex-col md:flex-row justify-between items-start md:items-center gap-4 mb-8'>");
            sb.AppendLine("            <div class='flex items-center gap-3'>");
            sb.AppendLine("                <div class='bg-gradient-to-r from-purple-600 to-blue-600 p-2 rounded-lg shadow-lg'>");
            sb.AppendLine("                    <img src='/_content/BlazorImage/icon.png' width='40' height='40' alt='BlazorImage Logo' class='h-10 w-10' />");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div>");
            sb.AppendLine("                    <h1 class='text-2xl md:text-3xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-purple-600 to-blue-600 dark:from-purple-400 dark:to-blue-400'>BlazorImage Dashboard</h1>");
            sb.AppendLine("                    <p class='text-gray-600 dark:text-gray-400 mt-1'>Monitor and manage your image cache with ease</p>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("            <div class='flex items-center gap-2'>");
            sb.AppendLine("                <a href='/' class='flex btn btn-outline btn-sm gap-2'>");
            sb.AppendLine("                    <svg xmlns='http://www.w3.org/2000/svg' class='h-4 w-4' fill='none' viewBox='0 0 24 24' stroke='currentColor'>");
            sb.AppendLine("                        <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6' />");
            sb.AppendLine("                    </svg>");
            sb.AppendLine("                    <span class='hidden sm:inline'>Home</span>");
            sb.AppendLine("                </a>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </header>");
        }

        private void AppendTabbedInterface(StringBuilder sb, string route, List<ImageInfo> imageInfosList)
        {
            // Tabs navigation
            sb.AppendLine("        <div class='mb-8'>");
            sb.AppendLine("            <div class='border-b border-gray-200 dark:border-gray-700'>");
            sb.AppendLine("                <nav class='flex -mb-px space-x-8' aria-label='Tabs'>");
             sb.AppendLine("                    <button role='tab' aria-selected='true' aria-controls='tab-cache' class='border-purple-500 text-purple-600 dark:text-purple-400 whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm'>Cache Status</button>");
            sb.AppendLine("                    <button role='tab' aria-selected='false' aria-controls='tab-images' class='border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 dark:text-gray-400 dark:hover:text-gray-300 dark:hover:border-gray-600 whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm'>Image Management</button>");
            sb.AppendLine("                </nav>");
            sb.AppendLine("            </div>");             

            // Tab content - Cache Status
            sb.AppendLine("            <div role='tabpanel' id='tab-cache' class='py-6'>");
            AppendCacheStatusTab(sb, route);
            sb.AppendLine("            </div>");

            // Tab content - Image Management
            sb.AppendLine("            <div role='tabpanel' id='tab-images' class='py-6 hidden'>");
            AppendImageTable(sb, route, imageInfosList);
            sb.AppendLine("            </div>");

            sb.AppendLine("        </div>");
        }

        private void AppendCacheStatusTab(StringBuilder sb, string route)
        {
            // Total Images
            sb.AppendLine("                <div class='card mb-4'>");
            sb.AppendLine("                    <div class='p-6'>");
            sb.AppendLine("                        <div class='flex justify-between'>");
            sb.AppendLine("                            <div>");
            sb.AppendLine("                                <p class='text-sm font-medium text-gray-500 dark:text-gray-400'>Output Folder</p>");
            sb.AppendLine($"                                <p class='text-3xl font-bold mt-1'>{_outputDir}</p>");
            sb.AppendLine("                            </div>");
            sb.AppendLine("                            <div class='p-2 bg-blue-100 dark:bg-blue-900 rounded-full h-10 w-10 flex items-center justify-center'>");
            sb.AppendLine("                                <svg xmlns='http://www.w3.org/2000/svg' class='h-5 w-5 text-blue-600 dark:text-blue-400' viewBox='0 0 20 20' fill='currentColor'>");
            sb.AppendLine("                                    <path fill-rule='evenodd' d='M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z' clip-rule='evenodd' />");
            sb.AppendLine("                                </svg>");
            sb.AppendLine("                            </div>");
            sb.AppendLine("                        </div>"); 
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            <div class='card'>");
            sb.AppendLine("                <div class='p-6 border-b border-gray-200 dark:border-gray-700'>");
            sb.AppendLine("                    <div class='flex justify-between items-center'>");
            sb.AppendLine("                        <div>");
            sb.AppendLine("                            <h3 class='text-lg font-medium text-gray-900 dark:text-gray-100'>Cache Status</h3>");
            sb.AppendLine("                            <p class='text-sm text-gray-500 dark:text-gray-400'>Tracked image attributes in cache</p>");
            sb.AppendLine("                        </div>");
            sb.AppendLine($"                        <a href='{route}/clear-data' class='btn btn-outline btn-sm flex items-center gap-2'>");
            sb.AppendLine("                            <svg xmlns='http://www.w3.org/2000/svg' class='h-4 w-4' fill='none' viewBox='0 0 24 24' stroke='currentColor'>");
            sb.AppendLine("                                <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15' />");
            sb.AppendLine("                            </svg>");
            sb.AppendLine("                            Clean Cache");
            sb.AppendLine("                        </a>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='p-6'>");
            sb.AppendLine("                    <div class='mb-4'>");
            sb.AppendLine("                        <div class='flex items-center gap-2 mb-2'>");
            sb.AppendLine("                            <div class='h-3 w-3 rounded-full bg-blue-500'></div>");
            sb.AppendLine($"                            <span class='text-sm font-medium'>Tracked Items:</span>");
            sb.AppendLine($"                            <span class='inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200'>{_dictionaryCacheData.SourceInfoCache.Count} entries</span>");
            sb.AppendLine("                        </div>"); 
            sb.AppendLine("                    </div>");

            sb.AppendLine("                    <div class='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 max-h-[400px] overflow-y-auto p-1'>");

            var cacheItems = _dictionaryCacheData.SourceInfoCache.ToList();
            if (cacheItems.Count == 0)
            {
                sb.AppendLine("                        <div class='col-span-full text-center p-8 text-gray-500 dark:text-gray-400'>");
                sb.AppendLine("                            <svg xmlns='http://www.w3.org/2000/svg' class='mx-auto h-12 w-12 text-gray-400' fill='none' viewBox='0 0 24 24' stroke='currentColor'>");
                sb.AppendLine("                                <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4' />");
                sb.AppendLine("                            </svg>");
                sb.AppendLine("                            <p class='mt-4 text-lg font-medium'>No cache entries found</p>");
                sb.AppendLine("                            <p class='mt-2'>Cache is empty or has been cleared</p>");
                sb.AppendLine("                        </div>");
            }
            else
            {
                foreach (var item in cacheItems)
                {
                    sb.AppendLine("                        <div class='p-4 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg'>");
            
                    sb.AppendLine("                            <div class='space-y-2 text-xs'>");
                    sb.AppendLine("                            <div class='flex justify-between items-center'>");
                    sb.AppendLine("                                    <span class='font-medium text-gray-500 dark:text-gray-400'>Image:</span>");

                    sb.AppendLine($"                                <img src='/{item.Value.placeholder}' alt='Placeholder' class='h-12 w-12 object-cover rounded-md' loading='lazy' />");
                    sb.AppendLine("                            </div>");
                    sb.AppendLine("                                <div class='flex justify-between'>");
                    sb.AppendLine("                                    <span class='font-medium text-gray-500 dark:text-gray-400'>Source:</span>");
                    sb.AppendLine($"                                    <span class='truncate max-w-[200px] hover:max-w-fit'>{item.Value.source}</span>");
                    sb.AppendLine("                                </div>");
                    sb.AppendLine("                                <div class='flex justify-between'>");
                    sb.AppendLine("                                    <span class='font-medium text-gray-500 dark:text-gray-400'>Quality:</span>");
                    sb.AppendLine($"                                    <span>{item.Key.Quality}</span>");
                    sb.AppendLine("                                </div>");
                    sb.AppendLine("                                <div class='flex justify-between'>");
                    sb.AppendLine("                                    <span class='font-medium text-gray-500 dark:text-gray-400'>Format:</span>");
                    sb.AppendLine($"                                    <span class='badge badge-purple'>{item.Key.Format}</span>");
                    sb.AppendLine("                                </div>");
                    sb.AppendLine("                                <div class='flex justify-between'>");
                    sb.AppendLine("                                    <span class='font-medium text-gray-500 dark:text-gray-400'>Width:</span>");
                    sb.AppendLine($"                                    <span>{(item.Key.Width == -1 ? "---" : item.Key.Width.ToString() + "px")}</span>");
                    sb.AppendLine("                                </div>");
                    sb.AppendLine("                            </div>");
                    sb.AppendLine("                        </div>");
                }
            }

            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");
        }

        private void AppendImageTable(StringBuilder sb, string route, List<ImageInfo> imageInfosList)
        {

            // Total Images
            sb.AppendLine("                <div class='card mb-4'>");
            sb.AppendLine("                    <div class='p-6'>");
            sb.AppendLine("                        <div class='flex justify-between'>");
            sb.AppendLine("                            <div>");
            sb.AppendLine("                                <p class='text-sm font-medium text-gray-500 dark:text-gray-400'>Total Images</p>");
            sb.AppendLine($"                                <p class='text-3xl font-bold mt-1'>{imageInfosList.Count}</p>");
            sb.AppendLine("                            </div>");
            sb.AppendLine("                            <div class='p-2 bg-blue-100 dark:bg-blue-900 rounded-full h-10 w-10 flex items-center justify-center'>");
            sb.AppendLine("                                <svg xmlns='http://www.w3.org/2000/svg' class='h-5 w-5 text-blue-600 dark:text-blue-400' viewBox='0 0 20 20' fill='currentColor'>");
            sb.AppendLine("                                    <path fill-rule='evenodd' d='M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z' clip-rule='evenodd' />");
            sb.AppendLine("                                </svg>");
            sb.AppendLine("                            </div>");
            sb.AppendLine("                        </div>");
            sb.AppendLine("                        <div class='mt-4 flex items-center text-sm'>");
            sb.AppendLine($"                            <span class='text-green-500 dark:text-green-400 mr-1'>{imageInfosList.Select(a=>a.ProcessedTime).OrderByDescending(a=>a!.Value).FirstOrDefault()?.ToString("yyyy-MM-dd HH:mm:ss UTC")}</span>");
            sb.AppendLine("                            <span class='text-gray-500 dark:text-gray-400'>Last processed image</span>");
            sb.AppendLine("                        </div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");

            sb.AppendLine("            <div class='card'>");
            sb.AppendLine("                <div class='p-6 border-b border-gray-200 dark:border-gray-700'>");
            sb.AppendLine("                    <div class='flex flex-col md:flex-row justify-between md:items-center gap-4'>");
            sb.AppendLine("                        <div>");
            sb.AppendLine("                            <h3 class='text-lg font-medium text-gray-900 dark:text-gray-100 flex items-center gap-2'>");
            sb.AppendLine("                                <svg xmlns='http://www.w3.org/2000/svg' class='h-5 w-5 text-purple-500' viewBox='0 0 20 20' fill='currentColor'>");
            sb.AppendLine("                                    <path fill-rule='evenodd' d='M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z' clip-rule='evenodd' />");
            sb.AppendLine("                                </svg>");
            sb.AppendLine("                                Image Information");
            sb.AppendLine("                            </h3>");
            sb.AppendLine("                            <p class='text-sm text-gray-500 dark:text-gray-400'>The processed images stored in LiteDb</p>");
            sb.AppendLine("                        </div>");

            sb.AppendLine("                        <div class='flex flex-col sm:flex-row gap-3'>");
            sb.AppendLine("                            <div class='relative'>");
            sb.AppendLine("                                <svg xmlns='http://www.w3.org/2000/svg' class='absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400' fill='none' viewBox='0 0 24 24' stroke='currentColor'>");
            sb.AppendLine("                                    <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z' />");
            sb.AppendLine("                                </svg>");
            sb.AppendLine("                                <input type='text' id='imageSearch' placeholder='Search images...' class='pl-10 pr-4 py-2 w-full sm:w-64 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-purple-500 focus:border-transparent'>");
            sb.AppendLine("                            </div>");

            sb.AppendLine("                            <div class='relative inline-block text-left'>");
            sb.AppendLine("                                <button type='button' id='dropdownActionButton' class='btn btn-outline btn-sm flex items-center'>");
            sb.AppendLine("                                    <svg xmlns='http://www.w3.org/2000/svg' class='mr-2 h-4 w-4' fill='none' viewBox='0 0 24 24' stroke='currentColor'>");
            sb.AppendLine("                                        <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z' />");
            sb.AppendLine("                                        <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M15 12a3 3 0 11-6 0 3 3 0 016 0z' />");
            sb.AppendLine("                                    </svg>");
            sb.AppendLine("                                    Actions");
            sb.AppendLine("                                    <svg class='ml-2 -mr-1 h-4 w-4' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20' fill='currentColor'>");
            sb.AppendLine("                                        <path fill-rule='evenodd' d='M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z' clip-rule='evenodd' />");
            sb.AppendLine("                                    </svg>");
            sb.AppendLine("                                </button>");

            sb.AppendLine("                                <div id='actionDropdown' class='hidden origin-top-right absolute right-0 mt-2 w-56 rounded-md shadow-lg bg-white dark:bg-gray-800 ring-1 ring-black ring-opacity-5 focus:outline-none z-10'>");
            sb.AppendLine("                                    <div class='py-1'>");
            sb.AppendLine($"                                        <a href='{route}/reset-all' class='group flex items-center px-4 py-2 text-sm text-amber-600 dark:text-amber-400 hover:bg-gray-100 dark:hover:bg-gray-700'>");
            sb.AppendLine("                                            <svg class='mr-3 h-5 w-5 text-amber-500 dark:text-amber-400' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20' fill='currentColor'>");
            sb.AppendLine("                                                <path fill-rule='evenodd' d='M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z' clip-rule='evenodd' />");
            sb.AppendLine("                                            </svg>");
            sb.AppendLine("                                            Reset Cache");
            sb.AppendLine("                                        </a>");
            sb.AppendLine($"                                        <a href='{route}/hard-reset-all' class='group flex items-center px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-gray-100 dark:hover:bg-gray-700'>");
            sb.AppendLine("                                            <svg class='mr-3 h-5 w-5 text-red-500 dark:text-red-400' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20' fill='currentColor'>");
            sb.AppendLine("                                                <path fill-rule='evenodd' d='M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z' clip-rule='evenodd' />");
            sb.AppendLine("                                            </svg>");
            sb.AppendLine("                                            Hard Reset Images");
            sb.AppendLine("                                        </a>");
            sb.AppendLine("                                    </div>");
            sb.AppendLine("                                </div>");
            sb.AppendLine("                            </div>");
            sb.AppendLine("                        </div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");

            // Table
            sb.AppendLine("                <div class='overflow-x-auto'>");
            sb.AppendLine("                    <table id='imageTable' class='min-w-full divide-y divide-gray-200 dark:divide-gray-700'>");
            sb.AppendLine("                        <thead class='bg-gray-50 dark:bg-gray-700'>");
            sb.AppendLine("                            <tr>");
            sb.AppendLine("                                <th scope='col' class='px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider'>Action</th>");
            sb.AppendLine("                                <th scope='col' class='px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider'>Preview</th>");
            sb.AppendLine("                                <th scope='col' class='px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider'>Name</th>");
            sb.AppendLine("                                <th scope='col' class='px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider'>Format</th>");
            sb.AppendLine("                                <th scope='col' class='px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider'>Quality</th>");
            sb.AppendLine("                                <th scope='col' class='px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider'>Processed Time</th>");
            sb.AppendLine("                            </tr>");
            sb.AppendLine("                        </thead>");

            // Table body
            sb.AppendLine("                        <tbody class='bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700'>");

            if (imageInfosList.Count == 0)
            {
                sb.AppendLine("                            <tr><td colspan='6' class='px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 text-center'>No data found in the database</td></tr>");
            }
            else
            {
                int i = 1;
                foreach (var imageInfo in imageInfosList)
                {
                    var formatExt = imageInfo.Format.GetValueOrDefault().ToFileExtension();
                    var imagePath = $"/{_outputDir}/{imageInfo.SanitizedName}/{formatExt}/{imageInfo.SanitizedName}-placeholder.{formatExt}";

                    sb.AppendLine($"                            <tr class='group hover:bg-gray-50 dark:hover:bg-gray-700' data-name='{imageInfo.SanitizedName.ToLowerInvariant()}'>");
                    sb.AppendLine($"                                <td class='px-6 py-4 whitespace-nowrap'>");
                    sb.AppendLine($"                                    <a href='{route}/delete?cache={imageInfo.Key}' class='inline-flex items-center justify-center p-1.5 text-red-600 dark:text-red-400 hover:text-red-900 dark:hover:text-red-300 rounded-full hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors' aria-label='Remove {imageInfo.SanitizedName}'>");
                    sb.AppendLine($"                                        <svg xmlns='http://www.w3.org/2000/svg' class='h-5 w-5' fill='none' viewBox='0 0 24 24' stroke='currentColor'>");
                    sb.AppendLine($"                                            <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16' />");
                    sb.AppendLine($"                                        </svg>");
                    sb.AppendLine($"                                    </a>");
                    sb.AppendLine($"                                </td>");
                    sb.AppendLine($"                                <td class='px-6 py-4 whitespace-nowrap'>");
                    sb.AppendLine($"                                    <div class='h-10 w-10 rounded-md overflow-hidden bg-gray-100 dark:bg-gray-700'>");
                    sb.AppendLine($"                                        <img class='h-full w-full object-cover' loading='lazy' src='{imagePath}' alt='{imageInfo.SanitizedName} preview' />");
                    sb.AppendLine($"                                    </div>");
                    sb.AppendLine($"                                </td>");
                    sb.AppendLine($"                                <td class='px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100'>{imageInfo.SanitizedName}</td>");
                    sb.AppendLine($"                                <td class='px-6 py-4 whitespace-nowrap'>");
                    sb.AppendLine($"                                    <span class='badge badge-purple'>");
                    sb.AppendLine($"                                        {imageInfo.Format.GetValueOrDefault().ToMimeType()}");
                    sb.AppendLine($"                                    </span>");
                    sb.AppendLine($"                                </td>");
                    sb.AppendLine($"                                <td class='px-6 py-4 whitespace-nowrap text-sm text-gray-700 dark:text-gray-300'>{imageInfo.Quality}</td>");
                    sb.AppendLine($"                                <td class='px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400'>{imageInfo?.ProcessedTime.GetValueOrDefault().ToString("yyyy-MM-dd HH:mm:ss UTC")}</td>");
                    sb.AppendLine("                            </tr>");
                    i++;
                }
            }

            sb.AppendLine("                        </tbody>");
            sb.AppendLine("                    </table>");
            sb.AppendLine("                </div>");

            // Pagination
            sb.AppendLine("                <div class='px-6 py-3 flex items-center justify-between border-t border-gray-200 dark:border-gray-700'>");
            sb.AppendLine("                    <div class='flex-1 flex justify-between sm:hidden'>");
            sb.AppendLine("                        <button id='mobilePrevPage' class='relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 text-sm font-medium rounded-md text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50' disabled>Previous</button>");
            sb.AppendLine("                        <button id='mobileNextPage' class='ml-3 relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 text-sm font-medium rounded-md text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50'>Next</button>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                    <div class='hidden sm:flex-1 sm:flex sm:items-center sm:justify-between'>");
            sb.AppendLine("                        <div>");
            sb.AppendLine("                            <p class='text-sm text-gray-700 dark:text-gray-300'>");
            sb.AppendLine("                                Showing <span id='pageStart' class='font-medium'>1</span> to <span id='pageEnd' class='font-medium'>10</span> of <span id='totalItems' class='font-medium'>" + imageInfosList.Count + "</span> results");
            sb.AppendLine("                            </p>");
            sb.AppendLine("                        </div>");
            sb.AppendLine("                        <div>");
            sb.AppendLine("                            <nav class='relative z-0 inline-flex rounded-md shadow-sm -space-x-px' aria-label='Pagination'>");
            sb.AppendLine("                                <button id='prevPage' class='relative inline-flex items-center px-2 py-2 rounded-l-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm font-medium text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50' disabled>");
            sb.AppendLine("                                    <span class='sr-only'>Previous</span>");
            sb.AppendLine("                                    <svg class='h-5 w-5' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20' fill='currentColor' aria-hidden='true'>");
            sb.AppendLine("                                        <path fill-rule='evenodd' d='M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z' clip-rule='evenodd' />");
            sb.AppendLine("                                    </svg>");
            sb.AppendLine("                                </button>");
            sb.AppendLine("                                <div id='paginationNumbers' class='bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400'></div>");
            sb.AppendLine("                                <button id='nextPage' class='relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm font-medium text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50'>");
            sb.AppendLine("                                    <span class='sr-only'>Next</span>");
            sb.AppendLine("                                    <svg class='h-5 w-5' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20' fill='currentColor' aria-hidden='true'>");
            sb.AppendLine("                                        <path fill-rule='evenodd' d='M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z' clip-rule='evenodd' />");
            sb.AppendLine("                                    </svg>");
            sb.AppendLine("                                </button>");
            sb.AppendLine("                            </nav>");
            sb.AppendLine("                        </div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");
        }
    }
}
