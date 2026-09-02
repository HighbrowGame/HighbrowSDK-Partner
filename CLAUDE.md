# Highbrow SDK - Claude Code & AI Agent Guidelines (`CLAUDE.md`)

This file provides system context and integration instructions for Claude Code (CLI) and Anthropic Claude-based tools when working with `HighbrowSDK`.

---

## Quick Reference
For the full AI agent guide, see [AGENTS.md](file:///Volumes/ExtDisk01/Projects/Jobs/HighbrowSDK-Partner/AGENTS.md).

## Key Namespaces
- `using Highbrow.Core;` (SDK initialization, config, context)
- `using Highbrow.Log;` (Log APIs, Enums, Models)
- `using Highbrow.Ad;` (In-house cross promotion ads)

## Essential API Patterns (4 Core Fact Logs)

### 1. Initialize SDK (2-Tier Routing)
```csharp
HighbrowSDK.Initialize(new HighbrowConfig
{
    AppKey = "YOUR_APP_KEY",
    UseSandbox = false, // true for dev/test, false for production
    EnableLog = true,
    AutoSessionTracking = true // 5-minute session heartbeat (Alive) starts automatically
});
```

### 2. Track Auth (Login)
```csharp
HighbrowLog.TrackAuth(suid, accountId, AccountType.GooglePlay, nickname);
```

### 3. Track IAP Purchase (Receipt)
```csharp
HighbrowLog.TrackPurchase(receiptId, price, priceId, productId, productName);
```

### 4. Track Ad & Show Highbrow House Ads
```csharp
// Third-party ad impression
HighbrowLog.TrackAd(AdType.RewardVideo);

// Highbrow in-house house ad
HighbrowAd.Show(onCompleted: () => { /* reward */ });
```

## Architectural Guardrails
1. **4 Core Fact Logs Only:** Client only emits `Auth`, `Alive` (Auto), `Purchase`, and `Advertise`.
2. **Server-Derived Metrics:** New User, First Purchase, and DAU/DADU are derived automatically on the Highbrow Collector backend via state DB. Never write client-side logic to determine these.
3. **Surgical Modifications:** Only add SDK tracking calls at exact success/callback locations without altering surrounding game logic.
