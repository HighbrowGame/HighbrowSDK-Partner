# Changelog

개발사 마이그레이션 기준 문서입니다. AI 업데이트(`INTEGRATION_PROMPT.md` 업데이트 모드)는 설치된 버전보다 높고 최신 버전 이하인 모든 항목을, 오래된 버전부터 순서대로 적용합니다. 이 파일은 최신 버전이 맨 위에 있으므로 아래에서 위로 읽습니다.
각 항목은 `변경 내용 / 찾는 방법 / 수정 방법 / 사람 판단 필요`를 적습니다. 코드 수정이 필요 없는 버전도 "코드 수정 없음"으로 반드시 기록합니다.

> 릴리즈 시 이 파일에 새 버전 항목을 추가하세요. (최신 버전이 맨 위)

---

## 1.1.0

- **변경 내용** · Unity IAP 상품의 가격과 통화를 한 번에 읽는 `HighbrowIap.TrackPurchase`를 추가했습니다. Unity IAP 4 또는 5가 설치된 프로젝트에서 사용할 수 있습니다.
- 새로 생성하는 Auth, Alive, Purchase, Advertise 로그 JSON에 `SdkVersion`을 포함합니다. 기존 큐 로그는 생성 당시의 JSON으로 재전송합니다.
- 기존 `originalPrice` 하나를 받는 두 오버로드를 제거했습니다. 자체 결제 연동은 `storePrice`와 `storeCurrency`를 같은 검증 주문에서 필수로 전달합니다.
- **찾는 방법** · 결제 완료 콜백의 `HighbrowLog.TrackPurchase`와 `HighbrowLog.Manager.TrackPurchase` 호출을 확인하세요.
- **수정 방법** · IAP 4는 `HighbrowIap.TrackPurchase(args.purchasedProduct)`로 교체하세요. IAP 5는 해당 주문의 상품과 영수증 또는 스토어 주문 ID를 전달하세요. 커스텀 asmdef는 `Highbrow.Iap`를 참조해야 합니다.
- **사람 판단 필요** · 자체 검증 서버 연동은 위의 새 인자를 사용하세요. `OriginalPrice`와 `Currency`에는 같은 주문의 스토어 금액과 통화가 들어갑니다.

## 1.0.4

- **변경 내용:** 현재 앱 실행에서 로그인 성공을 알리는 `TrackAuth` 호출 이후에만 Alive를 전송합니다. 이전 실행에서 저장된 Alive와 큐에서 그 뒤에 있는 로그는 순서를 유지하기 위해 로그인 성공까지 보관합니다.
- **찾는 방법:** 로그인 성공 콜백의 `TrackAuth`와 수동 `StartSessionTracking`, 로그아웃의 `ClearUser` 호출을 확인하세요.
- **수정 방법:** 기존 연동이 로그인 성공 콜백에서 `TrackAuth`를 호출한다면 코드 수정이 필요 없습니다. 수동 세션 추적은 `TrackAuth` 이후에 시작하고 로그아웃 시 `ClearUser`를 호출하세요.
- **사람 판단 필요:** 로그인 성공 시 `result`를 생략하거나 `"OK"`를 전달하는지 확인하세요.

## 1.0.3

- **변경 내용:**
  - 로그 서버 장애(5xx) 시 오프라인 큐가 같은 로그를 30초마다 무한 재전송해 서버에 중복 로그가 쌓이던 문제 수정. 서버 오류 응답을 받은 로그는 재전송 3회 실패 시 폐기하고, 재전송 실패가 이어지면 자동 재전송 간격을 30초→1분→2분…(최대 10분)으로 늘림. 네트워크 단절(응답 없음)은 횟수에 세지 않아 오프라인 중 로그는 계속 보존됨.
  - 공개 API(`FlushOfflineQueue`, `MaxOfflineQueueSize`, `FlushRetryIntervalSeconds`)는 변경 없음. 코드 수정 없음.
- **찾는 방법:** 해당 없음.
- **수정 방법:** 코드 수정 없음. 패키지 버전만 올리면 됨.
- **사람 판단 필요:** 없음.

## 1.0.2

- **변경 내용:**
  - 결제 로그 `receiptId` 처리 변경. `product.receipt`(Unity IAP 영수증 전문)를 그대로 넘기면 SDK가 스토어별 주문번호를 추출함. (Google: Payload 안의 `orderId`, Apple: `TransactionID`. OneStore는 영수증 샘플 확인 전까지 `orderId` 등을 찾는 임시 규칙)
  - 샌드박스(`UseSandbox = true`)에서만 결제 로그에 `RawData`(넘겨받은 영수증 원문, 8192자 초과 시 생략)를 추가 전송함. 주문번호를 못 찾아도 샌드박스에서는 `ReceiptId`를 `UNRESOLVED`로 채워 `RawData`와 함께 전송함(점검용). 라이브에서는 전송하지 않고 거부함. 코드 수정 없음.
  - `args.purchasedProduct.transactionID`는 Google Play에서 주문번호가 아니라 구매 토큰이 올 수 있어 사용 금지. 128자를 넘는 값은 이전에는 잘라서 전송했으나, 이제는 오류로 거부함.
- **찾는 방법:** 프로젝트 전체에서 `HighbrowLog.TrackPurchase(` 호출을 모두 검색해 `receiptId` 인자를 확인.
- **수정 방법:**
  - `receiptId: ...transactionID`(또는 `.transactionID`에서 가져온 변수) → `receiptId: args.purchasedProduct.receipt`로 변경.
  - 자체 결제 검증 서버를 쓰는 경우: Google은 `orderId`(`GPA.xxxx`, 구매 토큰 아님), Apple은 transactionID를 넘기고 있는지 확인.
- **사람 판단 필요:** 자체 검증 서버가 Google 구매 토큰만 내려주는 구조라면 `orderId`를 함께 내려주도록 서버 수정 여부.

## 1.0.1

- **변경 내용:**
  - `AccountType.Guest`가 수집 서버 기준 값 `0`(`None`과 동일)으로 변경됨. 기존 `Guest = 9`는 제거됨.
  - `TrackAuth`: `suid`는 유저 고유 ID(필수), `accountId`는 게임이 계정별(서버별 등)로 정의한 ID이며 `null`/빈 값이면 SDK가 SUID로 대체함. (게스트 전용 처리는 제거)
  - 계정 전환(게스트→구글 등) 시 Alive 로그에 이전 AccountType이 남던 문제 수정.
- **찾는 방법:**
  - 프로젝트 전체에서 `HighbrowLog.TrackAuth(` 호출을 모두 검색.
  - `(AccountType)` 캐스팅 또는 `AccountType`에 숫자(예: `9`)를 직접 지정한 코드를 검색.
- **수정 방법:**
  - 모든 `TrackAuth` 호출의 `accountId`를 `null`로 변경. (게임이 계정별 ID를 따로 정의한 경우에만 그 값 유지. 구글 sub ID, 애플 user ID, 생성 시각 등은 제거)
  - 게스트는 `AccountType.Guest` 사용. 숫자 직접 지정·캐스팅은 열거형 값으로 교체.
- **사람 판단 필요:** 게임에 서버별 등 계정별 ID가 실제로 있는 경우, 그 값을 `accountId`로 넘길지 여부.

## 1.0.0

- 최초 정식 릴리즈. (마이그레이션 없음)
