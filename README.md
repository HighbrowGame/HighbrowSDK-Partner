# Highbrow SDK (Unity Client)

`HighbrowSDK`는 퍼블리싱 파트너사를 위한 공식 Unity 클라이언트 SDK입니다.
안정적인 실시간 로그 수집(Snowflake 연동)을 시작으로 향후 크로스 프로모션 광고(`Highbrow.Ad`), 게임센터 보상 시스템(`Highbrow.GameCenter`)까지 유연하게 확장 가능한 모듈형 아키텍처로 설계되었습니다.

---

## 📑 목차
1. [🌟 AI 원클릭 연동 가이드 (Master Prompt)](#-ai-원클릭-연동-가이드-master-prompt)
2. [특징 및 아키텍처 원칙](#-특징-및-아키텍처-원칙)
3. [설치 가이드 (Unity Package Manager)](#-설치-가이드-unity-package-manager)
4. [SDK 수동 초기화 및 API 명세](#-sdk-수동-초기화-및-api-명세)
5. [개별 AI 프롬프트 템플릿 (선택 사항)](#-개별-ai-프롬프트-템플릿-선택-사항)
6. [확장 모듈 로드맵 (`Ad`, `GameCenter`)](#-확장-모듈-로드맵)

---

## 🌟 AI 원클릭 연동 가이드 (Master Prompt)

파트너사 개발자가 매 작업마다 개별 프롬프트를 입력할 필요 없이, **단 하나의 마스터 프롬프트**로 전체 연동을 대화형으로 완수할 수 있습니다.

> **사용 방법:**
> 1. Cursor Composer / GitHub Copilot Chat / Claude에 아래 프롬프트를 전체 복사해 붙여넣습니다.
> 2. AI가 프로젝트 상황에 대한 **사전 질문(신규 유저/첫 결제 판별 가능 여부, IAP/광고 플러그인 종류 등)**을 제시합니다.
> 3. 답변을 입력하면 AI가 **맞춤형 연동 계획**을 제시하고 승인을 요청합니다.
> 4. 승인 후 AI가 프로젝트 코드를 안전하게 순차 수정합니다.

```markdown
# Role & Objective
당신은 10년 차 이상의 Unity/C# 클라이언트 아키텍트이자 데이터 엔지니어입니다.
현재 열려 있는 Unity 프로젝트에 `HighbrowSDK` (로그 수집 및 코어 모듈)를 연동하는 작업을 수행합니다.

# Workflow & Execution Rules (엄격 준수)
1. **[중요] 즉시 코드를 수정하지 마세요.**
2. **Phase 1 (사전 질문):** 먼저 아래 [사전 질문 체크리스트]를 개발자에게 질문하고 응답을 기다리세요.
3. **Phase 2 (계획 승인):** 개발자의 답변을 바탕으로 수정할 파일 목록과 구체적인 연동 계획(Integration Plan)을 정리하여 개발자에게 제시하고, **개발자의 명시적 승인("진행", "Proceed" 등)**을 받으세요.
4. **Phase 3 (순차 실행):** 승인을 받은 후, Step 1(초기화)부터 순차적으로 최소 변경(Surgical Changes) 원칙을 지키며 기존 코드를 수정하세요.
5. **Phase 4 (검증):** 연동 완료 후 컴파일 체크 및 테스트 확인 지침을 제공하세요.

---

## Phase 1: 사전 질문 체크리스트 (개발자에게 이 질문을 출력하세요)

프로젝트 환경과 여건에 맞게 안전하게 연동하기 위해 다음 질문에 답변해 주세요:

### 1. SDK 기본 정보
- 하이브로 발급 **AppKey**: (예: `YOUR_APP_KEY`)
- 타겟 환경: `DEV` / `QA` / `PROD` 중 선택 (기본: `DEV`)
- 서버 리전: `kr` / `us` / `dev` / `qa` 등 (기본: `kr`)

### 2. 유저 식별자 (SUID) 및 로그인
- 게임 내 유저 고유 ID(SUID) 변수명 또는 획득 경로: (예: `userSeq`, `FirebaseUser.UserId`, `uuid` 등)
- 지원하는 소셜/인증 수단: (예: Google, Apple, Guest, Facebook 등)

### 3. 신규 유저(New User) 판별 가능 여부
- [ ] **(A) 판별 가능:** 게임 서버 또는 로컬에서 신규 계정/캐릭터 생성 여부를 명확히 알 수 있음 -> `TrackNewUser` 연동
- [ ] **(B) 판별 불가:** 클라이언트 단에서 신규 여부를 구분하기 어려움 -> `TrackNewUser` 호출 생략 (`TrackAuth`만 연동)
- [ ] **(C) 로컬 플래그 대체:** PlayerPrefs를 이용해 로컬 첫 실행 여부로 신규 유저 판단 희망

### 4. 첫 결제(First Purchase) 판별 가능 여부
- [ ] **(A) 판별 가능:** 유저의 생애 첫 인앱 결제 여부를 변수/플래그로 알 수 있음 -> 첫 결제 시 `isFirstPurchase: true` 전달
- [ ] **(B) 판별 불가:** 첫 결제 여부를 알 수 없음 -> 기본값 `isFirstPurchase: false`로 설정 (`TrackPurchase` 영수증 로그만 발송)

### 5. 인앱 결제 (IAP) 연동 대상
- 사용하는 IAP 플러그인: (예: Unity IAP `IStoreListener`, 커스텀 네이티브 IAP, 기타 에셋)
- 결제 완료 콜백이 위치한 클래스/파일: (예: `IAPManager.cs`, `ShopManager.cs`)

### 6. 광고 (Ad) 연동 대상 (광고가 있는 경우)
- 사용하는 광고 네트워크/미디에이션: (예: AppLovin MAX, IronSource, Google AdMob, Unity Ads, 없음)
- 광고 시청 스킵 유료 패키지(No Ads 등) 기능 존재 여부: (예: 있음 / 없음)
- 광고 콜백이 위치한 클래스/파일: (예: `AdManager.cs`)

### 7. 세션 하트비트 추적 방식
- [ ] **(A) SDK 자동 추적 (권장):** `AutoSessionTracking = true`로 설정하여 백그라운드 5분 주기 자동 전송
- [ ] **(B) 수동 제어:** 특정 씬(로비 등) 진입 시 `HighbrowLog.StartSessionTracking()` / 로그아웃 시 `StopSessionTracking()` 직접 호출

---

## Phase 2 ~ Phase 4 실행 가이드라인 (AI 내부 지침)

- **사전 질문 응답을 받기 전까지는 절대 프로젝트 파일을 수정하지 마세요.**
- 개발자가 질문에 답변하면, 답변을 분석하여:
  1. SDK 초기화 코드 (`HighbrowSDK.Initialize(...)`) 구성
  2. 수정 대상 파일 및 삽입 위치 목록
  3. `TrackAuth`, `TrackNewUser`(조건부), `TrackPurchase`, `TrackAd`(조건부), `SessionTracking` 적용 계획
  을 작성해 보여주고, **"이 계획대로 연동을 진행할까요? (Yes / 수정 요청)"**을 물어보세요.
- 승인을 받은 후에만 `using Highbrow.Core;`, `using Highbrow.Log;`를 추가하고 코드를 안전하게 삽입하세요.
```

---

## 🏛 특징 및 아키텍처 원칙

1. **Namespace 및 모듈 분리:**
   - 코어 및 진입점: `Highbrow.Core`
   - 로그 수집 모듈: `Highbrow.Log`
   - 광고 모듈 (확장 예정): `Highbrow.Ad`
   - 게임센터 모듈 (확장 예정): `Highbrow.GameCenter`
2. **완벽한 디커플링 (Zero Coupling):**
   - 각 서브 모듈은 독립된 Assembly Definition(`.asmdef`)으로 컴파일되며 상호 참조하지 않습니다. 로그 기능만 필요할 경우 광고/게임센터 모듈에 일절 영향을 받지 않습니다.
3. **단일 진입점 (`HighbrowSDK.Initialize`):**
   - `HighbrowSDK.Initialize(config)` 한 번으로 활성화된 모듈들이 일괄 초기화됩니다.
4. **오프라인 큐 & 자동 재시도 (PlayerPrefs Retry Queue):**
   - 네트워크 단절 또는 HTTP 오류 시 로그를 로컬 저장소(`PlayerPrefs`)에 FIFO 큐로 캐싱하며, 네트워크 정상화 시 백그라운드에서 자동 재전송합니다.
5. **공통 메타데이터 자동 주입 (Auto-Injection):**
   - UTC 시각(`Time`), 기기 고유 ID(`Duid`), OS 종류(`Os`), 스토어 마켓(`Market`), 국가 코드(`Country`), 클라이언트 버전(`ClientVersion`), 기기 스펙(`DeviceInfo`)을 SDK 내부에서 자동 수집 및 주입합니다.

---

## 📦 설치 가이드 (Unity Package Manager)

### 방법 1. Git URL로 설치 (권장)
Unity Editor 상단 메뉴: **Window** > **Package Manager** > **`+` 버튼** > **Add package from git URL...**
```text
https://github.com/highbrow-inc/HighbrowSDK-Partner.git?path=Packages/com.highbrow.sdk
```

### 방법 2. `manifest.json`에 직접 추가
프로젝트의 `Packages/manifest.json` 파일의 `dependencies` 블록에 아래 내용을 추가합니다:
```json
{
  "dependencies": {
    "com.highbrow.sdk": "https://github.com/highbrow-inc/HighbrowSDK-Partner.git",
    ...
  }
}
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
            ServerMode = "DEV",                          // DEV, QA, PROD 중 선택
            Region = "kr",                              // 서버 지역 코드 (kr, us, dev, qa 등)
            LogEndpointUrl = "https://log-api.highbrow-inc.com/v1/collect",
            
            EnableLog = true,                           // 로그 모듈 활성화
            AutoSessionTracking = true,                 // 5분 주기 세션 하트비트 자동 활성화
            SessionIntervalSeconds = 300f,              // 세션 로그 주기 (기본 300초 = 5분)
            MaxOfflineQueueSize = 300,                  // 오프라인 캐시 최대 개수
            FlushRetryIntervalSeconds = 30f,            // 오프라인 큐 재전송 주기 (초)
            DebugMode = true                            // 개발 단계에서 디버그 로그 활성화
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

개별 파일 단위로 직접 AI에게 지시하고 싶을 때 사용하는 템플릿입니다.

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

---

## 🔮 확장 모듈 로드맵

- **`Highbrow.Ad` (`HighbrowAdPlayer`)**: 하이브로 자사 게임 간 크로스 프로모션 배너 및 전면/보상형 광고 송출.
- **`Highbrow.GameCenter` (`HighbrowGameCenter`)**: 하이브로 게임 다운로드 미션 및 크로스 리워드(게임 간 재화 지급) 연동.

---

## 📞 기술 지원 및 문의
- **엔지니어링 팀:** dev@highbrow-inc.com
- **공식 웹사이트:** https://highbrow-inc.com
