# Bobeta Developer Setup Guide

This guide walks you through running the Bobeta platform and Makopa game locally.

## 1. Install prerequisites

- **.NET 8+** (or .NET 10 if the solution targets it) — [Download](https://dotnet.microsoft.com/download)
- **Node.js** (optional) — only if you need to run Tailwind or other front-end tooling; the Web project can use the CDN Tailwind build as-is.
- **PostgreSQL** — for the API database.

## 2. Run the database

1. Ensure PostgreSQL is running and you can create a database.
2. Update the connection string in **Bobeta.API/appsettings.json** if needed:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=Bobeta;Username=postgres;Password=postgres"
   }
   ```

   **Azure App Service (production):** If your portal **Connection strings** entry is named **`AZURE_POSTGRESQL_CONNECTIONSTRING`** (common for Azure Database for PostgreSQL integrations), the API uses that first, then falls back to **`DefaultConnection`** for local dev. No need to rename the Azure entry to `DefaultConnection`.

3. Apply migrations from the API project directory:

   ```bash
   cd Bobeta.API
   dotnet ef database update
   ```

## 3. Run the API

```bash
cd Bobeta.API
dotnet run
```

The API listens on the URLs in **`Bobeta.API/Properties/launchSettings.json`**. Kestrel binds to **`0.0.0.0`** on **`https://7029`** and **`http://5163`** so **Android emulators** (`10.0.2.2`) and **phones on your LAN** can reach the host, not only `localhost`. Use **HTTPS** for browser/Web (`https://localhost:7029`); **Bobeta.Mobile** Debug defaults to **HTTP** (`http://10.0.2.2:5163`) to avoid dev-certificate trust issues on devices. Note the exact URLs in the console.

### Azure SignalR (production scale-out)

**Development / Staging (default):** Leave **`Azure:SignalR:ConnectionString`** unset. The API uses in-process SignalR on the single instance.

**Production (e.g. Azure App Service with multiple instances):** Create [Azure SignalR Service](https://learn.microsoft.com/azure/azure-signalr/signalr-resource-create) in **Default** mode for this ASP.NET Core app. In App Service **Configuration**, add **`Azure__SignalR__ConnectionString`** (application setting) with the resource’s connection string. That maps to configuration key **`Azure:SignalR:ConnectionString`**. Hub URLs for clients stay **`/hubs/game`** on your API base URL.

To exercise Azure SignalR on your machine only, from **`Bobeta.API`**: `dotnet user-secrets set "Azure:SignalR:ConnectionString" "<your-connection-string>"`.

## 4. Run the Web app

1. Open **`Bobeta.Web/wwwroot/appsettings.json`** (or **`wwwroot/appsettings.Development.json`**) and set **`ApiBaseUrl`** to the API base URL (defaults to **`https://localhost:7029`** to match the API’s HTTPS profile).
2. Run the Web project:

   ```bash
   cd Bobeta.Web
   dotnet run
   ```

The Blazor app will typically be available at **http://localhost:5034** or **https://localhost:7181** (see **`Bobeta.Web/Properties/launchSettings.json`** and the console).

### Bobeta.Mobile — `ApiBaseUrl`

Configuration is merged at startup in this order (later wins):

| File | When |
|------|------|
| **`appsettings.json`** | Always embedded; default **`ApiBaseUrl`** in the repo is the **Azure** (or other stable) API. |
| **`appsettings.Development.json`** | **Debug** only. Defaults to **`http://10.0.2.2:5163`** for the **Android emulator** (HTTP avoids trusting the ASP.NET dev HTTPS cert on the device). **Debug Android** sets **`AndroidUsesCleartextTraffic`** so HTTP to the local API is allowed. For a **physical phone**, use your PC’s LAN IP (e.g. `http://192.168.1.10:5163`). For **Windows MAUI** on the same PC as the API, use **`https://localhost:7029`** (or `http://localhost:5163`). |
| **`appsettings.Production.json`** | **Release** builds only; keep your store / stable **`ApiBaseUrl`** here (often same Azure host as the base file). |

- **Release** builds therefore keep **Azure** (or whatever you put in Production) without editing Development.
- **Debug** builds default to **local API** via Development; switch back to a remote URL by temporarily changing **`appsettings.Development.json`** or building **Release** when the device cannot reach your PC.
- If you still see **network unreachable** to `10.0.2.2`, confirm the API is running with the **`https`** (or **`http`**) profile, **Windows Firewall** allows **dotnet**/Kestrel on those ports, and you are on the **Google Android Emulator** (`10.0.2.2` does not apply the same way on every stack or on a physical device—use your LAN IP there).

PostgreSQL must be running for a local API; migrations run on startup only when **`ASPNETCORE_ENVIRONMENT`** is **Development** or **Staging** — otherwise a missing database can surface as **500** on send-otp.

## 5. Open the browser

Navigate to:

**http://localhost:5002**

## 6. Demo test accounts (Development / Staging)

These accounts are for **local or non-production** environments only. Demo seeding and static OTP are **disabled in Production** (`DemoEnvironmentHelper`).

| Item | Value |
|------|--------|
| **Demo phone 1** | `+242700000001` (in the app: country **+242**, national **`700000001`**) |
| **Demo phone 2** | `+242700000002` (national **`700000002`**) |
| **Static OTP** (when enabled) | `123456` — see **`DemoAuth`** in `Bobeta.API/appsettings.Development.json` and `appsettings.Staging.json` |
| **Wallet** | Each seeded player gets **100,000** balance (see `DemoAccountsSeeder`) |

**How it works**

1. On startup, the API runs migrations and **`DemoAccountsSeeder`** (Development / Staging only) and creates the two players if missing.
2. **`OtpService`** accepts the configured **`DemoAuth:StaticOtp`** for numbers listed in **`DemoAuth:PhoneNumbers`** only when `EnableStaticOtp` is true and the host is Development or Staging.
3. Source of truth for phone constants: `Bobeta.Persistence/Seeding/DemoAccountsSeeder.cs`.

For any **new** number (not seeded), you still use a real SMS OTP unless you add that number to `DemoAuth:PhoneNumbers` (non-production only).

## 7. Test flow

1. **Login** — e.g. demo phone **+242** / **700000001** and OTP **123456** in Development, or your real SMS flow.
2. **Create player** — skipped for seeded demos (already registered); required for new numbers.
3. **Deposit** — add balance from the wallet/dashboard.
4. **Join game** — create or join a game from the dashboard.
5. Open the game page: **/game/{sessionId}** (you can get the session ID from the join/create response or dashboard).

## 8. Multiplayer testing

To test real-time multiplayer:

1. Open **two browser windows** (or one normal and one incognito).
2. Log in as two different players (or two accounts).
3. Have one player create a game and the other join it.
4. Start the game and open **/game/{sessionId}** in both windows.
5. Play cards in turn; moves should appear in real time via SignalR.

### Local single-browser test (AI opponent)

If no second player joins, the app can simulate an AI opponent for local testing:

- After **5 seconds** with no opponent move, the client calls the test endpoint to simulate one AI move (random valid card, 1 second delay).
- This allows testing the full flow in a single browser.

---

## 9. MTN MoMo Setup

To enable production-ready Mobile Money (MoMo) payments (deposits and withdrawals) via MTN:

### 1. Create account at MoMo Developer Portal

1. Go to [https://momodeveloper.mtn.com](https://momodeveloper.mtn.com).
2. Sign up or sign in to the developer portal.
3. Use the **Sandbox** for testing; switch to **Production** when going live.

### 2. Create API User

1. In the portal, create an **API User** under your product (Collection and/or Disbursement).
2. Note the **API User ID** (e.g. a UUID).
3. **Generate API Key** for that user and store it securely (it is shown only once).

### 3. Create Collection and Disbursement subscriptions

1. Subscribe to **Collection** (for request-to-pay / deposits) and **Disbursement** (for transfers / withdrawals).
2. For each product, create a **Subscription** and note the **Primary Key** (subscription key).
3. You will need:
   - **Collection Primary Key** — for deposit (request-to-pay) API calls.
   - **Disbursement Primary Key** — for withdrawal (transfer) API calls.

### 4. Configure appsettings.json

Add or update the `MoMo` section in **Bobeta.API/appsettings.json** (use **User Secrets** or environment variables in production; do not commit real keys):

```json
{
  "MoMo": {
    "BaseUrl": "https://sandbox.momodeveloper.mtn.com",
    "SubscriptionKey": "your-subscription-key",
    "ApiUser": "your-api-user-id",
    "ApiKey": "your-api-key",
    "CollectionPrimaryKey": "your-collection-primary-key",
    "DisbursementPrimaryKey": "your-disbursement-primary-key",
    "CallbackUrl": "https://your-api-host/api/payments/momo/callback",
    "CallbackSubscriptionKey": "your-collection-or-callback-subscription-key",
    "TargetEnvironment": "mtuganda",
    "Currency": "UGX",
    "UseSandbox": true
  }
}
```

- **CallbackUrl**: Must be a publicly reachable URL where MTN will send payment status callbacks. For local testing you can use a tunnel (e.g. ngrok) and set this to your tunnel URL plus `/api/payments/momo/callback`.
- **CallbackSubscriptionKey**: Used to validate incoming callbacks: the request header `Ocp-Apim-Subscription-Key` must match this value (e.g. use your Collection primary key). Callbacks that do not match return 401.
- **TargetEnvironment**: Use the value for your country/sandbox (e.g. `mtuganda` for Uganda sandbox). Incoming callbacks must send the same value in `X-Target-Environment`.
- For **production**, set `BaseUrl` and `TargetEnvironment` to the production values and `UseSandbox` to `false`.

### 5. Apply migrations

Ensure the **PaymentTransactions** table exists (from the MoMo integration migration):

```bash
cd Bobeta.API
dotnet ef database update
```

After configuration, the API will accept deposit and withdrawal requests via `/api/payments/deposit` and `/api/payments/withdraw`, and the **PaymentStatusWorker** will poll pending payments every 60 seconds so that completed payments update the wallet even if a callback is missed.

---

## 10. SMS Gateway Setup (SendSMSGate)

To send OTP and notification SMS (e.g. for phone verification) via SendSMSGate:

### 1. Create account at SendSMSGate

1. Go to [https://sendsmsgate.com](https://sendsmsgate.com).
2. Sign up or sign in and obtain your API credentials (user/login and password).

### 2. Generate API credentials

1. In your SendSMSGate account, note your **user** (login) and **password** for the HTTP API.
2. Configure a **Sender ID** (alphanumeric, max 11 characters, or digital up to 15) that will appear as the SMS sender.

### 3. Configure SmsGatewaySettings

Add or update the `SmsGatewaySettings` section in **Bobeta.API/appsettings.json** (use **User Secrets** or environment variables in production; do not commit real credentials):

```json
{
  "SmsGatewaySettings": {
    "BaseUrl": "https://cloud.sendsmsgate.com",
    "Username": "your-sendsmsgate-login",
    "Password": "your-sendsmsgate-password",
    "SenderId": "Bobeta",
    "DeliveryReportUrl": "https://your-api-host/api/sms/dlr"
  }
}
```

- **BaseUrl**: SendSMSGate API base (default `https://cloud.sendsmsgate.com`).
- **Username** / **Password**: Your SendSMSGate HTTP API credentials.
- **SenderId**: Sender name/number shown on SMS (must pass moderation on SendSMSGate).
- **DeliveryReportUrl**: Public URL where SendSMSGate will send delivery reports (DLR). Must be reachable from the internet (e.g. your deployed API URL + `/api/sms/dlr`). For local testing you can use a tunnel (e.g. ngrok) and set this to your tunnel URL plus `/api/sms/dlr`.

### 4. Configure Delivery Report (DLR) endpoint

1. In your SendSMSGate account, set the DLR callback URL to the same value as **DeliveryReportUrl** (e.g. `https://your-api-host/api/sms/dlr`).
2. The API accepts both GET and POST at `/api/sms/dlr` with parameters `smsid` (provider message ID) and `status` (`send`, `deliver`, `not_deliver`, `expired`). When SendSMSGate calls this URL, the platform updates the corresponding SMS record status.

After configuration, OTP messages (e.g. for login) are sent via SendSMSGate, and delivery status is updated when DLR callbacks are received.

---

For API details (e.g. Swagger), open the API base URL in the browser (e.g. `https://localhost:7029/swagger` when using the API’s HTTPS profile).
