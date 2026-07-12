using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Bobeta.Web;
using Bobeta.Web.Services;
using Bobeta.Web.ViewModels.Games;
using Bobeta.Web.Shared;
using Bobeta.Web.Shared.Services.Realtime;
using Bobeta.Client;
using Bobeta.Client.Contracts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBobetaWebShared();
builder.Services.AddScoped<IAccessTokenProvider, Bobeta.Web.Shared.Services.WebAccessTokenProvider>();
builder.Services.AddScoped<GamePlayTestService>();

var apiBaseUrl = (builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress).TrimEnd('/');
if (string.IsNullOrWhiteSpace(apiBaseUrl))
    apiBaseUrl = builder.HostEnvironment.BaseAddress.TrimEnd('/');
var apiBaseUri = new Uri(apiBaseUrl + "/", UriKind.Absolute);
builder.Services.AddTransient<Bobeta.Web.Shared.Services.WasmApiFetchOptionsHandler>();
builder.Services.AddBobetaClient(
    http => http.BaseAddress = apiBaseUri,
    useBearerToken: true,
    b => b.AddHttpMessageHandler<Bobeta.Web.Shared.Services.WasmApiFetchOptionsHandler>());

await builder.Build().RunAsync();
