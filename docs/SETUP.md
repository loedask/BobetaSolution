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

For API details (e.g. Swagger), open the API base URL in the browser (e.g. `https://localhost:5001/swagger`).
