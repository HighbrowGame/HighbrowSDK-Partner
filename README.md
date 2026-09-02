# Highbrow SDK (Unity Client)

`HighbrowSDK`는 퍼블리싱 파트너사를 위한 공식 Unity 클라이언트 SDK입니다.
안정적인 실시간 로그 수집(Snowflake 연동)을 시작으로 자사 게임 광고(`Highbrow.Ad`)까지 유연하게 확장 가능한 모듈형 아키텍처로 설계되었습니다.

---

## 📑 목차
1. [🌟 AI 원클릭 연동 가이드 (Master Prompt)](#-ai-원클릭-연동-가이드-master-prompt)
2. [특징 및 아키텍처 원칙](#-특징-및-아키텍처-원칙)
3. [설치 가이드 (Unity Package Manager)](#-설치-가이드-unity-package-manager)
4. [SDK 수동 초기화 및 4대 핵심 로그 API](#-sdk-수동-초기화-및-4대-핵심-로그-api)
5. [개별 AI 프롬프트 템플릿 (선택 사항)](#-개별-ai-프롬프트-템플릿-선택-사항)
6. [확장 모듈 (`Highbrow.Ad`)](#-확장-모듈-highbrowad)

---

## 🌟 AI 원클릭 연동 가이드 (Master Prompt)

파트너사 개발자가 매 작업마다 개별 프롬프트를 입력할 필요 없이, **단 하나의 마스터 프롬프트**로 전체 연동을 대화형으로 완수할 수 있습니다.

> **사용 방법:**
> 1. Cursor Composer / GitHub Copilot Chat / Claude에 아래 프롬프트를 전체 복사해 붙여넣습니다.
> 2. AI가 프로젝트 상황에 대한 **간단한 사전 질문(발급 AppKey, IAP/광고 파일 위치 등)**을 제시합니다.
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

### 1. SDK 기본 정보 및 환경 모드
- 하이브로 발급 **AppKey**: (예: `YOUR_APP_KEY`)
- 테스트/배포 모드:
  - [ ] **(A) 개발/테스트 모드:** `UseSandbox = true` (샌드박스 수집 서버로 자동 전송)
  - [ ] **(B) 라이브 상용 배포:** `UseSandbox = false` (Production 수집 서버로 자동 전송)

### 2. 유저 식별자 (SUID) 및 로그인
- 게임 내 유저 고유 ID(SUID) 변수명 또는 획득 경로: (예: `userSeq`, `FirebaseUser.UserId`, `uuid` 등)
- 지원하는 소셜/인증 수단: (예: Google, Apple, Guest, Facebook 등)
- 로그인 성공 콜백이 위치한 클래스/파일: (예: `LoginManager.cs`, `TitleScene.cs`)

### 3. 인앱 결제 (IAP) 연동 대상
- 사용하는 IAP 플러그인: (예: Unity IAP `IStoreListener`, 자체 결제, 기타 에셋)
- 결제 완료 콜백이 위치한 클래스/파일: (예: `IAPManager.cs`, `ShopManager.cs`)

### 4. 광고 (Ad) 연동 대상 (광고가 있는 경우)
- 사용하는 광고 네트워크/미디에이션: (예: AppLovin MAX, IronSource, Google AdMob, Unity Ads, 없음)
- 하이브로 자사 광고(`HighbrowAd.Show`) 사용 여부: (예: 사용 / 미사용)
- 광고 콜백이 위치한 클래스/파일: (예: `AdManager.cs`)

### 5. 세션 하트비트 추적 방식
- [ ] **(A) SDK 자동 추적 (기본 권장):** `AutoSessionTracking = true`로 설정하여 백그라운드 5분 주기 자동 전송
- [ ] **(B) 수동 제어:** 특정 씬(로비 등) 진입 시 `HighbrowLog.StartSessionTracking()` / 로그아웃 시 `StopSessionTracking()` 직접 호출

> **참고:** 신규 유저 판별, 첫 결제 판별, DAU/DADU 집계는 하이브로 중계 서버가 DB 기반으로 자동 처리하므로 클라이언트에서 별도로 연동할 필요가 없습니다.

---

## Phase 2 ~ Phase 4 실행 가이드라인 (AI 내부 지침)

- **사전 질문 응답을 받기 전까지는 절대 프로젝트 파일을 수정하지 마세요.**
- 개발자가 질문에 답변하면, 답변을 분석하여:
  1. SDK 초기화 코드 (`HighbrowSDK.Initialize(...)`) 구성 (UseSandbox 설정 포함)
  2. 수정 대상 파일 및 삽입 위치 목록
  3. `TrackAuth`, `TrackPurchase`, `TrackAd`, `SessionTracking` 적용 계획
  을 작성해 보여주고, **"이 계획대로 연동을 진행할까요? (Yes / 수정 요청)"**을 물어보세요.
- 승인을 받은 후에만 `using Highbrow.Core;`, `using Highbrow.Log;`, `using Highbrow.Ad;`를 추가하고 코드를 안전하게 삽입하세요.
```

---

## 🏛 특징 및 아키텍처 원칙

1. **클라이언트 4대 핵심 팩트 로그 모델:**
   - 클라이언트는 오직 실제 일어난 사실(Fact) 4가지만 전송:
     1. **`Auth`**: 로그인 인증 성공
     2. **`Alive`**: 5분 주기 세션 하트비트 (자동)
     3. **`Purchase`**: 인앱 결제 성공 (영수증)
     4. **`Advertise`**: 광고 시청
   - **파생 지표 자동화:** 신규 유저(New User), 첫 결제(First Purchase), 일일 활성 유저(DAU/DADU)는 **하이브로 중계 수집 서버가 자체 유저 DB를 기반으로 100% 무결하게 자동 판별**하여 Snowflake에 적재합니다. (클라이언트 연동 부담 0)
2. **2단계 자동 엔드포인트 라우팅 (Sandbox vs Production):**
   - `UseSandbox = true` 설정 시 샌드박스 주소(`https://sandbox-log-api.highbrow-inc.com`)로 자동 전송.
   - `UseSandbox = false` (기본값) 설정 시 상용 라이브 주소(`https://log-api.highbrow-inc.com`)로 자동 전송.
3. **RESTful 엔드포인트 라우팅:**
   - 인증: `/v1/log/auth`
   - 세션 하트비트: `/v1/log/alive`
   - 결제 영수증: `/v1/log/purchase`
   - 광고 시청: `/v1/log/ad`
4. **완벽한 디커플링 (Zero Coupling):**
   - 각 서브 모듈은 독립된 Assembly Definition(`.asmdef`)으로 컴파일되며 상호 참조하지 않습니다.
5. **오프라인 큐 & 자동 재시도 (PlayerPrefs Retry Queue):**
   - 네트워크 단절 또는 HTTP 오류 시 로그를 로컬 저장소(`PlayerPrefs`)에 FIFO 큐로 캐싱하며, 네트워크 정상화 시 백그라운드에서 자동 재전송합니다.
6. **공통 메타데이터 자동 주입 (Auto-Injection):**
   - UTC 시각(`Time`), 기기 고유 ID(`Duid`), OS 종류(`Os`), 스토어 마켓(`Market`), 국가 코드(`Country`), 클라이언트 버전(`ClientVersion`), 기기 스펙(`DeviceInfo`)을 SDK 내부에서 자동 수집 및 주입합니다.

---

## 📦 설치 가이드 (Unity Package Manager)

### Git URL로 설치
Unity Editor 상단 메뉴: **Window** > **Package Manager** > **`+` 버튼** > **Add package from git URL...**
```text
https://github.com/HighbrowGame/HighbrowSDK-Partner.git
```

---

## 🚀 SDK 수동 초기화 및 4대 핵심 로그 API

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
            
            EnableLog = true,                           // 로그 모듈 활성화
            AutoSessionTracking = true,                 // 5분 주기 세션 하트비트(Alive) 자동 활성화
            SessionIntervalSeconds = 300f,              // 세션 로그 주기 (기본 300초 = 5분)
            MaxOfflineQueueSize = 300,                  // 오프라인 캐시 최대 개수
            FlushRetryIntervalSeconds = 30f,            // 오프라인 큐 재전송 주기 (초)
            DebugMode = false                           // 상용 배포 시 false 지정
        };

        HighbrowSDK.Initialize(config);
    }
}
```

### 2. 4대 핵심 로그 API 요약

| 로그 종류 | API 메서드 | 설명 |
| :--- | :--- | :--- |
| **1. 인증 로그** | `HighbrowLog.TrackAuth(suid, accountId, accountType, nickname);` | 로그인 완료 시점 호출 (신규 유저/DAU 서버 자동 판별) |
| **2. 세션 하트비트** | `HighbrowLog.StartSessionTracking();` | 5분 주기 자동 발송 (`AutoSessionTracking = true` 시 자동 동작) |
| **3. 결제 영수증** | `HighbrowLog.TrackPurchase(receiptId, price, priceId, productId, productName);` | IAP 결제 성공 시점 호출 (첫 결제 여부 서버 자동 판별) |
| **4. 광고 시청** | `HighbrowLog.TrackAd(adType);` | 광고 시청 완료 시점 호출 |

---

## 🛠 개별 AI 프롬프트 템플릿 (선택 사항)

<details>
<summary><b>1. 로그인/인증 연동 템플릿 (Click to expand)</b></summary>

```markdown
# Instruction
현재 열려 있는 로그인 관리 스크립트에 `HighbrowSDK`의 인증 로그(`TrackAuth`) 연동 코드를 추가해줘.
1. 상단에 `using Highbrow.Log;` 추가.
2. 유저 로그인 성공 콜백에서 `HighbrowLog.TrackAuth(suid, accountId, accountType, nickname, "OK");` 호출.
```
</details>

<details>
<summary><b>2. IAP 결제 영수증 연동 템플릿 (Click to expand)</b></summary>

```markdown
# Instruction
현재 열려 있는 IAP 관리 스크립트에 `HighbrowSDK` 결제 영수증 로그(`TrackPurchase`)를 연동해줘.
1. 상단에 `using Highbrow.Log;` 추가.
2. 결제 완료 시점에 `HighbrowLog.TrackPurchase(receiptId, price, priceId, productId, productName);` 호출.
```
</details>

<details>
<summary><b>3. 광고 시청 연동 템플릿 (Click to expand)</b></summary>

```markdown
# Instruction
현재 열려 있는 광고 관리 스크립트에 `HighbrowSDK` 광고 시청 로그(`TrackAd`)를 연동해줘.
1. 상단에 `using Highbrow.Log;` 추가.
2. 광고 시청 완료 시: `HighbrowLog.TrackAd(adType);` 호출.
```
</details>

---

## 🔮 확장 모듈 (`Highbrow.Ad`)

- **`Highbrow.Ad` (`HighbrowAd`)**: 하이브로 자사 게임 크로스 프로모션 광고 송출 기능.
  ```csharp
  using Highbrow.Ad;

  HighbrowAd.Show(
      onCompleted: () => { /* 보상 지급 또는 게임 재개 */ },
      onFailed: () => { /* 대체 로직 */ }
  );
  ```

---

## 📞 기술 지원 및 문의
- **엔지니어링 팀:** dev@highbrow-inc.com
- **공식 웹사이트:** https://highbrow-inc.com
