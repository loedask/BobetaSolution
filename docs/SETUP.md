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

The API will listen on the configured port (e.g. `https://localhost:5001` or similar). Note the URL for the next step.

## 4. Run the Web app

1. Open **Bobeta.Web/appsettings.json** (or use launch settings) and set **ApiBaseUrl** to the API base URL (e.g. `https://localhost:5001`).
2. Run the Web project:

   ```bash
   cd Bobeta.Web
   dotnet run
   ```

The Blazor app will typically be available at **http://localhost:5002** (or the port shown in the console).

## 5. Open the browser

Navigate to:

**http://localhost:5002**

## 6. Test flow

1. **Login** — use the auth flow (e.g. phone + OTP).
2. **Create player** — if required for first-time setup.
3. **Deposit** — add balance from the wallet/dashboard.
4. **Join game** — create or join a game from the dashboard.
5. Open the game page: **/game/{sessionId}** (you can get the session ID from the join/create response or dashboard).

## 7. Multiplayer testing

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

## MTN MoMo Setup

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

For API details (e.g. Swagger), open the API base URL in the browser (e.g. `https://localhost:5001/swagger`).
