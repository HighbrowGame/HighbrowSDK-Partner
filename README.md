# Highbrow SDK (Unity Client)

`HighbrowSDK`는 퍼블리싱 파트너사를 위한 공식 Unity 클라이언트 SDK입니다.
안정적인 실시간 로그 수집(Snowflake 연동)과 하이브로우 자체 인하우스 크로스 프로모션 광고 플레이어(`Highbrow.Ad`)를 제공하는 가볍고 안전한 모듈형 아키텍처로 설계되었습니다.

---

## 📑 목차
1. [🌟 AI 원클릭 연동 가이드 (Master Prompt)](#-ai-원클릭-연동-가이드-master-prompt)
2. [특징 및 아키텍처 원칙](#-특징-및-아키텍처-원칙)
3. [설치 가이드 (Unity Package Manager)](#-설치-가이드-unity-package-manager)
4. [SDK 수동 초기화 및 API 명세](#-sdk-수동-초기화-및-api-명세)
5. [하이브로우 자체 하우스 광고 연동 (`Highbrow.Ad`)](#-하이브로우-자체-하우스-광고-연동-highbrowad)
6. [개별 AI 프롬프트 템플릿 (선택 사항)](#-개별-ai-프롬프트-템플릿-선택-사항)

---

## 🌟 AI 원클릭 연동 가이드 (Master Prompt)

파트너사 개발자가 매 작업마다 개별 코드를 작성하거나 긴 프롬프트를 복사할 필요 없이, **파일 하나만 멘션(`@INTEGRATION_PROMPT.md`)하여 단 한 줄**로 안전한 대화형 연동을 완료할 수 있습니다.

> ### 💡 AI 툴별 1줄 요청 명령어
> - **Cursor / Windsurf:**
>   ```text
>   @INTEGRATION_PROMPT.md 읽고 우리 프로젝트에 HighbrowSDK 연동을 진행해줘.
>   ```
> - **GitHub Copilot Chat:**
>   ```text
>   #file:INTEGRATION_PROMPT.md 읽고 우리 프로젝트에 HighbrowSDK 연동을 진행해줘.
>   ```
> - **Claude Code (CLI):**
>   ```text
>   INTEGRATION_PROMPT.md를 읽고 우리 프로젝트에 HighbrowSDK 연동을 진행해줘.
>   ```
>
> *(※ 또는 아래 마크다운 블록을 전체 복사하여 AI 채팅창에 직접 붙여넣으셔도 됩니다.)*

```markdown
# Role & Objective
당신은 10년 차 이상의 Unity/C# 수석 클라이언트 아키텍트이자 데이터 엔지니어입니다.
현재 열려 있는 Unity 프로젝트에 `HighbrowSDK` (Snowflake 로그 수집 및 하우스 크로스 프로모션 광고 모듈)를 연동하는 작업을 수행합니다.

# Workflow & Execution Rules (엄격 준수)
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

## Phase 1: 사전 질문 체크리스트 (개발자에게 이 질문을 출력하세요)

프로젝트 환경에 최적화된 안전한 연동을 위해 다음 질문에 답변해 주세요 (자동 탐색된 파일이 맞다면 엔터/확인만 하셔도 됩니다):

### 1. SDK 기본 정보 및 환경 모드
- 하이브로 발급 **AppKey**: (예: `YOUR_APP_KEY`)
- 테스트/배포 모드:
  - [ ] **(A) 개발/테스트 모드:** `UseSandbox = true` (샌드박스 수집 서버로 자동 전송)
  - [ ] **(B) 라이브 상용 배포 (기본값):** `UseSandbox = false` (Production 수집 서버로 자동 전송)
- 서버 리전: `kr` (기본값) / `us` / `dev` / `qa` 등

### 2. 유저 식별자 (SUID) 및 로그인
- 자동 탐색된 로그인 스크립트: (예: `LoginManager.cs` / `AccountController.cs`)
- 게임 내 유저 고유 ID(SUID) 변수명 또는 획득 경로: (예: `userSeq`, `userId`, `uuid` 등)
- 지원하는 소셜/인증 수단: (예: Google, Apple, Guest, Facebook 등)

### 3. 신규 유저(New User) 판별 가능 여부
- [ ] **(A) 판별 가능:** 신규 계정/캐릭터 생성 여부를 명확히 알 수 있음 -> `TrackNewUser` 연동
- [ ] **(B) 판별 불가 (권장 기본값):** 클라이언트에서 신규 여부 구분이 어려움 -> `TrackNewUser` 생략 (`TrackAuth`만 연동)
- [ ] **(C) 로컬 플래그 대체:** PlayerPrefs를 이용해 로컬 첫 실행 여부로 신규 유저 판단

### 4. 첫 결제(First Purchase) 판별 가능 여부
- [ ] **(A) 판별 가능:** 유저의 생애 첫 결제 여부를 알 수 있음 -> 첫 결제 시 `isFirstPurchase: true` 전달
- [ ] **(B) 판별 불가 (권장 기본값):** 첫 결제 여부를 알 수 없음 -> 기본값 `isFirstPurchase: false`로 설정 (`TrackPurchase` 발송 시 백엔드가 계산)

### 5. 인앱 결제 (IAP) 연동 대상
- 자동 탐색된 IAP 스크립트: (예: `UnityIAPHandler.cs` / `ShopManager.cs`)
- 사용하는 IAP 플러그인: (Unity IAP `ProcessPurchase`, 커스텀 네이티브 IAP, 기타)

### 6. 광고 (Ad) 연동 및 하이브로우 하우스 광고
- 자동 탐색된 광고 스크립트: (예: `AdManager.cs` / `MAXCustomAd.cs`)
- 사용하는 광고 미디에이션: (AppLovin MAX, IronSource, Google AdMob, Unity Ads, 없음)
- **하이브로우 자체 하우스 광고 (`HighbrowAd`) 사용 여부:**
  - [ ] **(A) 사용 희망:** 상용 미디에이션 No-Fill(광고 없음) 또는 전용 버튼 클릭 시 `HighbrowAd.Show(...)` 자동 호출
  - [ ] **(B) 미사용:** 상용 미디에이션 광고 로그(`HighbrowLog.TrackAd`)만 연동

### 7. 세션 하트비트 추적 방식
- [ ] **(A) SDK 자동 추적 (기본 권장):** `AutoSessionTracking = true`로 설정하여 백그라운드 5분 주기 자동 전송
- [ ] **(B) 수동 제어:** 특정 씬 진입/퇴장 시 `HighbrowLog.StartSessionTracking()` / `StopSessionTracking()` 직접 호출

---

## Phase 2 ~ Phase 4 실행 가이드라인 (AI 내부 지침)

1. **사전 질문 응답을 받기 전까지는 절대 프로젝트 파일을 수정하지 마세요.**
2. 개발자가 응답하면 다음 5대 연동 요소를 배치하는 구체적 Diff 계획을 제시하고 승인을 요청하세요:
   - **초기화:** `HighbrowSDK.Initialize(new HighbrowConfig { AppKey = "...", UseSandbox = ..., ... });`
   - **로그인:** `HighbrowLog.TrackAuth(suid, accountId, accountType, nickname);` (+신규 유저 시 `TrackNewUser`)
   - **결제:** `HighbrowLog.TrackPurchase(receiptId, price, priceId, productId, productName, purchaseTime, isFirstPurchase);`
   - **광고 로그:** `HighbrowLog.TrackAd(adType, isComplete, userAdSkipPackage);`
   - **하우스 광고 (선택):** `HighbrowAd.Show(onCompleted: () => { ... });`
3. 승인 후 네임스페이스(`using Highbrow.Core;`, `using Highbrow.Log;`, `using Highbrow.Ad;`)를 추가하고 코드를 안전하게 삽입하세요.
```

---

## 🏛 특징 및 아키텍처 원칙

1. **2단계 자동 엔드포인트 라우팅 (Sandbox vs Production):**
   - `UseSandbox = true` 설정 시 샌드박스 주소(`https://sandbox-log-api.highbrow-inc.com/v1/collect`)로 자동 전송.
   - `UseSandbox = false` (기본값) 설정 시 상용 라이브 주소(`https://log-api.highbrow-inc.com/v1/collect`)로 자동 전송.
   - 복잡한 URL 입력 없이 불리언 플래그 하나로 완벽하게 스위칭됩니다.
2. **Namespace 및 모듈 분리:**
   - 코어 및 진입점: `Highbrow.Core`
   - 로그 수집 모듈: `Highbrow.Log`
   - 자체 크로스 프로모션 광고 모듈: `Highbrow.Ad` (UI 프리팹 & 재생기 내장)
3. **완벽한 디커플링 (Zero Coupling):**
   - 각 서브 모듈은 독립된 Assembly Definition(`.asmdef`)으로 컴파일되며 상호 참조하지 않습니다.
4. **단일 진입점 (`HighbrowSDK.Initialize`):**
   - `HighbrowSDK.Initialize(config)` 한 번으로 활성화된 모듈들이 일괄 초기화됩니다.
5. **오프라인 큐 & 자동 재시도 (PlayerPrefs Retry Queue):**
   - 네트워크 단절 또는 HTTP 오류 시 로그를 로컬 저장소(`PlayerPrefs`)에 FIFO 큐로 캐싱하며, 네트워크 정상화 시 백그라운드에서 자동 재전송합니다.
6. **공통 메타데이터 자동 주입 (Auto-Injection):**
   - UTC 시각(`Time`), 기기 고유 ID(`Duid`), OS 종류(`Os`), 스토어 마켓(`Market`), 국가 코드(`Country`), 클라이언트 버전(`ClientVersion`), 기기 스펙(`DeviceInfo`)을 SDK 내부에서 자동 수집 및 주입합니다.

---

## 📦 설치 가이드 (Unity Package Manager)

### 방법 1. Git URL로 설치 (권장)
Unity Editor 상단 메뉴: **Window** > **Package Manager** > **`+` 버튼** > **Add package from git URL...**
```text
https://github.com/HighbrowGame/HighbrowSDK-Partner.git
```

---

## 🚀 SDK 수동 초기화 및 API 명세

### 1. SDK 초기화 예제

```csharp
using Highbrow.Core;
using Highbrow.Log;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        HighbrowConfig config = new HighbrowConfig
        {
            AppKey = "YOUR_ISSUED_HIGHBROW_APP_KEY",   // 하이브로 발급 앱 키
            UseSandbox = false,                         // true: 샌드박스 테스트, false: 상용 라이브 배포
            Region = "kr",                              // 서버 지역 코드 (kr, us, dev, qa 등)
            
            EnableLog = true,                           // 로그 모듈 활성화
            AutoSessionTracking = true,                 // 5분 주기 세션 하트비트 자동 활성화
            SessionIntervalSeconds = 300f,              // 세션 로그 주기 (기본 300초 = 5분)
            MaxOfflineQueueSize = 300,                  // 오프라인 캐시 최대 개수
            FlushRetryIntervalSeconds = 30f,            // 오프라인 큐 재전송 주기 (초)
            DebugMode = false                           // 상용 배포 시 false 지정
        };

        HighbrowSDK.Initialize(config);
    }
}
```

### 2. 주요 로그 API 요약

| 로그 종류 | API 메서드 | 파트너사 미지원 시 처리 |
| :--- | :--- | :--- |
| **인증 로그** | `HighbrowLog.TrackAuth(...)` | **필수 연동** (로그인 시점) |
| **신규 유저 로그** | `HighbrowLog.TrackNewUser(...)` | 판별 불가능 시 **생략 가능** |
| **세션 하트비트** | `HighbrowLog.StartSessionTracking()` | `AutoSessionTracking = true` 시 자동 발송 |
| **결제 영수증 로그** | `HighbrowLog.TrackPurchase(...)` | **필수 연동** (IAP 성공 시점) |
| **첫 결제 로그** | `HighbrowLog.TrackFirstPurchase(...)` | 판별 불가능 시 `isFirstPurchase: false` 설정 |
| **광고 시청 로그** | `HighbrowLog.TrackAd(...)` | 게임 내 광고 존재 시 연동 |

---

## 🛠 개별 AI 프롬프트 템플릿 (선택 사항)

<details>
<summary><b>1. 로그인/인증 연동 템플릿 (Click to expand)</b></summary>

```markdown
# Instruction
현재 열려 있는 로그인 관리 스크립트에 `HighbrowSDK`의 인증 로그(`TrackAuth`) 연동 코드를 추가해줘.
1. 상단에 `using Highbrow.Log;` 추가.
2. 유저 로그인 성공 콜백에서 `HighbrowLog.TrackAuth(suid, accountId, accountType, nickname, "OK");` 호출.
3. 신규 유저 판별이 가능한 경우 `HighbrowLog.TrackNewUser();`도 함께 호출. (불가능하면 생략)
```
</details>

<details>
<summary><b>2. IAP 결제 영수증 연동 템플릿 (Click to expand)</b></summary>

```markdown
# Instruction
현재 열려 있는 IAP 관리 스크립트에 `HighbrowSDK` 결제 영수증 로그(`TrackPurchase`)를 연동해줘.
1. 상단에 `using Highbrow.Log;` 추가.
2. 결제 완료 시점에 `HighbrowLog.TrackPurchase(receiptId, price, priceId, productId, productName, isFirstPurchase: isFirst);` 호출.
3. 첫 결제 여부를 모르는 경우 `isFirstPurchase: false`로 전달.
```
</details>

<details>
<summary><b>3. 광고 시청 연동 템플릿 (Click to expand)</b></summary>

```markdown
# Instruction
현재 열려 있는 광고 관리 스크립트에 `HighbrowSDK` 광고 시청 로그(`TrackAd`)를 연동해줘.
1. 상단에 `using Highbrow.Log;` 추가.
2. 광고 시작 시: `HighbrowLog.TrackAd(adType, isComplete: false);`
3. 광고 시청 완료/보상 지급 시: `HighbrowLog.TrackAd(adType, isComplete: true, userAdSkipPackage: hasSkipPass);`
```
</details>

<details>
<summary><b>4. 하이브로우 자체 하우스 광고 (No-Fill 대체) 연동 템플릿 (Click to expand)</b></summary>

```markdown
# Instruction
현재 열려 있는 광고 관리 스크립트에서 상용 광고 미디에이션(MAX/IronSource/AdMob 등) 로딩 실패/No-Fill 콜백 발생 시, 또는 하우스 광고 전용 버튼 클릭 시 `HighbrowSDK`의 자체 크로스 프로모션 광고(`HighbrowAd`)가 실행되도록 연동해줘.
1. 상단에 `using Highbrow.Ad;` 추가.
2. No-Fill 실패 콜백 또는 하우스 광고 재생 시점에 다음 코드 호출:
   HighbrowAd.Show(
       onCompleted: () => { /* 기존 리워드 지급 또는 게임 재개 콜백 호출 */ },
       onFailed: () => { /* 실패 처리 또는 안내 팝업 */ }
   );
```
</details>

---

## 📺 하이브로우 자체 하우스 광고 연동 (`Highbrow.Ad`)

상용 광고 미디에이션(MAX/IronSource/AdMob 등)에서 No-Fill/로딩 실패가 발생하거나, 하이브로우 자사 게임 간 크로스 프로모션 광고를 직접 송출하고자 할 때 `HighbrowAd`를 사용합니다.

- **Resources 자동 로딩:** Addressables 패키지 없이 `Resources.Load`로 UI 프리팹(`UI_HighbrowAd`)을 자동 생성합니다.
- **자동 로그 연동:** 광고 재생 시작 및 완료/스킵 시 `HighbrowLog.TrackAd(AdType.CrossPromotion)` 로그가 내부에서 자동 수집됩니다.

### 사용 예제 (단 한 줄 호출)

```csharp
using Highbrow.Ad;
using UnityEngine;

public class AdRewardHandler : MonoBehaviour
{
    public void ShowHouseAd()
    {
        // 하이브로우 크로스 프로모션 광고 팝업 호출
        HighbrowAd.Show(
            onCompleted: () =>
            {
                Debug.Log("광고 시청 완료 (또는 스킵 종료) -> 보상 지급 또는 게임 재개");
                GrantUserReward();
            },
            onFailed: () =>
            {
                Debug.LogWarning("광고 팝업 생성 실패");
            }
        );
    }

    private void GrantUserReward()
    {
        // 유저 보상 로직
    }
}
```

---

## 📞 기술 지원 및 문의
- **엔지니어링 팀:** dev@highbrow-inc.com
- **공식 웹사이트:** https://highbrow-inc.com
