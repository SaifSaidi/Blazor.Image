using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorImage.Models;
using Microsoft.Extensions.Options;

namespace BlazorImage.Services
{

    class DashboardService: IDashboardService
    {
        private readonly ILiteDatabase _db;

        private readonly DictionaryCacheDataService _dictionaryCacheData;

        private readonly BlazorImageConfig blazorImageConfig;
         

        public DashboardService(ILiteDatabase db, DictionaryCacheDataService dictionaryCacheData,
        IOptions<BlazorImageConfig> options)
        {
            _db = db;
            _dictionaryCacheData = dictionaryCacheData;
            blazorImageConfig = options.Value; 
        }

        public string DashboardData(string route)
        {

            var collection = _db.GetCollection<ImageInfo>(Constants.LiteDbCollection);

            var imageInfos = collection.FindAll();
            StringBuilder sb = BlazorImageHtml(route, imageInfos);

            return sb.ToString();
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

            var sb = new StringBuilder(8192); // Pre-allocate buffer for better performance

            // HTML head with optimized resources
            AppendHtmlHead(sb);

            // Body start
            sb.AppendLine("<body class='bg-gray-50 dark:bg-gray-900 font-sans antialiased text-gray-900 dark:text-gray-100'>");
            sb.AppendLine("    <div class='container mx-auto px-4 md:px-8 py-6 md:py-10'>");

            // Header section
            AppendHeader(sb, route);

            // Cache status section
            AppendCacheStatus(sb);

            // Image information table
            AppendImageTable(sb, route, imageInfosList);

            // Footer
            sb.AppendLine("        <footer class='mt-8 text-center text-gray-500 text-sm'>");
            sb.AppendLine("            <p>&copy; 2025 BlazorImage. All rights reserved.</p>");
            sb.AppendLine("        </footer>");
            sb.AppendLine("    </div>");

            // Append scripts at the end for better performance
            AppendScripts(sb);

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb;
        }

        private void AppendHtmlHead(StringBuilder sb)
        {
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='UTF-8'>");
            sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("    <meta name='description' content='BlazorImage Dashboard - Monitor and manage your image cache'>");
            sb.AppendLine("    <title>BlazorImage Dashboard</title>");

            // Preload critical resources
            sb.AppendLine("    <link rel='preconnect' href='https://cdn.tailwindcss.com'>");
            sb.AppendLine("    <link rel='preconnect' href='https://cdnjs.cloudflare.com'>");

            // Inline critical CSS for faster rendering
            sb.AppendLine("    <style>");
            sb.AppendLine("      /* Critical CSS */");
            sb.AppendLine("      .skeleton { background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%); background-size: 200% 100%; animation: skeleton-loading 1.5s infinite; }");
            sb.AppendLine("      @keyframes skeleton-loading { 0% { background-position: 200% 0; } 100% { background-position: -200% 0; } }");
            sb.AppendLine("      @media (prefers-color-scheme: dark) { .dark\\:bg-gray-900 { background-color: #111827; } .dark\\:text-gray-100 { color: #f3f4f6; } }");
            sb.AppendLine("    </style>");

            // Load external resources with performance attributes
            sb.AppendLine("    <script src='https://cdn.tailwindcss.com' defer></script>");
            sb.AppendLine("    <link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css' rel='stylesheet' media='print' onload=\"this.media='all'\" />");

            sb.AppendLine("</head>");
        }

        private void AppendHeader(StringBuilder sb, string route)
        {
            sb.AppendLine("        <header class='mb-6 md:mb-10 flex flex-col md:flex-row justify-between items-start md:items-center gap-4'>");
            sb.AppendLine("            <div>");
            sb.AppendLine("                <h1 class='text-2xl md:text-3xl font-bold text-indigo-700 dark:text-indigo-400'><i class='fa fa-image text-indigo-500 mr-2' aria-hidden='true'></i> BlazorImage Dashboard</h1>");
            sb.AppendLine("                <p class='text-gray-600 dark:text-gray-400 mt-2'>Monitor and manage your BlazorImage cache services with ease.</p>");
            sb.AppendLine("            </div>");

            // Optimized dropdown with better accessibility
            sb.AppendLine("            <div class='relative inline-block text-left dropdown'>");
            sb.AppendLine("                <button type='button' class='inline-flex justify-center w-full px-4 py-2 font-semibold text-gray-700 dark:text-gray-200 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-md shadow-sm hover:bg-gray-100 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-opacity-50 transition-colors dropdown-toggle' id='dropdownActionButton' aria-haspopup='true' aria-expanded='false'>");
            sb.AppendLine("                    <i class='fa fa-wrench mr-2' aria-hidden='true'></i> Actions");
            sb.AppendLine("                    <svg class='w-5 h-5 ml-2 -mr-1' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20' fill='currentColor' aria-hidden='true'>");
            sb.AppendLine("                        <path fill-rule='evenodd' d='M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z' clip-rule='evenodd' />");
            sb.AppendLine("                    </svg>");
            sb.AppendLine("                </button>");

            sb.AppendLine("                <div class='absolute right-0 mt-2 w-56 rounded-md shadow-lg bg-white dark:bg-gray-800 ring-1 ring-black ring-opacity-5 focus:outline-none dropdown-menu opacity-0 invisible transform transition-all duration-200 translate-y-2 z-10' role='menu' aria-orientation='vertical' aria-labelledby='dropdownActionButton'>");
            sb.AppendLine("                    <div class='py-1'>");
            sb.AppendLine($"                        <a href='{route}/refresh-all' class='block px-4 py-2 text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white transition-colors' role='menuitem'><i class='fa fa-sync-alt mr-2' aria-hidden='true'></i> Refresh Cache</a>");
            sb.AppendLine($"                        <a href='{route}/reset-all' class='block px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-red-700 dark:hover:text-red-300 transition-colors' role='menuitem'><i class='fa fa-trash mr-2' aria-hidden='true'></i> Reset Cache</a>");
            sb.AppendLine($"                        <a href='{route}/hard-reset-all' class='block px-4 py-2 text-sm text-red-700 dark:text-red-500 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-red-800 dark:hover:text-red-400 transition-colors' role='menuitem'><i class='fa fa-skull-crossbones mr-2' aria-hidden='true'></i> Hard Reset</a>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </header>");
        }

        private void AppendCacheStatus(StringBuilder sb)
        {
            sb.AppendLine("        <section class='mb-6 md:mb-8 p-4 md:p-6 bg-white dark:bg-gray-800 shadow rounded-lg border border-gray-200 dark:border-gray-700 transition-colors'>");
            sb.AppendLine("            <div class='flex justify-between items-center mb-4'>");
            sb.AppendLine("                <h2 class='text-lg md:text-xl font-semibold text-gray-800 dark:text-gray-200'><i class='fa fa-memory text-gray-500 dark:text-gray-400 mr-2' aria-hidden='true'></i> Cache Status</h2>");
            sb.AppendLine("                <button id='toggleButton' class='bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-200 font-semibold py-1 px-3 rounded-full focus:outline-none focus:ring-2 focus:ring-gray-400 focus:ring-opacity-50 text-sm transition-colors' aria-expanded='false'><span id='toggleText'>(Show Cache Keys)</span></button>");
            sb.AppendLine("            </div>");
            sb.AppendLine($"            <p class='text-gray-700 dark:text-gray-300 mb-3'><span class='font-semibold'>Tracked Items:</span> <span class='text-blue-700 dark:text-blue-400 font-medium'>({_dictionaryCacheData.SourceInfoCache.Count}) entries</span></p>");

            // Cache list with improved accessibility
            sb.AppendLine("            <div id='cacheListContainer' class='hidden'>");
            sb.AppendLine("                <ul id='cacheList' class='list-disc pl-5 text-sm text-gray-600 dark:text-gray-400 mt-2 max-h-60 overflow-y-auto' role='list'>");

            // Limit the number of items rendered initially for performance
            var cacheItems = _dictionaryCacheData.SourceInfoCache.Take(100).ToList();
            foreach (var item in cacheItems)
            {
                sb.AppendLine($"                    <li class='py-1'>{item.Key}</li>");
            }

            if (_dictionaryCacheData.SourceInfoCache.Count > 100)
            {
                sb.AppendLine($"                    <li class='py-1 font-semibold'>... and {_dictionaryCacheData.SourceInfoCache.Count - 100} more items</li>");
            }

            sb.AppendLine("                </ul>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </section>");
        }

        private void AppendImageTable(StringBuilder sb, string route, List<ImageInfo> imageInfosList)
        {
            sb.AppendLine("        <section class='bg-white dark:bg-gray-800 shadow-lg rounded-lg overflow-hidden border border-gray-200 dark:border-gray-700 transition-colors'>");
            sb.AppendLine("            <div class='px-4 md:px-6 py-4 md:py-5 bg-gray-100 dark:bg-gray-700 border-b border-gray-200 dark:border-gray-600'>");
            sb.AppendLine("                <div class='flex justify-between items-center'>");
            sb.AppendLine("                    <h2 class='text-lg md:text-xl font-semibold text-gray-800 dark:text-gray-200'><i class='fa fa-table mr-2' aria-hidden='true'></i> Image Information</h2>");

            // Add search functionality
            sb.AppendLine("                    <div class='relative'>");
            sb.AppendLine("                        <input type='text' id='imageSearch' placeholder='Search images...' class='w-full md:w-64 px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500'>");
            sb.AppendLine("                        <i class='fa fa-search absolute right-3 top-2.5 text-gray-400' aria-hidden='true'></i>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");

            // Table with responsive design
            sb.AppendLine("            <div class='overflow-x-auto'>");
            sb.AppendLine("                <table id='imageTable' class='min-w-full divide-y divide-gray-300 dark:divide-gray-600'>");
            sb.AppendLine("                    <thead class='bg-gray-50 dark:bg-gray-700'>");
            sb.AppendLine("                        <tr>");
            sb.AppendLine("                            <th class='px-4 md:px-6 py-3 text-left text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-600 transition-colors' data-sort='action'><i class='fa fa-cog mr-1' aria-hidden='true'></i> Action</th>");
            sb.AppendLine("                            <th class='px-4 md:px-6 py-3 text-left text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-600 transition-colors' data-sort='image'><i class='fa fa-image mr-1' aria-hidden='true'></i> Image</th>");
            sb.AppendLine("                            <th class='px-4 md:px-6 py-3 text-left text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-600 transition-colors' data-sort='name'><i class='fa fa-file-signature mr-1' aria-hidden='true'></i> Sanitized Name</th>");
            sb.AppendLine("                            <th class='px-4 md:px-6 py-3 text-left text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-600 transition-colors' data-sort='format'><i class='fa fa-file-alt mr-1' aria-hidden='true'></i> Format</th>");
            sb.AppendLine("                            <th class='px-4 md:px-6 py-3 text-left text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-600 transition-colors' data-sort='quality'><i class='fa fa-compress mr-1' aria-hidden='true'></i> Quality</th>");
            sb.AppendLine("                            <th class='px-4 md:px-6 py-3 text-left text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-600 transition-colors' data-sort='time'><i class='fa fa-clock mr-1' aria-hidden='true'></i> Processed Time (UTC)</th>");
            sb.AppendLine("                        </tr>");
            sb.AppendLine("                    </thead>");

            sb.AppendLine("                    <tbody class='bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700'>");

            if (imageInfosList.Count == 0)
            {
                sb.AppendLine("                        <tr><td colspan='6' class='px-4 md:px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 text-center'>No data found in the database</td></tr>");
            }
            else
            {
                int i = 1;
                foreach (var imageInfo in imageInfosList)
                {
                    var formatExt = imageInfo.Format.GetValueOrDefault().ToFileExtension();
                    var dirName = blazorImageConfig.Dir.TrimStart('/');
                     var imagePath = $"/{dirName}/{imageInfo.SanitizedName}/{formatExt}/{imageInfo.SanitizedName}-placeholder.{formatExt}";

                    sb.AppendLine($"                        <tr class='{(i % 2 == 0 ? "bg-gray-50 dark:bg-gray-700" : "bg-white dark:bg-gray-800")} hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors' data-name='{imageInfo.SanitizedName.ToLowerInvariant()}'>");
                    sb.AppendLine($"                            <td class='px-4 md:px-6 py-4 whitespace-nowrap text-sm font-medium'><a href='{route}/delete?cache={imageInfo.Key}' class='text-red-600 dark:text-red-400 hover:text-red-900 dark:hover:text-red-300 transition-colors' aria-label='Remove {imageInfo.SanitizedName}'><i class='fa fa-trash-alt mr-1' aria-hidden='true'></i> Remove</a></td>");
                    sb.AppendLine($"                            <td class='px-4 md:px-6 py-4 whitespace-nowrap'><img class='object-cover rounded-md w-12 h-12' loading='lazy' src='{imagePath}' alt='{imageInfo.SanitizedName} preview' /></td>");
                    sb.AppendLine($"                            <td class='px-4 md:px-6 py-4 whitespace-nowrap text-gray-700 dark:text-gray-300'>{imageInfo.SanitizedName} </td>");
                    sb.AppendLine($"                            <td class='px-4 md:px-6 py-4 whitespace-nowrap text-gray-700 dark:text-gray-300'>{imageInfo.Format.GetValueOrDefault().ToMimeType()}</td>");
                    sb.AppendLine($"                            <td class='px-4 md:px-6 py-4 whitespace-nowrap text-gray-700 dark:text-gray-300'>{imageInfo.Quality}</td>");
                    sb.AppendLine($"                            <td class='px-4 md:px-6 py-4 whitespace-nowrap text-gray-700 dark:text-gray-300'>{imageInfo?.ProcessedTime.GetValueOrDefault().ToString("yyyy-MM-dd HH:mm:ss UTC")}</td>");
                    sb.AppendLine("                        </tr>");
                    i++;
                }
            }

            sb.AppendLine("                    </tbody>");
            sb.AppendLine("                </table>");
            sb.AppendLine("            </div>");

            // Pagination controls
            sb.AppendLine("            <div class='px-4 md:px-6 py-3 flex items-center justify-between border-t border-gray-200 dark:border-gray-700'>");
            sb.AppendLine("                <div class='flex-1 flex justify-between sm:hidden'>");
            sb.AppendLine("                    <button id='mobilePrevPage' class='relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 text-sm font-medium rounded-md text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50' disabled>Previous</button>");
            sb.AppendLine("                    <button id='mobileNextPage' class='ml-3 relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 text-sm font-medium rounded-md text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50'>Next</button>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='hidden sm:flex-1 sm:flex sm:items-center sm:justify-between'>");
            sb.AppendLine("                    <div>");
            sb.AppendLine("                        <p class='text-sm text-gray-700 dark:text-gray-300'>");
            sb.AppendLine("                            Showing <span id='pageStart'>1</span> to <span id='pageEnd'>10</span> of <span id='totalItems'>" + imageInfosList.Count + "</span> results");
            sb.AppendLine("                        </p>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                    <div>");
            sb.AppendLine("                        <nav class='relative z-0 inline-flex rounded-md shadow-sm -space-x-px' aria-label='Pagination'>");
            sb.AppendLine("                            <button id='prevPage' class='relative inline-flex items-center px-2 py-2 rounded-l-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm font-medium text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50' disabled>");
            sb.AppendLine("                                <span class='sr-only'>Previous</span>");
            sb.AppendLine("                                <i class='fa fa-chevron-left h-5 w-5' aria-hidden='true'></i>");
            sb.AppendLine("                            </button>");
            sb.AppendLine("                            <div id='paginationNumbers' class='bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400'></div>");
            sb.AppendLine("                            <button id='nextPage' class='relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm font-medium text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50'>");
            sb.AppendLine("                                <span class='sr-only'>Next</span>");
            sb.AppendLine("                                <i class='fa fa-chevron-right h-5 w-5' aria-hidden='true'></i>");
            sb.AppendLine("                            </button>");
            sb.AppendLine("                        </nav>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </section>");
        }

        private void AppendScripts(StringBuilder sb)
        {
            sb.AppendLine("    <script>");
            sb.AppendLine("        document.addEventListener('DOMContentLoaded', function() {");
            sb.AppendLine("            // Cache DOM elements");
            sb.AppendLine("            const $ = document.querySelector.bind(document);");
            sb.AppendLine("            const $$ = document.querySelectorAll.bind(document);");
            sb.AppendLine("            const toggleBtn = $('#toggleButton');");
            sb.AppendLine("            const cacheContainer = $('#cacheListContainer');");
            sb.AppendLine("            const toggleText = $('#toggleText');");
            sb.AppendLine("            const dropdownBtn = $('.dropdown-toggle');");
            sb.AppendLine("            const dropdownMenu = $('.dropdown-menu');");
            sb.AppendLine("            const imageSearch = $('#imageSearch');");
            sb.AppendLine("            const imageTable = $('#imageTable');");
            sb.AppendLine("            const tableBody = imageTable?.querySelector('tbody');");
            sb.AppendLine("            const prevBtn = $('#prevPage');");
            sb.AppendLine("            const nextBtn = $('#nextPage');");
            sb.AppendLine("            const mobilePrevBtn = $('#mobilePrevPage');");
            sb.AppendLine("            const mobileNextBtn = $('#mobileNextPage');");
            sb.AppendLine("            const pageStart = $('#pageStart');");
            sb.AppendLine("            const pageEnd = $('#pageEnd');");
            sb.AppendLine("            const totalItems = $('#totalItems');");
            sb.AppendLine("            const paginationNumbers = $('#paginationNumbers');");
            sb.AppendLine("            ");
            sb.AppendLine("            // Constants");
            sb.AppendLine("            const ITEMS_PER_PAGE = 10;");
            sb.AppendLine("            const MAX_VISIBLE_PAGES = 5;");
            sb.AppendLine("            let currentPage = 1;");
            sb.AppendLine("            let sortState = { column: null, asc: true };");
            sb.AppendLine("            ");
            sb.AppendLine("            // Cache list toggle");
            sb.AppendLine("            if (toggleBtn && cacheContainer && toggleText) {");
            sb.AppendLine("                toggleBtn.addEventListener('click', function() {");
            sb.AppendLine("                    const isHidden = cacheContainer.classList.contains('hidden');");
            sb.AppendLine("                    cacheContainer.classList.toggle('hidden');");
            sb.AppendLine("                    toggleText.textContent = isHidden ? '(Hide Cache Keys)' : '(Show Cache Keys)';");
            sb.AppendLine("                    toggleBtn.setAttribute('aria-expanded', isHidden ? 'true' : 'false');");
            sb.AppendLine("                });");
            sb.AppendLine("            }");
            sb.AppendLine("            ");
            sb.AppendLine("            // Dropdown menu");
            sb.AppendLine("            if (dropdownBtn && dropdownMenu) {");
            sb.AppendLine("                const toggleDropdown = (show) => {");
            sb.AppendLine("                    const classes = ['invisible', 'opacity-0', 'translate-y-2'];");
            sb.AppendLine("                    const activeClasses = ['opacity-100', 'translate-y-0'];");
            sb.AppendLine("                    ");
            sb.AppendLine("                    if (show === undefined) {");
            sb.AppendLine("                        classes.forEach(c => dropdownMenu.classList.toggle(c));");
            sb.AppendLine("                        activeClasses.forEach(c => dropdownMenu.classList.toggle(c));");
            sb.AppendLine("                        dropdownBtn.setAttribute('aria-expanded', dropdownMenu.classList.contains('opacity-100'));");
            sb.AppendLine("                    } else if (show) {");
            sb.AppendLine("                        classes.forEach(c => dropdownMenu.classList.remove(c));");
            sb.AppendLine("                        activeClasses.forEach(c => dropdownMenu.classList.add(c));");
            sb.AppendLine("                        dropdownBtn.setAttribute('aria-expanded', 'true');");
            sb.AppendLine("                    } else {");
            sb.AppendLine("                        classes.forEach(c => dropdownMenu.classList.add(c));");
            sb.AppendLine("                        activeClasses.forEach(c => dropdownMenu.classList.remove(c));");
            sb.AppendLine("                        dropdownBtn.setAttribute('aria-expanded', 'false');");
            sb.AppendLine("                    }");
            sb.AppendLine("                };");
            sb.AppendLine("                ");
            sb.AppendLine("                dropdownBtn.addEventListener('click', (e) => {");
            sb.AppendLine("                    e.stopPropagation();");
            sb.AppendLine("                    toggleDropdown();");
            sb.AppendLine("                });");
            sb.AppendLine("                ");
            sb.AppendLine("                document.addEventListener('click', (e) => {");
            sb.AppendLine("                    if (!dropdownMenu.closest('.dropdown')?.contains(e.target)) {");
            sb.AppendLine("                        toggleDropdown(false);");
            sb.AppendLine("                    }");
            sb.AppendLine("                });");
            sb.AppendLine("            }");
            sb.AppendLine("            ");
            sb.AppendLine("            // Table functions");
            sb.AppendLine("            if (imageTable) {");
            sb.AppendLine("                // Utility functions");
            sb.AppendLine("                const getCellValue = (tr, idx) => {");
            sb.AppendLine("                    const cell = tr.children[idx];");
            sb.AppendLine("                    return cell ? (cell.innerText || cell.textContent || '').trim() : '';");
            sb.AppendLine("                };");
            sb.AppendLine("                ");
            sb.AppendLine("                const compareValues = (v1, v2) => {");
            sb.AppendLine("                    return !isNaN(v1) && !isNaN(v2) ? Number(v1) - Number(v2) : String(v1).localeCompare(String(v2));");
            sb.AppendLine("                };");
            sb.AppendLine("                ");
            sb.AppendLine("                const getVisibleRows = () => {");
            sb.AppendLine("                    return Array.from($$('#imageTable tbody tr:not([data-empty])'))");
            sb.AppendLine("                        .filter(row => !row.classList.contains('hidden'));");
            sb.AppendLine("                };");
            sb.AppendLine("                ");
            sb.AppendLine("                // Table sorting");
            sb.AppendLine("                const tableHeaders = $$('th');");
            sb.AppendLine("                if (tableHeaders.length && tableBody) {");
            sb.AppendLine("                    tableHeaders.forEach(th => {");
            sb.AppendLine("                        th.addEventListener('click', () => {");
            sb.AppendLine("                            const rows = Array.from(tableBody.querySelectorAll('tr:not([data-empty])'));");
            sb.AppendLine("                            if (!rows.length) return;");
            sb.AppendLine("                            ");
            sb.AppendLine("                            // Clear previous sort indicators");
            sb.AppendLine("                            tableHeaders.forEach(header => {");
            sb.AppendLine("                                header.classList.remove('text-indigo-600');");
            sb.AppendLine("                                const icon = header.querySelector('i:not(:first-child)');");
            sb.AppendLine("                                if (icon) icon.remove();");
            sb.AppendLine("                            });");
            sb.AppendLine("                            ");
            sb.AppendLine("                            // Set new sort state");
            sb.AppendLine("                            const idx = Array.from(th.parentNode.children).indexOf(th);");
            sb.AppendLine("                            const isNewColumn = sortState.column !== idx;");
            sb.AppendLine("                            const asc = isNewColumn ? true : !sortState.asc;");
            sb.AppendLine("                            sortState = { column: idx, asc };");
            sb.AppendLine("                            ");
            sb.AppendLine("                            // Add sort indicator");
            sb.AppendLine("                            th.classList.add('text-indigo-600');");
            sb.AppendLine("                            const sortIcon = document.createElement('i');");
            sb.AppendLine("                            sortIcon.className = `fa fa-sort-${asc ? 'up' : 'down'} ml-1`;");
            sb.AppendLine("                            sortIcon.setAttribute('aria-hidden', 'true');");
            sb.AppendLine("                            th.appendChild(sortIcon);");
            sb.AppendLine("                            ");
            sb.AppendLine("                            // Sort rows");
            sb.AppendLine("                            rows.sort((a, b) => {");
            sb.AppendLine("                                const v1 = getCellValue(asc ? a : b, idx);");
            sb.AppendLine("                                const v2 = getCellValue(asc ? b : a, idx);");
            sb.AppendLine("                                return compareValues(v1, v2);");
            sb.AppendLine("                            }).forEach(tr => tableBody.appendChild(tr));");
            sb.AppendLine("                            ");
            sb.AppendLine("                            // Reset to first page and update pagination");
            sb.AppendLine("                            currentPage = 1;");
            sb.AppendLine("                            updatePagination();");
            sb.AppendLine("                        });");
            sb.AppendLine("                    });");
            sb.AppendLine("                }");
            sb.AppendLine("                ");
            sb.AppendLine("                // Search functionality");
            sb.AppendLine("                if (imageSearch && tableBody) {");
            sb.AppendLine("                    // Debounce function to improve performance");
            sb.AppendLine("                    const debounce = (fn, delay) => {");
            sb.AppendLine("                        let timer;");
            sb.AppendLine("                        return function(...args) {");
            sb.AppendLine("                            clearTimeout(timer);");
            sb.AppendLine("                            timer = setTimeout(() => fn.apply(this, args), delay);");
            sb.AppendLine("                        };");
            sb.AppendLine("                    };");
            sb.AppendLine("                    ");
            sb.AppendLine("                    const handleSearch = debounce(function() {");
            sb.AppendLine("                        const searchTerm = this.value.toLowerCase();");
            sb.AppendLine("                        const rows = tableBody.querySelectorAll('tr:not([data-empty])');");
            sb.AppendLine("                        let visibleCount = 0;");
            sb.AppendLine("                        ");
            sb.AppendLine("                        rows.forEach(row => {");
            sb.AppendLine("                            const name = row.getAttribute('data-name') || '';");
            sb.AppendLine("                            const text = row.textContent.toLowerCase();");
            sb.AppendLine("                            const isVisible = !searchTerm || name.includes(searchTerm) || text.includes(searchTerm);");
            sb.AppendLine("                            ");
            sb.AppendLine("                            row.classList.toggle('hidden', !isVisible);");
            sb.AppendLine("                            if (isVisible) visibleCount++;");
            sb.AppendLine("                        });");
            sb.AppendLine("                        ");
            sb.AppendLine("                        // Show empty message if no results");
            sb.AppendLine("                        let emptyRow = tableBody.querySelector('tr[data-empty]');");
            sb.AppendLine("                        if (visibleCount === 0) {");
            sb.AppendLine("                            if (!emptyRow) {");
            sb.AppendLine("                                emptyRow = document.createElement('tr');");
            sb.AppendLine("                                emptyRow.setAttribute('data-empty', 'true');");
            sb.AppendLine("                                emptyRow.innerHTML = '<td colspan=\"6\" class=\"px-4 md:px-6 py-4 whitespace-nowrap text-sm text-gray-500 text-center\">No matching results found</td>';");
            sb.AppendLine("                                tableBody.appendChild(emptyRow);");
            sb.AppendLine("                            } else {");
            sb.AppendLine("                                emptyRow.classList.remove('hidden');");
            sb.AppendLine("                            }");
            sb.AppendLine("                        } else if (emptyRow) {");
            sb.AppendLine("                            emptyRow.classList.add('hidden');");
            sb.AppendLine("                        }");
            sb.AppendLine("                        ");
            sb.AppendLine("                        // Reset to first page and update pagination");
            sb.AppendLine("                        currentPage = 1;");
            sb.AppendLine("                        updatePagination();");
            sb.AppendLine("                    }, 200);");
            sb.AppendLine("                    ");
            sb.AppendLine("                    imageSearch.addEventListener('input', handleSearch);");
            sb.AppendLine("                }");
            sb.AppendLine("                ");
            sb.AppendLine("                // Pagination");
            sb.AppendLine("                function updatePagination() {");
            sb.AppendLine("                    if (!paginationNumbers || !pageStart || !pageEnd || !totalItems) return;");
            sb.AppendLine("                    ");
            sb.AppendLine("                    const rows = getVisibleRows();");
            sb.AppendLine("                    const totalPages = Math.max(1, Math.ceil(rows.length / ITEMS_PER_PAGE));");
            sb.AppendLine("                    ");
            sb.AppendLine("                    // Ensure current page is valid");
            sb.AppendLine("                    if (currentPage > totalPages) currentPage = totalPages;");
            sb.AppendLine("                    ");
            sb.AppendLine("                    // Update pagination info");
            sb.AppendLine("                    const start = rows.length ? (currentPage - 1) * ITEMS_PER_PAGE + 1 : 0;");
            sb.AppendLine("                    const end = Math.min(start + ITEMS_PER_PAGE - 1, rows.length);");
            sb.AppendLine("                    pageStart.textContent = start.toString();");
            sb.AppendLine("                    pageEnd.textContent = end.toString();");
            sb.AppendLine("                    totalItems.textContent = rows.length.toString();");
            sb.AppendLine("                    ");
            sb.AppendLine("                    // Update button states");
            sb.AppendLine("                    const isFirstPage = currentPage === 1;");
            sb.AppendLine("                    const isLastPage = currentPage === totalPages;");
            sb.AppendLine("                    ");
            sb.AppendLine("                    if (prevBtn) prevBtn.disabled = isFirstPage;");
            sb.AppendLine("                    if (nextBtn) nextBtn.disabled = isLastPage;");
            sb.AppendLine("                    if (mobilePrevBtn) mobilePrevBtn.disabled = isFirstPage;");
            sb.AppendLine("                    if (mobileNextBtn) mobileNextBtn.disabled = isLastPage;");
            sb.AppendLine("                    ");
            sb.AppendLine("                    // Show/hide rows based on current page");
            sb.AppendLine("                    rows.forEach((row, index) => {");
            sb.AppendLine("                        const rowPage = Math.floor(index / ITEMS_PER_PAGE) + 1;");
            sb.AppendLine("                        row.classList.toggle('hidden', rowPage !== currentPage);");
            sb.AppendLine("                    });");
            sb.AppendLine("                    ");
            sb.AppendLine("                    // Generate pagination numbers");
            sb.AppendLine("                    paginationNumbers.innerHTML = '';");
            sb.AppendLine("                    ");
            sb.AppendLine("                    if (totalPages <= 1) return;");
            sb.AppendLine("                    ");
            sb.AppendLine("                    // Calculate visible page range");
            sb.AppendLine("                    let startPage = Math.max(1, currentPage - Math.floor(MAX_VISIBLE_PAGES / 2));");
            sb.AppendLine("                    let endPage = Math.min(totalPages, startPage + MAX_VISIBLE_PAGES - 1);");
            sb.AppendLine("                    ");
            sb.AppendLine("                    if (endPage - startPage + 1 < MAX_VISIBLE_PAGES) {");
            sb.AppendLine("                        startPage = Math.max(1, endPage - MAX_VISIBLE_PAGES + 1);");
            sb.AppendLine("                    }");
            sb.AppendLine("                    ");
            sb.AppendLine("                    // Create page buttons efficiently with document fragment");
            sb.AppendLine("                    const fragment = document.createDocumentFragment();");
            sb.AppendLine("                    for (let i = startPage; i <= endPage; i++) {");
            sb.AppendLine("                        const pageButton = document.createElement('button');");
            sb.AppendLine("                        const isActive = i === currentPage;");
            sb.AppendLine("                        pageButton.className = `relative inline-flex items-center px-4 py-2 border ${isActive ? 'bg-indigo-50 border-indigo-500 text-indigo-600' : 'bg-white border-gray-300 text-gray-700 hover:bg-gray-50'} text-sm font-medium`;");
            sb.AppendLine("                        pageButton.textContent = i.toString();");
            sb.AppendLine("                        pageButton.addEventListener('click', () => {");
            sb.AppendLine("                            if (currentPage !== i) {");
            sb.AppendLine("                                currentPage = i;");
            sb.AppendLine("                                updatePagination();");
            sb.AppendLine("                            }");
            sb.AppendLine("                        });");
            sb.AppendLine("                        fragment.appendChild(pageButton);");
            sb.AppendLine("                    }");
            sb.AppendLine("                    paginationNumbers.appendChild(fragment);");
            sb.AppendLine("                }");
            sb.AppendLine("                ");
            sb.AppendLine("                // Pagination event listeners");
            sb.AppendLine("                const setupPaginationButton = (btn, delta) => {");
            sb.AppendLine("                    if (!btn) return;");
            sb.AppendLine("                    btn.addEventListener('click', () => {");
            sb.AppendLine("                        const rows = getVisibleRows();");
            sb.AppendLine("                        const totalPages = Math.ceil(rows.length / ITEMS_PER_PAGE);");
            sb.AppendLine("                        const newPage = currentPage + delta;");
            sb.AppendLine("                        ");
            sb.AppendLine("                        if (newPage >= 1 && newPage <= totalPages) {");
            sb.AppendLine("                            currentPage = newPage;");
            sb.AppendLine("                            updatePagination();");
            sb.AppendLine("                        }");
            sb.AppendLine("                    });");
            sb.AppendLine("                };");
            sb.AppendLine("                ");
            sb.AppendLine("                setupPaginationButton(prevBtn, -1);");
            sb.AppendLine("                setupPaginationButton(nextBtn, 1);");
            sb.AppendLine("                setupPaginationButton(mobilePrevBtn, -1);");
            sb.AppendLine("                setupPaginationButton(mobileNextBtn, 1);");
            sb.AppendLine("                ");
            sb.AppendLine("                // Initialize pagination");
            sb.AppendLine("                updatePagination();");
            sb.AppendLine("                ");
            sb.AppendLine("                // Lazy load images");
            sb.AppendLine("                if ('IntersectionObserver' in window) {");
            sb.AppendLine("                    const lazyImages = $$('img[loading=\"lazy\"]');");
            sb.AppendLine("                    if (lazyImages.length) {");
            sb.AppendLine("                        const imageObserver = new IntersectionObserver((entries) => {");
            sb.AppendLine("                            entries.forEach(entry => {");
            sb.AppendLine("                                if (entry.isIntersecting) {");
            sb.AppendLine("                                    const img = entry.target;");
            sb.AppendLine("                                    const src = img.getAttribute('data-src');");
            sb.AppendLine("                                    if (src) {");
            sb.AppendLine("                                        img.src = src;");
            sb.AppendLine("                                        img.removeAttribute('data-src');");
            sb.AppendLine("                                    }");
            sb.AppendLine("                                    imageObserver.unobserve(img);");
            sb.AppendLine("                                }");
            sb.AppendLine("                            });");
            sb.AppendLine("                        }, { rootMargin: '50px' });");
            sb.AppendLine("                        ");
            sb.AppendLine("                        lazyImages.forEach(img => imageObserver.observe(img));");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        });");
            sb.AppendLine("    </script>");
        }
    }
}
