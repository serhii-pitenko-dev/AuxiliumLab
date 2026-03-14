// Minimal ASP.NET Core static-file host for the Blazor WebAssembly frontend.
//
// UseDefaultFiles()  — serves index.html for root requests
// UseStaticFiles()   — serves all files from wwwroot/
// MapFallbackToFile  — returns index.html for unknown paths (SPA deep links)

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
