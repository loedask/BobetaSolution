using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseWebAssemblyDebugging();

var staticFileOptions = new StaticFileOptions
{
    ContentTypeProvider = CreateContentTypeProvider()
};

app.UseBlazorFrameworkFiles();
app.UseStaticFiles(staticFileOptions);
app.MapFallbackToFile("index.html");

app.Run();

static FileExtensionContentTypeProvider CreateContentTypeProvider()
{
    var provider = new FileExtensionContentTypeProvider();
    provider.Mappings[".webmanifest"] = "application/manifest+json";
    return provider;
}
