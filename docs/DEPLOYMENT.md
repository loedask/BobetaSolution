# Deployment notes (latency & regions)

## API region (already configured)

The API publish profile targets **South Africa North** (`southafricanorth-01.azurewebsites.net`). That is a reasonable choice for players in Cameroon and Central/West Africa.

To change region: Azure Portal → App Service **bobeta** → **Settings** → **Scale up (App Service plan)** / recreate the app in the desired region. You cannot move an existing app across regions in place; clone or redeploy to a new app in the target region.

## PWA (Blazor WASM host)

The PWA profile (`bobeta-pwa`) should run in the **same region as the API** so static assets and API round-trips stay low-latency. If the PWA app is still on a default US region:

1. Create (or move) `bobeta-pwa` in **South Africa North** (same resource group as the API is fine).
2. Point `ApiBaseUrl` in PWA configuration to the API URL.
3. Redeploy the Web project using the updated publish profile.

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
