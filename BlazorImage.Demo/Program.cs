using BlazorImage.Demo.Components;
using BlazorImage.Extensions;

var builder = WebApplication.CreateBuilder(args);
  

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazorImage(a=>
{ 
   a.SlidingExpiration = TimeSpan.FromHours(1);
    a.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10);
    a.DefaultQuality = 70;
    a.AspectWidth = 1.0;
    a.AspectHeigth = 1.0;
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
app.MapBlazorImage("/blazor/images");

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(); 

app.Run();
 