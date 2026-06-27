# Bobeta Hosting Guide

This guide covers what you need when hosting the Bobeta API and Web app in production. SignalR and the rest of the stack work after you publish, but a few configuration and host settings matter.

## 1. Publishing the apps

Publish both apps to **Azure Linux App Service** (see [DEPLOYMENT.md](./DEPLOYMENT.md) for profiles and portal setup):

| App | Project | Notes |
|-----|---------|--------|
| API | **`Bobeta.API`** | `linux-x64`, Zip Deploy profile **`bobeta - Zip Deploy`** |
| PWA | **`Bobeta.Web.Host`** | Serves **`Bobeta.Web`** (Blazor WASM); profile **`bobeta-pwa - Zip Deploy`** |

Enable **WebSockets** on the **API** app in the Azure Portal (**Configuration → General settings**). SignalR is part of the API; no separate install step.

For local development, run **`Bobeta.Web`** directly (`dotnet run`); use **`Bobeta.Web.Host`** only for production-style hosting and Azure publish.

## 2. Configuration (required)

When you host, you must configure the environment:

- **API**
  - **Connection string** — Points to your hosted PostgreSQL instance (not localhost).
  - **JWT** — Key, Issuer, and Audience (e.g. in `appsettings.Production.json` or environment variables) if they differ from development.

- **Web app**
  - **ApiBaseUrl** — Must point to your **hosted** API base URL (e.g. `https://api.yourdomain.com`). If this is wrong, the Blazor app and SignalR will call the wrong endpoint.

Set these via `appsettings.Production.json`, environment variables, or your host’s application settings.

## 3. Host-specific setup

### WebSockets (for SignalR)

SignalR uses WebSockets when possible. Depending on where you host:

- **IIS** — Enable WebSockets for the app (and for the Application Request Routing / ARR proxy if you use one).
- **Linux with Nginx or Apache** — Configure the reverse proxy to upgrade and forward WebSocket connections to the API. Without this, SignalR falls back to long polling (still works, but less efficient).
- **Azure App Service (or similar)** — WebSockets are usually available; enable them in the portal if they are turned off.

### CORS

If the Web app and API are served from different origins (e.g. `https://app.yourdomain.com` and `https://api.yourdomain.com`), ensure CORS is configured on the API to allow the Web app’s origin. If both are behind the same host or reverse proxy, you may not need CORS.

## 4. Scaling out (multiple API instances)

The current setup does not use a SignalR backplane (e.g. Redis). That is fine when you run **one** API instance: all clients connect to it and receive real-time updates.

If you run **multiple** API instances behind a load balancer, each instance has its own SignalR connections. A client connected to instance A will not receive broadcasts sent from instance B unless you add a **SignalR backplane** (e.g. Redis). For a single-instance deployment, no extra setup is needed; for multi-instance, you would add and configure a backplane.

## Summary

| Scenario | What you need |
|----------|----------------|
| Single server, correct config | Publish API and Web app; set connection string, JWT, and ApiBaseUrl; enable WebSockets on the host. |
| Reverse proxy (Nginx, IIS ARR) | Configure WebSocket upgrade and forwarding to the API. |
| Web and API on different domains | Configure CORS on the API for the Web app origin. |
| Multiple API instances | Add a SignalR backplane (e.g. Redis) and configure scale-out. |