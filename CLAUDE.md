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

### 1. Initialize SDK (2-Tier Routing & Dynamic Market)
```csharp
HighbrowSDK.Initialize(new HighbrowConfig
{
    AppKey = "YOUR_APP_KEY",
    Market = targetMarket, // Dynamically resolved store (OneStore, GooglePlay, AppleStore, etc.)
    ClientVersion = Application.version, // Optional: defaults to Application.version
    UseSandbox = false, // true for dev/test, false for production
    EnableLog = true,
    AutoSessionTracking = true // 2-minute session heartbeat (Alive) starts automatically
});
```

### 2. Track Auth (Login)
```csharp
HighbrowLog.TrackAuth(suid, accountId, AccountType.GooglePlay, nickname);
```

### 3. Track IAP Purchase (Receipt)
```csharp
HighbrowLog.TrackPurchase(receiptId, originalPrice, priceId, currency, productId, productName);
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

## Partner Migration Rule
1. **CHANGELOG is the channel:** When an SDK change requires partner code changes, add a version entry to `CHANGELOG.md` in the same change (`변경 내용 / 찾는 방법 / 수정 방법 / 사람 판단 필요`, newest on top; write "코드 수정 없음" if none). Partners apply it through the update mode of `INTEGRATION_PROMPT.md`. Do not rely on separate per-partner notices.
2. **Keep versions and docs in sync:** Bump `package.json` and `SdkVersion`, and keep partner-facing docs (`AGENTS.md`, `README.md`, `INTEGRATION_GUIDE.md`, `INTEGRATION_PROMPT.md`) consistent with the behavior change.
3. **Update mode exists only from the final v1.0.1 (`6ead54d`):** Partners still on v1.0.0 cannot run it from their installed package. Tell them to change the tag in `Packages/manifest.json` and delete the `packages-lock.json` entry first, then run the update prompt and state their previous version.
