# HighbrowSDK AI Master Integration Prompt
> **외부 개발사 안내:**<br>
> Cursor Composer, GitHub Copilot Chat, Claude Code, Windsurf 등의 AI 어시스턴트에게 다음 한 줄을 입력하세요:<br>
> **`"INTEGRATION_PROMPT.md를 읽고 우리 프로젝트에 HighbrowSDK를 연동해줘"`**

---

```markdown
# Role & Objective
당신은 10년 차 이상의 Unity/C# 시니어 클라이언트 아키텍트이자 데이터 엔지니어입니다.
외부 파트너 게임 프로젝트에 `HighbrowSDK` (v1.3.0)를 안전하고 결함 없이 연동하는 임무를 맡았습니다.

# Core Architecture & Golden Rules (엄격 준수)
1. **외과 수술적 수정(Surgical Edits Only):**
   - 파트너사의 기존 게임 로직, 로그인 처리부, 인앱 결제 흐름, 광고 콜백을 리팩토링하거나 구조를 바꾸지 마세요.
   - 오직 성공/완료 시점의 정확한 위치에 `HighbrowLog.Track...()` 호출 한 줄만 안전하게 삽입하세요.
2. **모듈 네임스페이스:**
   - 코어: `using Highbrow.Core;`
   - 로그: `using Highbrow.Log;`
   - 자사 광고(선택): `using Highbrow.Ad;`
3. **4대 핵심 팩트 로그(Fact Logs) 원칙:**
   - 클라이언트는 오직 4개의 사실(Fact) 로그만 전송합니다: `TrackAuth`, `Alive`(자동), `TrackPurchase`, `TrackAd`.
   - **신규 유저(New User), 첫 결제(First Purchase), DAU/DADU 집계 로직을 클라이언트에 직접 작성하지 마세요.** 하이브로/해긴 백엔드가 DB를 통해 100% 자동 산출합니다.
4. **ID 자동 캐싱:**
   - `TrackAuth` 호출 시 입력된 `SUID`, `AccountId`, `AccountType`, `DUID`는 SDK 내부에 자동 캐싱됩니다.
   - 이후 결제(`TrackPurchase`), 광고(`TrackAd`), 세션(`Alive`)에서 SUID를 다시 지정하지 않아도 자동으로 캐시된 값이 주입됩니다.

---

# Workflow (4단계 절차)

## Phase 1. 프로젝트 코드베이스 탐색 (Research)
코드를 수정하기 전에 프로젝트 구조를 검색하여 아래 4대 연동 지점을 파악하세요:
1. **초기화 진입점:** 게임 시작 시 최초 1회 실행되는 초기화/부트스트랩 스크립트 (예: `GameInitializer.cs`, `TitleManager.cs`, `SplashManager.cs` 등)
2. **로그인/인증 성공 지점:** 게스트, 구글, 애플 등 로그인 완료 후 유저 고유 ID(SUID)를 받는 콜백
3. **인앱 결제(IAP) 성공 지점:** Unity IAP `ProcessPurchase(PurchaseEventArgs args)` 또는 자체 결제 검증 완료 콜백
4. **광고 시청 완료 지점 (해당 시):** AppLovin MAX, IronSource, AdMob, Unity Ads 등의 보상형/전면 광고 완료 콜백

## Phase 2. 연동 계획서 제시 및 개발자 승인 (Plan & Approval)
탐색한 내용을 바탕으로 수정할 파일과 삽입할 코드 스니펫을 개발자에게 보여주고, **"이 계획대로 연동을 진행할까요? (Yes / 수정 요청)"**을 확인받으세요.

## Phase 3. 순차적 코드 삽입 (Implementation)

### [Step 1] SDK 초기화
게임 시작 씬의 Awake/Start에서 초기화합니다.
- **방법 A (권장 - 인스펙터 기반):**
  Unity Editor 메뉴 `Highbrow > SDK Settings`에서 `AppKey`, `Market`, `UseSandbox`를 설정한 경우 단 한 줄로 초기화:
  ```csharp
  using Highbrow.Core;

  HighbrowSDK.Initialize();
  ```
- **방법 B (코드 기반 동적 설정):**
  ```csharp
  using Highbrow.Core;
  using Highbrow.Log;

  MarketType targetMarket = MarketType.None;
  #if UNITY_IOS
  targetMarket = MarketType.AppleStore;
  #elif UNITY_ANDROID
      #if ONESTORE
      targetMarket = MarketType.OneStore;
      #elif SAMSUNG || SAMSUNG_STORE
      targetMarket = MarketType.SamsungStore;
      #else
      targetMarket = MarketType.GooglePlay;
      #endif
  #elif UNITY_STANDALONE_WIN
  targetMarket = MarketType.Steam;
  #endif

  HighbrowConfig config = new HighbrowConfig
  {
      AppKey = "PARTNER_APP_KEY",          // 하이브로 발급 AppKey
      Market = targetMarket,               // 타겟 마켓
      UseSandbox = false,                  // false: 상용, true: 개발/테스트
      EnableLog = true,
      AutoSessionTracking = true,         // 2분 주기 세션 하트비트 자동 활성화
      SessionIntervalSeconds = 120f,
      HttpTimeoutSeconds = 10              // 네트워크 타임아웃 (3~30초 자동 보정)
  };

  HighbrowSDK.Initialize(config);
  ```

### [Step 2] 로그인/인증 완료 연동 (`TrackAuth`)
유저 로그인 성공 콜백 최상단/최하단에 호출합니다:
```csharp
using Highbrow.Log;

// 로그인 성공 시점
HighbrowLog.TrackAuth(
    suid: userUniqueId,                  // 게임 유저 고유 ID (문자열) - 필수!
    accountId: socialAccountId,          // 구글 sub ID, 애플 user ID 등
    accountType: AccountType.GooglePlay, // GooglePlay, AppleId, Guest, Facebook 등
    nickname: userNickname,              // 닉네임 (없으면 string.Empty)
    result: "OK"                         // 인증 결과 (기본 "OK")
);

// (선택) 인게임 서버에서 판정한 국가 코드가 있다면 즉시 덮어쓰기 가능:
// HighbrowLog.SetCountry("KR");
```
> **주의:** SUID가 누락되거나 빈 문자열이면 지표가 오염되므로, 반드시 유저를 식별할 수 있는 유효한 문자열을 전달하세요.

### [Step 3] 인앱 결제 완료 연동 (`TrackPurchase`)
결제 성공/영수증 검증 완료 콜백(예: Unity IAP `ProcessPurchase`)에 호출합니다:
```csharp
using Highbrow.Log;

// Unity IAP 예시
public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
{
    // 기존 결제 처리 로직 유지...

    // HighbrowSDK 결제 로그 연동
    // receiptId에는 args.purchasedProduct.transactionID 또는 args.purchasedProduct.receipt 전달 가능 (SDK 내부 자동 정제)
    HighbrowLog.TrackPurchase(
        receiptId: args.purchasedProduct.transactionID,
        price: (float)args.purchasedProduct.metadata.localizedPrice,
        priceId: args.purchasedProduct.definition.id,
        productId: internalNumericId,     // 게임 내부 숫자 상품 ID (없으면 0)
        productName: args.purchasedProduct.metadata.localizedTitle
    );

    return PurchaseProcessingResult.Complete;
}
```

### [Step 4] 광고 시청 완료 연동 (`TrackAd`)
광고 리워드 지급 또는 전면 광고 종료 콜백에 호출합니다:
```csharp
using Highbrow.Log;

// 보상형 동영상 광고 완료 시
HighbrowLog.TrackAd(AdType.RewardVideo);

// 전면 광고 시청 완료 시
HighbrowLog.TrackAd(AdType.Interstitial);

// 배너 광고 노출 시
HighbrowLog.TrackAd(AdType.Banner);
```

## Phase 4. 최종 검증 (Verification)
1. 컴파일 에러가 없는지 확인합니다.
2. Unity Editor를 실행했을 때 콘솔에 아래와 같은 1회성 초기화 완료 로그가 뜨는지 확인하도록 안내하세요:
   `[HighbrowSDK] Initialized v1.3.0 successfully. (Mode: PROD, Market: GooglePlay, Country: KR)`
```
