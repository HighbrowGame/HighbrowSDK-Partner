# Highbrow SDK (Unity Client)

`HighbrowSDK`는 퍼블리싱 파트너사를 위한 공식 Unity 클라이언트 SDK입니다.
안정적인 실시간 로그 수집(Snowflake 연동)을 시작으로 자사 게임 광고(`Highbrow.Ad`)까지 유연하게 확장 가능한 모듈형 아키텍처로 설계되었습니다.

---

## 📑 목차
1. [🌟 5분 AI 초고속 연동 (한 줄 킥오프)](#-5분-ai-초고속-연동-한-줄-킥오프)
2. [📘 사람 개발자용 상세 연동 매뉴얼 (INTEGRATION_GUIDE.md)](INTEGRATION_GUIDE.md)
3. [특징 및 아키텍처 원칙](#-특징-및-아키텍처-원칙)
4. [설치 가이드 (Unity Package Manager)](#-설치-가이드-unity-package-manager)
5. [SDK 수동 초기화 및 4대 핵심 로그 API](#-sdk-수동-초기화-및-4대-핵심-로그-api)
6. [개별 AI 프롬프트 템플릿 (선택 사항)](#-개별-ai-프롬프트-템플릿-선택-사항)
7. [확장 모듈 (`Highbrow.Ad`)](#-확장-모듈-highbrowad)

---

## 🌟 5분 AI 초고속 연동 (한 줄 킥오프)

파트너사 개발자는 복잡한 가이드를 읽거나 긴 코드를 일일이 복사할 필요가 없습니다.<br>
사용 중인 AI 어시스턴트(**Cursor Composer, GitHub Copilot Chat, Claude Code, Windsurf**) 채팅창에 **다음 한 줄**을 그대로 입력하세요:

> 📋 **AI 킥오프 프롬프트 (복사하여 AI에 입력):**
> ```text
> INTEGRATION_PROMPT.md를 읽고 우리 프로젝트에 HighbrowSDK를 연동해줘
> ```

AI가 프로젝트 내의 초기화, 로그인(`TrackAuth`), 인앱 결제(`TrackPurchase`), 광고(`TrackAd`) 위치를 스스로 탐색하고, 맞춤형 연동 계획을 제시한 후 안전하게 코드를 자동 삽입합니다.

> 📖 **사람 개발자를 위한 상세 연동 매뉴얼:**<br>
> AI 도구 없이 직접 단계별로 수동 연동하거나, QA 체크리스트 및 상세 파라미터 설정을 확인하려면 [📘 INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md)를 확인하세요.

---

## 🏛 특징 및 아키텍처 원칙

1. **클라이언트 4대 핵심 팩트 로그 모델:**
   - 클라이언트는 오직 실제 일어난 사실(Fact) 4가지만 전송:
     1. **`Auth`**: 로그인 인증 성공 (ID 4종 자동 캐싱 & 1초 후 Alive 시작)
     2. **`Alive`**: 2분 주기 세션 하트비트 (TrackAuth 1초 후 자동 동작)
     3. **`Purchase`**: 인앱 결제 성공 (캐시된 SUID/DUID 자동 주입)
     4. **`Advertise`**: 광고 시청 (캐시된 SUID 자동 주입)
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
        // 타겟 스토어에 맞춘 동적 마켓 분기 (원스토어, 구글, 애플, 스팀 등)
        MarketType targetMarket = MarketType.None;
#if UNITY_IOS
        targetMarket = MarketType.AppleStore;
#elif UNITY_ANDROID
    #if ONESTORE
        targetMarket = MarketType.OneStore;
    #else
        targetMarket = MarketType.GooglePlay;
    #endif
#elif UNITY_STANDALONE_WIN
        targetMarket = MarketType.Steam;
#endif

        HighbrowConfig config = new HighbrowConfig
        {
            AppKey = "YOUR_ISSUED_HIGHBROW_APP_KEY",   // 하이브로 발급 앱 키
            Market = targetMarket,                      // 동적으로 감지된 타겟 마켓 지정
            ClientVersion = Application.version,        // 클라이언트 앱 버전 (null 시 Application.version 자동 사용)
            UseSandbox = false,                         // true: 샌드박스 테스트, false: 상용 라이브 배포
            
            EnableLog = true,                           // 로그 모듈 활성화
            AutoSessionTracking = true,                 // 2분 주기 세션 하트비트(Alive) 자동 활성화
            SessionIntervalSeconds = 120f,              // 세션 로그 주기 (기본 120초 = 2분)
            HttpTimeoutSeconds = 10,                    // HTTP 요청 타임아웃 (3~30초 범위 자동 클램프)
            MaxOfflineQueueSize = 300,                  // 오프라인 캐시 최대 개수
            FlushRetryIntervalSeconds = 30f,            // 오프라인 큐 재전송 주기 (초)
            CustomCountry = null,                       // 특정 국가 고정 시 2자리 ISO 코드(예: "KR", "US"). null 시 자동 감지
            DebugMode = false,                          // 상용 배포 시 false 지정
            DumpHttpPayload = false                     // DebugMode와 함께 true일 때만 요청/응답 전문 출력
        };

        HighbrowSDK.Initialize(config);

        // 또는 인스펙터 GUI(Highbrow > SDK Settings)를 사용하는 경우 한 줄로 초기화 가능:
        // HighbrowSDK.Initialize();
    }
}
```

### 2. 4대 핵심 로그 API 요약

| 로그 종류 | API 메서드 | 설명 |
| :--- | :--- | :--- |
| **1. 인증 로그** | `HighbrowLog.TrackAuth(suid, accountId, accountType, nickname, duid);` | 로그인 완료 시점 호출 (ID 4종 자동 캐싱 & 1초 후 2분 주기 Alive 자동 시작) |
| **2. 세션 하트비트** | `HighbrowLog.StartSessionTracking();` | 2분 주기 자동 발송 (`AutoSessionTracking = true` 시 TrackAuth 직후 자동 실행) |
| **3. 결제 영수증** | `HighbrowLog.TrackPurchase(receiptId, originalPrice, priceId, currency, productId, productName);` | IAP 결제 성공 시점 호출 (currency는 `args.purchasedProduct.metadata.isoCurrencyCode` 전달) |
| **4. 광고 시청** | `HighbrowLog.TrackAd(adType);` | 광고 시청 완료 시점 호출 (캐시된 SUID 자동 주입) |

> ⚠️ **결제 로그 연동 주의사항:**
> - `TrackPurchase`는 유저 로그인(`TrackAuth`)이 완료된 이후에만 수집됩니다. 미처리 결제 복구(Pending Purchase) 처리는 반드시 로그인 완료 후에 수행되도록 구성하세요.
> - `receiptId`에는 raw JSON 영수증 대신 스토어 트랜잭션 ID(`args.purchasedProduct.transactionID`)를 직접 전달하세요.
> - `currency`에는 스토어 결제 통화 코드(`args.purchasedProduct.metadata.isoCurrencyCode`)를 전달하세요. (미지정 시 기본값 `"KRW"`)

---

## 🛠 개별 AI 프롬프트 템플릿 (선택 사항)

<details>
<summary><b>1. 로그인/인증 연동 템플릿 (Click to expand)</b></summary>

```markdown
# Instruction
현재 열려 있는 로그인 관리 스크립트에 `HighbrowSDK`의 인증 로그(`TrackAuth`) 연동 코드를 추가해줘.
1. 상단에 `using Highbrow.Log;` 추가.
2. 유저 로그인 성공 콜백에서 `HighbrowLog.TrackAuth(suid, accountId, accountType, nickname, result: "OK");` 호출.
```
</details>

<details>
<summary><b>2. IAP 결제 영수증 연동 템플릿 (Click to expand)</b></summary>

```markdown
# Instruction
현재 열려 있는 IAP 관리 스크립트에 `HighbrowSDK` 결제 영수증 로그(`TrackPurchase`)를 연동해줘.
1. 상단에 `using Highbrow.Log;` 추가.
2. 결제 완료 시점에 `HighbrowLog.TrackPurchase(receiptId: args.purchasedProduct.transactionID, originalPrice: (float)args.purchasedProduct.metadata.localizedPrice, priceId: args.purchasedProduct.definition.id, currency: args.purchasedProduct.metadata.isoCurrencyCode, productId: internalId, productName: args.purchasedProduct.metadata.localizedTitle);` 호출.
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
  - 영상 재생 후 카운트다운이 끝나고 유저가 **닫기(스킵) 버튼을 클릭했을 때** `onCompleted` 콜백이 안전하게 호출됩니다.
  - UI 상호작용(닫기, 스토어 이동 등)을 위해 씬에 **EventSystem**이 존재해야 합니다.
  ```csharp
  using Highbrow.Ad;

  HighbrowAd.Show(
      onCompleted: () => { /* 닫기/스킵 완료 시 보상 지급 또는 게임 재개 */ },
      onFailed: () => { /* 대체 로직 */ }
  );
  ```

---

## 📞 기술 지원 및 문의
- **기술 지원 및 AppKey 발급 문의:** kms@highbrow.com
