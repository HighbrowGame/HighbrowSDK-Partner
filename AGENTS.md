# Highbrow SDK - AI Agent Integration Guide (`AGENTS.md`)

This guide instructs AI coding agents (Cursor, Copilot, Claude, Windsurf, etc.) on how to safely integrate and use `HighbrowSDK` in Unity partner projects without hallucinations or breaking existing game logic.

---

## 1. Core Architecture & Rules

1. **Namespaces:**
   - Core SDK: `using Highbrow.Core;`
   - Log Module: `using Highbrow.Log;`
   - Ad Module: `using Highbrow.Ad;`
2. **Surgical Edits Only:**
   - Do NOT refactor or rewrite the partner's existing game logic, login handlers, IAP flows, or ad callbacks.
   - Insert only the required `HighbrowLog.Track...()` or `HighbrowAd.Show(...)` calls at the exact completion/success points.
3. **Decoupled Modules:**
   - The Log module does NOT depend on Ad. Use `HighbrowLog` directly.
   - `HighbrowAdPlayer` automatically logs `TrackAd` events when played.

---

## 2. SDK Initialization (2-Tier Routing: Sandbox vs Production)

Initialize once in the game's startup bootstrap/splash script (e.g. `GameInitializer.cs` or `TitleManager.cs`):

```csharp
using Highbrow.Core;
using Highbrow.Log;

HighbrowConfig config = new HighbrowConfig
{
    AppKey = "PARTNER_APP_KEY",         // Issued by Highbrow
    UseSandbox = false,                 // true: Sandbox/DEV collector, false: Production collector
    Region = "kr",                     // "kr", "us", "dev", "qa", etc.
    EnableLog = true,
    AutoSessionTracking = true,        // Automatically sends 5-min heartbeat
    SessionIntervalSeconds = 300f,
    DebugMode = false                  // Set false in production
};

HighbrowSDK.Initialize(config);
```

---

## 3. Public API Reference & Integration Rules

### 1) Authentication & Login (`TrackAuth`)
- **Call Location:** Immediately after user login success (Google, Apple, Guest, Facebook, etc.).
- **Signature:**
  ```csharp
  HighbrowLog.TrackAuth(
      string suid,                  // Game user unique ID (as string)
      string accountId,             // Social platform ID (e.g. Google sub, Apple user ID)
      AccountType accountType,      // AccountType.GooglePlay, AccountType.AppleId, AccountType.Guest, etc.
      string nickname,              // User display name (or string.Empty)
      string result = "OK",         // Auth result status (default "OK")
      string ipAddress = null       // Optional IP string
  );
  ```
- **Gotcha (New User):**
  - If the game can determine if this is a new user/character: call `HighbrowLog.TrackNewUser(suid, accountType);` right after `TrackAuth`.
  - If the game **cannot** determine new user status: **SKIP** `TrackNewUser`. Do not invent speculative logic.

### 2) Session Tracking (`SessionTracking`)
- If `config.AutoSessionTracking = true` was set in `Initialize()`, session heartbeat (every 5 mins) runs **automatically** in the background. No manual call needed.
- If manual control is required:
  ```csharp
  HighbrowLog.StartSessionTracking(300f); // Start 5-min periodic heartbeat
  HighbrowLog.StopSessionTracking();      // Stop on logout / title return
  ```

### 3) In-App Purchase (`TrackPurchase`)
- **Call Location:** In the IAP receipt verification / purchase success callback (e.g. Unity IAP `ProcessPurchase`).
- **Signature:**
  ```csharp
  HighbrowLog.TrackPurchase(
      string receiptId,             // Apple transactionID or Google orderId
      float price,                  // Product price (e.g. 0.99f)
      string priceId,               // Store item SKU (e.g. "com.game.gem_100")
      int productId,                // Internal numeric product ID
      string productName,           // Product display name
      DateTime? purchaseTime = null,// Null defaults to DateTime.UtcNow
      bool isFirstPurchase = false, // True only if verified first purchase
      string suid = null            // Optional SUID override
  );
  ```
- **Gotcha (First Purchase):**
  - If the game tracks first purchase state: pass `isFirstPurchase: true`.
  - If the game **cannot** determine first purchase: pass `isFirstPurchase: false` (the backend will compute new paying status).

### 4) Advertisements (`TrackAd`)
- **Call Location:** In the ad mediation callbacks (AppLovin MAX, IronSource, AdMob, etc.).
- **Impression Start:**
  ```csharp
  HighbrowLog.TrackAd(AdType.RewardVideo, isComplete: false);
  ```
- **Impression Complete (Rewarded):**
  ```csharp
  HighbrowLog.TrackAd(AdType.RewardVideo, isComplete: true, userAdSkipPackage: hasNoAdsPass);
  ```
- **AdType Enum:** `AdType.RewardVideo`, `AdType.Interstitial`, `AdType.Banner`, `AdType.CrossPromotion`.

### 5) In-House Cross Promotion Ads (`HighbrowAd`)
- **Call Location:** When showing Highbrow house ads (e.g. mediation no-fill fallback, cross-promotion button).
- **One-Line Display:**
  ```csharp
  using Highbrow.Ad;

  HighbrowAd.Show(
      onCompleted: () => {
          // Grant reward or resume game
      },
      onFailed: () => {
          // Fallback handling
      }
  );
  ```
- **Prefab / Resources:** Loads `UI_HighbrowAd` directly from Resources (no Addressables needed). Automatically logs `TrackAd(AdType.CrossPromotion)`.

---

## 4. Enums Reference

```csharp
namespace Highbrow.Log
{
    public enum AccountType : sbyte
    {
        All = -1, None = 0, AppleGameCenter = 1, GooglePlay = 2,
        Facebook = 3, Steam = 4, AppleId = 5, GameCenterTeam = 6,
        Vng = 7, HighbrowId = 8, Guest = 9
    }

    public enum MarketType : sbyte
    {
        None = 0, AppleStore = 1, GooglePlay = 2, Steam = 3,
        OneStore = 4, SamsungStore = 5, VngWeb = 6, All = 100
    }

    public enum OsType : sbyte
    {
        None = 0, iOS = 1, Android = 2, OSX = 3, Windows = 4, Linux = 5, PC = 6
    }

    public enum AdType : sbyte
    {
        None = 0, RewardVideo = 1, Interstitial = 2, Banner = 3,
        CrossPromotion = 4, Offerwall = 5
    }
}
```

---

## 5. Offline & Network Resilience Note

The SDK automatically handles network errors by buffering failed payloads in `PlayerPrefs` (FIFO queue, max 300 logs) and retrying every 30s or upon application resume. Agents do not need to write custom retry or offline caching code.
