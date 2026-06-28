# SMS Providers

Bobeta sends OTP and notification SMS through `ISmsService`, backed by pluggable providers (`ISmsProvider`) with a configurable default and optional fallbacks.

Configuration and credentials: [SETUP.md — SMS providers](./SETUP.md#10-sms-providers-smsportal--sendsmsgate).

## Active providers

### SMSPortal (default)

**Control panel:** [https://cp.smsportal.com/](https://cp.smsportal.com/)  
**REST API:** [https://docs.smsportal.com/docs/rest](https://docs.smsportal.com/docs/rest)

Hosted aggregator with a modern JSON REST API (`POST https://rest.smsportal.com/v3/BulkMessages`). Recommended primary provider.

**Implementation:** `Bobeta.Infrastructure/Sms/Providers/SmsPortalSmsProvider.cs`  
**DLR webhook:** `POST /api/sms/dlr/smsportal`

### SendSMSGate (fallback)

**Website:** [https://sendsmsgate.com](https://sendsmsgate.com)

Legacy HTTP API provider. Kept configured as fallback when SMSPortal is unavailable.

**Implementation:** `Bobeta.Infrastructure/Sms/Providers/SendSmsGateSmsProvider.cs`  
**DLR webhook:** `GET|POST /api/sms/dlr`

## Routing

`MultiProviderSmsService` reads `Sms:DefaultProvider` and `Sms:FallbackProviders`. Switch providers via configuration (e.g. Azure `Sms__DefaultProvider`) without code changes.

```json
{
  "Sms": {
    "DefaultProvider": "SmsPortal",
    "EnableFallback": true,
    "FallbackProviders": [ "SendSmsGate" ]
  }
}
```

## Planned provider at higher volume: SMS Gateway for Android

**Website:** [https://sms-gateway.app/](https://sms-gateway.app/)  
**Documentation:** [https://docs.sms-gate.app/](https://docs.sms-gate.app/)

When SMS volume grows, we may add a third `ISmsProvider` implementation for a dedicated Android device gateway (fixed SIM cost vs per-SMS aggregator fees). See the migration notes in the previous version of this doc for operational considerations.

To add it: implement `ISmsProvider`, register in DI, add settings section, and list it in `Sms:FallbackProviders` or set as `DefaultProvider`.

---

## Related code

| Area | Location |
| --- | --- |
| SMS service interface | `Bobeta.Application/Interfaces/ISmsService.cs` |
| Provider abstraction | `Bobeta.Infrastructure/Sms/ISmsProvider.cs` |
| Multi-provider router | `Bobeta.Infrastructure/Sms/MultiProviderSmsService.cs` |
| SMSPortal | `Bobeta.Infrastructure/Sms/Providers/SmsPortalSmsProvider.cs` |
| SendSMSGate | `Bobeta.Infrastructure/Sms/Providers/SendSmsGateSmsProvider.cs` |
| Routing config | `Bobeta.Infrastructure/Sms/SmsOptions.cs` |
| Delivery report endpoints | `Bobeta.API/Controllers/SmsController.cs` |
| OTP usage | `Bobeta.Identity/Services/AuthService.cs`, `OtpService.cs` |
