using BlazorImage.Demo.Components;
using BlazorImage.Extensions;

var builder = WebApplication.CreateBuilder(args);
 
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazorImage(options =>
{
    //options.Dir = "output";
    options.ConfigSizes = [640, 1024, 1280, 1536];
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

 
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapBlazorImage("/blazor/images");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(); 

app.Run();
 