# SMS Providers

Bobeta sends OTP and notification SMS through an `ISmsService` abstraction. The active provider is configured in the API; see [SETUP.md — SMS Gateway Setup (SendSMSGate)](./SETUP.md#10-sms-gateway-setup-sendsmsgate) for current credentials and DLR configuration.

## Current provider: SendSMSGate

**Website:** [https://sendsmsgate.com](https://sendsmsgate.com)

SendSMSGate is used today for OTP login and notification SMS. It is a hosted SMS aggregator with per-message pricing and provider-side sender-ID moderation.

**When it fits:** Early stage and low volume, where setup simplicity and a managed aggregator outweigh per-SMS cost.

**Implementation:** `Bobeta.Infrastructure/Sms/SmsGatewayService.cs` (SendSMSGate HTTP API).

---

## Planned provider at higher volume: SMS Gateway for Android

**Website:** [https://sms-gateway.app/](https://sms-gateway.app/)  
**Documentation:** [https://docs.sms-gate.app/](https://docs.sms-gate.app/)

When SMS volume grows, we plan to move to [SMS Gateway for Android](https://sms-gateway.app/) instead of scaling SendSMSGate spend linearly with message count.

### Why switch at scale

| | SendSMSGate (current) | SMS Gateway for Android (planned) |
| --- | --- | --- |
| **Cost model** | Per-SMS provider fees | Fixed cost: own Android device + SIM/carrier plan |
| **Scaling economics** | Cost rises with every message | Marginal cost per message is effectively zero beyond SIM/plan |
| **Infrastructure** | Fully managed aggregator | Android phone acts as the gateway; optional cloud relay |

SendSMSGate charges per message. SMS Gateway for Android routes messages through hardware we control (an Android phone and SIM). At higher volume, that fixed-fee structure is cheaper than paying per SMS through an aggregator.

The project’s public cloud relay is currently free for existing features ([pricing](https://docs.sms-gate.app/pricing/)); message cost is then dominated by the SIM/carrier plan rather than a per-SMS gateway tariff.

### How it works (high level)

1. Install the SMS Gateway app on a dedicated Android device with a suitable SIM/plan.
2. Enable **Cloud Server** mode so the API can reach the device without a static device IP ([public cloud server guide](https://docs.sms-gate.app/getting-started/public-cloud-server/)).
3. The app generates Basic Auth credentials automatically on first cloud connection.
4. Bobeta’s API sends messages via HTTP POST to the cloud API, for example:

   ```http
   POST https://api.sms-gate.app/3rdparty/v1/messages
   Authorization: Basic <username>:<password>
   Content-Type: application/json

   {
     "textMessage": { "text": "Your Bobeta code is 123456" },
     "phoneNumbers": ["+256700000000"]
   }
   ```

5. The Android device sends SMS through the SIM; delivery reports and inbound SMS can be handled via webhooks ([sending messages](https://docs.sms-gate.app/features/sending-messages/), [API integration](https://docs.sms-gate.app/integration/api/)).

### Operational considerations before migration

- **Dedicated hardware:** Use a reliable Android device on power, with a SIM/plan sized for expected OTP and notification volume.
- **Uptime:** Cloud mode depends on the device and Google Play Services (FCM) or SSE fallback; plan for redundancy if SMS becomes business-critical.
- **Compliance:** Sender identity follows the SIM/number on the device, not a separate SendSMSGate sender-ID registration flow.
- **Code changes:** Implement a new `ISmsService` adapter for the SMS Gateway API, add configuration (base URL, credentials, webhook URLs), and map delivery status to existing `SmsMessage` records. Keep SendSMSGate available behind configuration until cutover is validated.

### Migration trigger (guideline)

Re-evaluate the provider when monthly SendSMSGate spend materially exceeds the all-in cost of a dedicated Android gateway (device, SIM/plan, and any hosting). Exact thresholds depend on destination markets and carrier rates.

---

## Related code

| Area | Location |
| --- | --- |
| SMS service interface | `Bobeta.Application/Interfaces/ISmsService.cs` |
| SendSMSGate implementation | `Bobeta.Infrastructure/Sms/SmsGatewayService.cs` |
| Configuration | `Bobeta.Infrastructure/Sms/SmsGatewaySettings.cs`, `Bobeta.API/appsettings.json` |
| Delivery report endpoint | `Bobeta.API/Controllers/SmsController.cs` (`/api/sms/dlr`) |
| OTP usage | `Bobeta.Identity/Services/AuthService.cs`, `OtpService.cs` |
