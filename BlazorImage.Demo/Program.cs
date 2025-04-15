using BlazorImage.Demo.Components;
using BlazorImage.Extensions; 

var builder = WebApplication.CreateBuilder(args);
 
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add BlazorImage service
builder.Services.AddBlazorImage();

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



// Add BlazorImage Dashboard
app.MapBlazorImageDashboard("/blazor/images");
// Add BlazorImage Runtime
app.MapBlazorImageRuntime();

app.MapStaticAssets();
 


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(); 

app.Run();
 