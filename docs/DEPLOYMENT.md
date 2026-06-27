# Deployment notes (latency & regions)

## Azure Linux (recommended)

Both production apps run on **Linux App Service** in **South Africa North**:

| App | Project to publish | Publish profile | Runtime |
|-----|-------------------|-----------------|---------|
| **bobeta** (API) | `Bobeta.API` | `bobeta - Zip Deploy` | .NET 10, `linux-x64` |
| **bobeta-pwa** (PWA) | `Bobeta.Web.Host` | `bobeta-pwa - Zip Deploy` | .NET 10, `linux-x64` |

**Do not** publish `Bobeta.Web` directly to App Service — standalone Blazor WASM is static files only; Linux App Service needs the thin **`Bobeta.Web.Host`** server for SPA routing and correct MIME types. Local dev can still use `dotnet run` on **`Bobeta.Web`**.

### One-time Azure Portal setup

**API (`bobeta`)** — should already be Linux (publish profile sets `IsLinux` + `linux-x64`). Confirm:

1. **Settings → Configuration → General settings** → **Stack**: .NET, **Major version**: .NET 10, **Platform**: Linux.
2. **Web sockets**: **On** (required for SignalR).
3. Connection string, JWT, and optional **`Azure__SignalR__ConnectionString`** as in [HOSTING.md](./HOSTING.md).

**PWA (`bobeta-pwa`)** — if the app was created as **Windows**, you **cannot** flip it to Linux in place. Either:

- **Recreate** `bobeta-pwa` as a **Linux** Web App (.NET 10) in the same region and resource group, then redeploy; or
- Create a new Linux app (e.g. `bobeta-pwa-linux`) and update DNS / custom domain when ready.

For a **new Linux** PWA app:

1. **Create** → Web App → **Linux**, runtime **.NET 10**.
2. Same **App Service plan** as the API is fine (or a separate plan).
3. **Configuration → General settings** → **Web sockets**: **On** (harmless for static/WASM; keeps settings consistent).
4. No `ApiBaseUrl` in App Service config — it lives in **`Bobeta.Web/wwwroot/appsettings.Production.json`** (baked into the WASM build at publish time).

### Publish from Visual Studio or CLI

**API:**

```bash
dotnet publish Bobeta.API/Bobeta.API.csproj -c Release -r linux-x64 --self-contained false
# Or: Publish with profile "bobeta - Zip Deploy"
```

**PWA:**

```bash
dotnet publish Bobeta.Web.Host/Bobeta.Web.Host.csproj -c Release -r linux-x64 --self-contained false
# Or: Publish with profile "bobeta-pwa - Zip Deploy"
```

Before publishing the PWA, set **`ApiBaseUrl`** in **`Bobeta.Web/wwwroot/appsettings.Production.json`** to your API URL.

## API region (already configured)

The API publish profile targets **South Africa North** (`southafricanorth-01.azurewebsites.net`). That is a reasonable choice for players in Cameroon and Central/West Africa.

To change region: Azure Portal → App Service **bobeta** → **Settings** → **Scale up (App Service plan)** / recreate the app in the desired region. You cannot move an existing app across regions in place; clone or redeploy to a new app in the target region.

## PWA (Blazor WASM host)

The PWA profile (`bobeta-pwa`) should run in the **same region as the API** so static assets and API round-trips stay low-latency.

1. Ensure **bobeta-pwa** is a **Linux** Web App (see above).
2. Point **`ApiBaseUrl`** in **`Bobeta.Web/wwwroot/appsettings.Production.json`** to the API URL.
3. Publish **`Bobeta.Web.Host`** using **`bobeta-pwa - Zip Deploy`**.

The legacy Windows profile on **`Bobeta.Web`** (`bobeta-pwa - Web Deploy`) is deprecated; use **`Bobeta.Web.Host`** instead.

## Response compression

The API enables **Brotli/Gzip** for JSON and other responses (see `Bobeta.API/Program.cs`). No extra App Service setting is required.

## Blazor WASM lazy-loaded pages

Lobby, wallet, and profile routes (`/dashboard`, `/history`, `/join`, etc.) are in the **`Bobeta.Web.Deferred`** assembly and download only when first visited. Login and **`/game/...`** stay in the main bundle for faster game entry.

After publish, confirm `_framework/Bobeta.Web.Deferred.wasm` exists under `wwwroot`.

## API contract (OpenAPI)

The HTTP client uses hand-maintained DTOs in `Bobeta.Client/Models/Api/` and `BaseHttpService` — there is no NSwag codegen. A reference OpenAPI export lives at `docs/openapi/bobeta-api.swagger.json` (regenerate from the API when endpoints change).

## Verify latency

From a browser devtools **Network** tab on the game page, check `play-card` and `state` requests:

- **TTFB** under ~300 ms on mobile networks in-region is typical.
- High TTFB with small payloads usually means geographic distance or cold start, not bandwidth.
