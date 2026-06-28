# SMS Providers

Bobeta sends OTP and notification SMS through `ISmsService`, backed by pluggable providers (`ISmsProvider`) with a configurable default and optional fallbacks.

Configuration and credentials: [SETUP.md — SMS providers](./SETUP.md#10-sms-providers-smsportal-twilio-sendsmsgate).

## Active providers

### SMSPortal (default)

**Control panel:** [https://cp.smsportal.com/](https://cp.smsportal.com/)  
**REST API:** [https://docs.smsportal.com/docs/rest](https://docs.smsportal.com/docs/rest)

**Implementation:** `Bobeta.Infrastructure/Sms/Providers/SmsPortalSmsProvider.cs`  
**DLR webhook:** `POST /api/sms/dlr/smsportal`

### Twilio

**Console:** [https://console.twilio.com/](https://console.twilio.com/)  
**Docs:** [https://www.twilio.com/docs/sms](https://www.twilio.com/docs/sms)

**Implementation:** `Bobeta.Infrastructure/Sms/Providers/TwilioSmsProvider.cs`  
**Status callback:** `POST /api/sms/dlr/twilio`

### SendSMSGate (fallback)

**Website:** [https://sendsmsgate.com](https://sendsmsgate.com)

**Implementation:** `Bobeta.Infrastructure/Sms/Providers/SendSmsGateSmsProvider.cs`  
**DLR webhook:** `GET|POST /api/sms/dlr`

## Routing

`MultiProviderSmsService` reads `Sms:DefaultProvider` and `Sms:FallbackProviders`. Switch providers via configuration (e.g. Azure `Sms__DefaultProvider`) without code changes.

```json
{
  "Sms": {
    "DefaultProvider": "SmsPortal",
    "EnableFallback": true,
    "FallbackProviders": [ "Twilio", "SendSmsGate" ]
  }
}
```

Unconfigured providers are skipped automatically.

---

## Related code

| Area | Location |
| --- | --- |
| SMS service interface | `Bobeta.Application/Interfaces/ISmsService.cs` |
| Provider abstraction | `Bobeta.Infrastructure/Sms/ISmsProvider.cs` |
| Multi-provider router | `Bobeta.Infrastructure/Sms/MultiProviderSmsService.cs` |
| SMSPortal | `Bobeta.Infrastructure/Sms/Providers/SmsPortalSmsProvider.cs` |
| Twilio | `Bobeta.Infrastructure/Sms/Providers/TwilioSmsProvider.cs` |
| SendSMSGate | `Bobeta.Infrastructure/Sms/Providers/SendSmsGateSmsProvider.cs` |
| Routing config | `Bobeta.Infrastructure/Sms/SmsOptions.cs` |
| Delivery report endpoints | `Bobeta.API/Controllers/SmsController.cs` |
| OTP usage | `Bobeta.Identity/Services/AuthService.cs`, `OtpService.cs` |
