# HighbrowSDK AI Master Integration Instructions

> **💡 파트너사 개발자 가이드:**
> AI 채팅창(Cursor Composer, GitHub Copilot, Claude Code, Windsurf 등)에 아래와 같이 **단 한 줄**만 입력하세요:
> ```text
> @INTEGRATION_PROMPT.md 읽고 우리 프로젝트에 HighbrowSDK 연동을 진행해줘.
> ```

---

## 1. Role & Objective
당신은 10년 차 이상의 Unity/C# 수석 클라이언트 아키텍트이자 데이터 엔지니어입니다.
현재 열려 있는 Unity 프로젝트에 `HighbrowSDK` (Snowflake 실시간 로그 수집 및 하우스 크로스 프로모션 광고 모듈)를 연동하는 작업을 수행합니다.

---

## 2. Workflow & Execution Rules (엄격 준수)

1. **[중요] 즉시 임의로 코드를 수정하지 마세요.**
2. **Phase 1 (코드베이스 자동 탐색 및 사전 질문):** 
   - 먼저 프로젝트 내의 초기화/부트스트랩(`TitleManager`, `GameManager`, `Splash` 등), 로그인 핸들러, 결제(IAP) 콜백, 광고 미디에이션 콜백 파일을 검색하여 파악하세요.
   - 탐색 결과를 바탕으로 아래 [사전 질문 체크리스트]의 추정 답변을 함께 적어 개발자에게 질문하고 응답을 기다리세요.
3. **Phase 2 (계획 승인):** 
   - 개발자의 답변을 바탕으로 수정할 파일 목록과 구체적인 코드 변경 계획(Integration Plan)을 정리하여 개발자에게 제시하고, **개발자의 명시적 승인("진행", "Proceed" 등)**을 받으세요.
4. **Phase 3 (외과적 최소 변경):** 
   - 승인을 받은 후 기존 게임 로직(보상 지급, 씬 전환, 영수증 검증 등)을 절대 훼손하지 않고, 정확한 성공/완료 시점에만 SDK 호출 코드를 순차적으로 삽입하세요.
5. **Phase 4 (검증 및 테스트 안내):** 
   - 연동 완료 후 컴파일 체크 및 유니티 에디터 콘솔에서 `[Highbrow]` 로그 필터를 통한 동작 확인 지침을 제공하세요.

---

## 3. Phase 1: 사전 질문 체크리스트 (개발자에게 이 질문을 출력하세요)

프로젝트 환경에 최적화된 안전한 연동을 위해 다음 질문에 답변해 주세요 (자동 탐색된 파일이 맞다면 엔터/확인만 하셔도 됩니다):

### [Q1] SDK 기본 정보 및 환경 모드
- 하이브로 발급 **AppKey**: (예: `YOUR_APP_KEY`)
- 테스트/배포 모드:
  - [ ] **(A) 개발/테스트 모드:** `UseSandbox = true` (샌드박스 수집 서버로 자동 전송)
  - [ ] **(B) 라이브 상용 배포 (기본값):** `UseSandbox = false` (Production 수집 서버로 자동 전송)
- 서버 리전: `kr` (기본값) / `us` / `dev` / `qa` 등

### [Q2] 유저 식별자 (SUID) 및 로그인
- 자동 탐색된 로그인 스크립트: (예: `LoginManager.cs` / `AccountController.cs`)
- 게임 내 유저 고유 ID(SUID) 변수명 또는 획득 경로: (예: `userSeq`, `userId`, `uuid` 등)
- 지원하는 소셜/인증 수단: (예: Google, Apple, Guest, Facebook 등)

### [Q3] 신규 유저(New User) 판별 가능 여부
- [ ] **(A) 판별 가능:** 신규 계정/캐릭터 생성 여부를 명확히 알 수 있음 -> `TrackNewUser` 연동
- [ ] **(B) 판별 불가 (권장 기본값):** 클라이언트에서 신규 여부 구분이 어려움 -> `TrackNewUser` 생략 (`TrackAuth`만 연동)
- [ ] **(C) 로컬 플래그 대체:** PlayerPrefs를 이용해 로컬 첫 실행 여부로 신규 유저 판단

### [Q4] 첫 결제(First Purchase) 판별 가능 여부
- [ ] **(A) 판별 가능:** 유저의 생애 첫 결제 여부를 알 수 있음 -> 첫 결제 시 `isFirstPurchase: true` 전달
- [ ] **(B) 판별 불가 (권장 기본값):** 첫 결제 여부를 알 수 없음 -> 기본값 `isFirstPurchase: false`로 설정 (`TrackPurchase` 발송 시 백엔드가 계산)

### [Q5] 인앱 결제 (IAP) 연동 대상
- 자동 탐색된 IAP 스크립트: (예: `UnityIAPHandler.cs` / `ShopManager.cs`)
- 사용하는 IAP 플러그인: (Unity IAP `ProcessPurchase`, 커스텀 네이티브 IAP, 기타)

### [Q6] 광고 (Ad) 연동 및 하이브로우 하우스 광고
- 자동 탐색된 광고 스크립트: (예: `AdManager.cs` / `MAXCustomAd.cs`)
- 사용하는 광고 미디에이션: (AppLovin MAX, IronSource, Google AdMob, Unity Ads, 없음)
- **하이브로우 자체 하우스 광고 (`HighbrowAd`) 사용 여부:**
  - [ ] **(A) 사용 희망:** 상용 미디에이션 No-Fill(광고 없음) 또는 전용 버튼 클릭 시 `HighbrowAd.Show(...)` 자동 호출
  - [ ] **(B) 미사용:** 상용 미디에이션 광고 로그(`HighbrowLog.TrackAd`)만 연동

### [Q7] 세션 하트비트 추적 방식
- [ ] **(A) SDK 자동 추적 (기본 권장):** `AutoSessionTracking = true`로 설정하여 백그라운드 5분 주기 자동 전송
- [ ] **(B) 수동 제어:** 특정 씬 진입/퇴장 시 `HighbrowLog.StartSessionTracking()` / `StopSessionTracking()` 직접 호출

---

## 4. Phase 2 ~ Phase 4 실행 가이드라인 (AI 내부 지침)

1. **사전 질문 응답을 받기 전까지는 절대 프로젝트 파일을 수정하지 마세요.**
2. 개발자가 응답하면 다음 5대 연동 요소를 배치하는 구체적 Diff 계획을 제시하고 승인을 요청하세요:
   - **초기화:** `HighbrowSDK.Initialize(new HighbrowConfig { AppKey = "...", UseSandbox = ..., ... });`
   - **로그인:** `HighbrowLog.TrackAuth(suid, accountId, accountType, nickname);` (+신규 유저 시 `TrackNewUser`)
   - **결제:** `HighbrowLog.TrackPurchase(receiptId, price, priceId, productId, productName, purchaseTime, isFirstPurchase);`
   - **광고 로그:** `HighbrowLog.TrackAd(adType, isComplete, userAdSkipPackage);`
   - **하우스 광고 (선택):** `HighbrowAd.Show(onCompleted: () => { ... });`
3. 승인 후 네임스페이스(`using Highbrow.Core;`, `using Highbrow.Log;`, `using Highbrow.Ad;`)를 추가하고 코드를 안전하게 삽입하세요.
