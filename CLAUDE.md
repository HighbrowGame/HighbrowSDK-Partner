# Highbrow SDK - Claude Code & AI Agent Guidelines (`CLAUDE.md`)

This file provides system context and integration instructions for Claude Code (CLI) and Anthropic Claude-based tools when working with `HighbrowSDK`.

---

## Quick Reference
For the full AI agent guide, see [AGENTS.md](file:///Volumes/ExtDisk01/Projects/Jobs/HighbrowSDK-Partner/AGENTS.md).

## Key Namespaces
- `using Highbrow.Core;` (SDK initialization, config, context)
- `using Highbrow.Log;` (Log APIs, Enums, Models)

## Essential API Patterns

### 1. Initialize SDK (2-Tier Routing)
```csharp
HighbrowSDK.Initialize(new HighbrowConfig
{
    AppKey = "YOUR_APP_KEY",
    UseSandbox = false, // true for dev/test, false for production
    Region = "kr",
    EnableLog = true,
    AutoSessionTracking = true
});
```

### 2. Track Auth
```csharp
HighbrowLog.TrackAuth(suid, accountId, AccountType.GooglePlay, nickname);
// Only call TrackNewUser if the game can distinguish new accounts:
// HighbrowLog.TrackNewUser(suid, AccountType.GooglePlay);
```

### 3. Track IAP Purchase
```csharp
HighbrowLog.TrackPurchase(receiptId, price, priceId, productId, productName, isFirstPurchase: false);
```

### 4. Track Ad
```csharp
HighbrowLog.TrackAd(AdType.RewardVideo, isComplete: true, userAdSkipPackage: false);
```

## Partner Limitations Guardrails
1. **Unknown New User:** Do not call `HighbrowLog.TrackNewUser()`. `TrackAuth` alone is sufficient.
2. **Unknown First Purchase:** Set `isFirstPurchase: false`. The backend handles lifetime first-purchase calculation.
3. **Surgical Modifications:** Only add SDK tracking calls at exact success/callback locations without altering surrounding game logic.
