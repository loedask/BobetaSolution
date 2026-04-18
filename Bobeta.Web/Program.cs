using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Bobeta.Web;
using Bobeta.Web.Services;
using Bobeta.Web.ViewModels.Auth;
using Bobeta.Web.ViewModels.Dashboard;
using Bobeta.Web.ViewModels.Wallet;
using Bobeta.Web.ViewModels.Games;
using Bobeta.Web.ViewModels.Profile;
using Bobeta.Web.Services.Realtime;
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

builder.Services.AddScoped<PhoneLoginViewModel>();
builder.Services.AddScoped<OtpVerificationViewModel>();
builder.Services.AddScoped<CreatePlayerViewModel>();
builder.Services.AddScoped<DashboardViewModel>();
builder.Services.AddScoped<DepositViewModel>();
builder.Services.AddScoped<WithdrawViewModel>();
builder.Services.AddScoped<JoinGameViewModel>();
builder.Services.AddScoped<CreateGameViewModel>();
builder.Services.AddScoped<GameHistoryViewModel>();
builder.Services.AddScoped<ProfileViewModel>();
builder.Services.AddScoped<GameHubClient>();
builder.Services.AddScoped<GamePlayTestService>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
// WASM uses fetch(); GET responses can be cached without varying on Authorization, causing 401s after OTP login.
builder.Services.AddTransient<WasmApiFetchOptionsHandler>();
builder.Services.AddBobetaClient(
    http => http.BaseAddress = new Uri(apiBaseUrl),
    useBearerToken: true,
    b => b.AddHttpMessageHandler<WasmApiFetchOptionsHandler>());

await builder.Build().RunAsync();
