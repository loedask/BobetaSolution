# Phone push (FCM / APNs)

Bobeta sends inbox + SignalR while the app is open. For closed or background phones it uses **Firebase Cloud Messaging** (FCM). iOS uses APNs through the same Firebase project when you upload your APNs key in the Firebase console.

## 1. Firebase project

1. Create a Firebase project and add an **Android** app with package `com.bobeta.mobile`.
2. Download the real `google-services.json` and replace `Bobeta.Mobile/Platforms/Android/google-services.json` (the repo ships a placeholder so the project builds).
3. For iOS, add an iOS app, download `GoogleService-Info.plist`, and upload your APNs auth key under Firebase Cloud Messaging.
4. Create a **service account** with Firebase Cloud Messaging Admin (or use the Firebase Admin SDK JSON from Project settings → Service accounts).

## 2. API configuration

In `Bobeta.API` appsettings, user-secrets, or Azure App Settings:

```json
"Fcm": {
  "Enabled": true,
  "ProjectId": "your-firebase-project-id",
  "CredentialsJson": "{ ... service account json ... }"
}
```

Prefer `CredentialsJson` via user-secrets / Key Vault. You can use `CredentialsPath` instead for a file on disk.

Apply the EF migration that adds `PlayerDeviceTokens`:

```bash
cd Bobeta.API
dotnet ef database update
```

## 3. Flow

1. After login, the mobile app requests notification permission and registers the FCM token at `POST api/Devices/register`.
2. When someone joins your waiting table (and for other inbox events), `NotificationService` writes the inbox row, publishes SignalR, then sends FCM to stored tokens.
3. Invalid tokens are removed automatically.

Until `Fcm:Enabled` is true with valid credentials, push is a no-op and the app still works with SignalR + inbox.
