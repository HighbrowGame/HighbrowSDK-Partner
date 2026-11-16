# Changelog

개발사 마이그레이션 기준 문서입니다. AI 업데이트(`INTEGRATION_PROMPT.md` 업데이트 모드)는 설치된 버전보다 높고 최신 버전 이하인 모든 항목을, 오래된 버전부터 순서대로 적용합니다. 이 파일은 최신 버전이 맨 위에 있으므로 아래에서 위로 읽습니다.
각 항목은 `변경 내용 / 찾는 방법 / 수정 방법 / 사람 판단 필요`를 적습니다. 코드 수정이 필요 없는 버전도 "코드 수정 없음"으로 반드시 기록합니다.

> 릴리즈 시 이 파일에 새 버전 항목을 추가하세요. (최신 버전이 맨 위)

---

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
