using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Bobeta.Web;
using Bobeta.Web.Services;
using Bobeta.Client;
using Bobeta.Client.Contracts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = baseAddress });

builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<AppStateService>();
builder.Services.AddScoped<IAccessTokenProvider, WebAccessTokenProvider>();
builder.Services.AddScoped<I18nService>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddBobetaClient(http => http.BaseAddress = new Uri(apiBaseUrl), useBearerToken: true);

await builder.Build().RunAsync();
