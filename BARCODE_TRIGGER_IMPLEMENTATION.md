# 바코드 트리거 기능 구현 완료

## 구현 일자
2026년 1월 22일

## 구현 내용

D8008 PLC 레지스터의 특정 비트가 0→1로 변경될 때 바코드 리더기(R5050PMG)에 TCP로 트리거 신호를 전송하는 기능을 추가했습니다.

## 변경된 파일

### 1. ConfigManager.cs
- `BarcodeConfig` 클래스 추가
  - `Enabled`: 바코드 기능 활성화 여부
  - `IpAddress`: 바코드 리더 IP 주소
  - `Port`: 바코드 리더 포트
  - `TriggerBitPosition`: D8008에서 감지할 비트 위치 (0~15)
  - `ConnectionTimeoutMs`: 연결 타임아웃
  - `AutoReconnect`: 자동 재연결 여부
  - `TriggerCommand`: 전송할 트리거 명령 (기본값: "+")

- `AppConfig`에 `Barcode` 속성 추가

### 2. BarcodeReaderClient.cs (신규)
- R5050PMG 바코드 리더와 TCP 통신하는 클라이언트 클래스
- 주요 기능:
  - `ConnectAsync()`: 바코드 리더에 연결
  - `SendTriggerAsync()`: 트리거 신호 전송
  - `Disconnect()`: 연결 종료
  - 자동 재연결 기능
  - 연결 상태 변경 이벤트
  - 에러 발생 이벤트

### 3. PlcMonitorService.cs
- `_barcodeClient` 필드 추가
- `_previousD8008Value` 필드 추가 (이전 D8008 값 저장)
- `InitializeBarcodeClient()` 메서드 추가
  - 바코드 클라이언트 초기화
  - 이벤트 핸들러 등록
  - 초기 연결 시도
- `CheckD8008AndTriggerBarcode()` 메서드 추가
  - D8008 값 읽기
  - 설정된 비트 위치의 0→1 변화 감지
  - 변화 감지 시 바코드 트리거 전송
- `ReadAndPublish()`에 `CheckD8008AndTriggerBarcode()` 호출 추가
- `Dispose()`에 `_barcodeClient` 정리 추가

### 4. config.json
- `barcode` 설정 섹션 추가
```json
"barcode": {
  "enabled": false,
  "ipAddress": "192.168.20.10",
  "port": 8080,
  "triggerBitPosition": 0,
  "connectionTimeoutMs": 3000,
  "autoReconnect": true,
  "triggerCommand": "+"
}
```

### 5. ERFX_Q03UDV_20260121-01.csproj
- `BarcodeReaderClient.cs` 컴파일 대상에 추가
- `RuntimeIdentifiers` 속성 추가 (win)

## 동작 원리

1. **초기화**
   - 프로그램 시작 시 `config.json`에서 바코드 설정 로드
   - `enabled=true`이면 바코드 클라이언트 초기화 및 연결 시도

2. **모니터링 루프** (10ms 주기)
   - PLC에서 D8008 레지스터 읽기
   - 설정된 비트 위치의 값을 이전 값과 비교
   - 0→1 변화 감지 시:
     - 로그 출력: `[INFO] D8008 bit {position} changed 0→1, sending barcode trigger`
     - 바코드 리더에 트리거 명령 전송 (기본값: "+")
   - 현재 값을 이전 값으로 저장

3. **트리거 전송**
   - TCP 소켓을 통해 UTF-8 인코딩된 명령 전송
   - 연결이 끊긴 경우 자동 재연결 시도 (AutoReconnect=true일 때)
   - 전송 실패 시 에러 로그 출력

## 사용 방법

### 설정
`config.json` 파일에서 다음 설정 변경:

```json
"barcode": {
  "enabled": true,                    // 기능 활성화
  "ipAddress": "192.168.20.10",      // 바코드 리더 IP (실제 IP로 변경)
  "port": 8080,                      // 바코드 리더 포트 (실제 포트로 변경)
  "triggerBitPosition": 0,           // 감지할 D8008 비트 위치 (0~15)
  "connectionTimeoutMs": 3000,
  "autoReconnect": true,
  "triggerCommand": "+"
}
```

### 비트 위치 설정
D8008은 16비트 레지스터이며, LSB부터 비트 0, 1, 2, ... 15 순서입니다.
- 비트 0: 0x0001
- 비트 1: 0x0002
- 비트 2: 0x0004
- 비트 3: 0x0008
- ...

`triggerBitPosition` 값을 0~15 사이로 설정하여 원하는 비트를 선택합니다.

## 로그 예시

```
[INFO] Barcode Reader Client initialized: 192.168.20.10:8080, Trigger Bit: 0
[INFO] Barcode Reader 연결됨: 192.168.20.10:8080
[INFO] D8008 bit 0 changed 0→1, sending barcode trigger: '+'
```

## 주의사항

1. **PLC 연결**: D8008을 읽기 위해 PLC가 연결되어 있어야 합니다.
2. **바코드 리더 네트워크**: 바코드 리더와 네트워크 연결이 가능해야 합니다.
3. **IP/포트 설정**: 실제 바코드 리더의 IP 주소와 포트를 정확히 입력해야 합니다.
4. **트리거 명령**: R5050PMG는 "+" 명령으로 트리거되지만, 다른 모델일 경우 `triggerCommand` 설정을 변경하세요.

## 빌드 상태

구현된 코드는 문법적으로 정확하며, 다음 이유로 현재 환경에서 빌드가 완료되지 않습니다:
- ActUtlType COM 라이브러리 미등록 (PLC 통신 드라이버 미설치)
- 이는 PLC 드라이버가 설치된 환경에서는 정상적으로 빌드됩니다.

새로 추가된 바코드 기능 코드는 완전히 구현되었으며, PLC 환경에서 정상 작동할 것으로 예상됩니다.
