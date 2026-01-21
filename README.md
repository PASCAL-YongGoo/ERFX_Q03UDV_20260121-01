# ERFX_Q03UDV_20260121-01

Mitsubishi Q03UDV PLC 모니터링 및 ZeroMQ/MQTT 메시지 브로커 애플리케이션

## 개요

이 프로젝트는 Mitsubishi MELSEC-Q 시리즈 PLC(Q03UDV)의 디바이스 값을 주기적으로 읽어 ZeroMQ와 MQTT 프로토콜로 발행하고, 외부 시스템으로부터 쓰기 명령을 수신하여 PLC에 값을 쓰는 기능을 제공합니다.

## 주요 기능

- **PLC 통신**: MX Component(ActUtlType) 기반 PLC 연결 및 디바이스 읽기/쓰기
- **ZeroMQ PUB/SUB**: 고성능 메시지 발행 및 구독
- **MQTT 3.1.1**: 표준 MQTT 프로토콜 지원
- **실시간 모니터링**: 설정 가능한 주기로 디바이스 값 모니터링
- **원격 쓰기**: 토픽 구독을 통한 외부 시스템에서 PLC 쓰기
- **보안**: 화이트리스트 기반 쓰기 주소 검증

## 아키텍처

```
┌─────────────────────────────────────────────────────────────────┐
│                        Form1 (Main UI)                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │
│  │  Timer   │  │   Grid   │  │  Status  │  │  Config  │        │
│  │ (100ms)  │  │  View    │  │  Labels  │  │  Manager │        │
│  └────┬─────┘  └──────────┘  └──────────┘  └──────────┘        │
│       │                                                         │
│       ▼                                                         │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                     PlcManager                            │  │
│  │  - Thread-safe (lock)                                     │  │
│  │  - Connection loss detection                              │  │
│  │  - Auto-reconnect support                                 │  │
│  └──────────────────────────────────────────────────────────┘  │
│       │                                                         │
└───────┼─────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────┐     ┌─────────────────────────────────────────┐
│   Mitsubishi  │     │              Message Layer               │
│   Q03UDV PLC  │     │  ┌─────────────┐    ┌─────────────┐     │
│               │     │  │   ZeroMQ    │    │    MQTT     │     │
│  ┌─────────┐  │     │  │ PUB: 5555   │    │ Broker:1883 │     │
│  │ D0, D10 │  │     │  │ SUB: 5556   │    │             │     │
│  │ M0      │  │     │  └─────────────┘    └─────────────┘     │
│  │ X0, Y0  │  │     │         │                  │            │
│  └─────────┘  │     │         ▼                  ▼            │
└───────────────┘     │    External Systems (Python, Node.js)   │
                      └─────────────────────────────────────────┘
```

## 프로젝트 구조

```
ERFX_Q03UDV_20260121-01/
├── ERFX_Q03UDV_20260121-01/
│   ├── Form1.cs                 # 메인 폼 (UI + 로직 통합)
│   ├── Form1.Designer.cs        # 폼 디자이너 코드
│   ├── PlcManager.cs            # PLC 통신 관리 (thread-safe)
│   ├── ConfigManager.cs         # 설정 파일 로드/관리
│   ├── DeviceItem.cs            # 디바이스 데이터 모델
│   │
│   ├── IMessagePublisher.cs     # 발행자 인터페이스
│   ├── IMessageSubscriber.cs    # 구독자 인터페이스
│   ├── ZeroMqPublisher.cs       # ZeroMQ PUB 구현
│   ├── ZeroMqSubscriber.cs      # ZeroMQ SUB 구현
│   ├── MqttPublisher.cs         # MQTT 발행자 구현
│   ├── MqttSubscriber.cs        # MQTT 구독자 구현
│   │
│   ├── PublishMessage.cs        # 발행 메시지 모델
│   ├── WriteCommand.cs          # 쓰기 명령 모델
│   │
│   ├── config.json              # 설정 파일
│   └── packages.config          # NuGet 패키지
│
├── BUILD_NOTES.md               # 빌드 및 개발 참고 사항
└── README.md                    # 프로젝트 문서 (이 파일)
```

## 설정 파일 (config.json)

```json
{
  "plc": {
    "stationNumber": 3
  },
  "monitoring": {
    "intervalMs": 100
  },
  "devices": [
    { "name": "데이터1", "address": "D0", "type": "Word" },
    { "name": "비트1", "address": "M0", "type": "Bit" }
  ],
  "zeromq": {
    "enabled": true,
    "publishEndpoint": "tcp://*:5555",
    "subscribeEndpoint": "tcp://localhost:5556",
    "subscribeEnabled": true,
    "topicPrefix": "plc"
  },
  "mqtt": {
    "enabled": true,
    "broker": "localhost",
    "port": 1883,
    "topicPrefix": "plc",
    "clientId": "Q03UDV_Monitor",
    "subscribeEnabled": true
  }
}
```

### 설정 항목 설명

| 섹션 | 항목 | 설명 |
|------|------|------|
| `plc` | `stationNumber` | MX Component 논리 스테이션 번호 |
| `monitoring` | `intervalMs` | 모니터링 주기 (밀리초) |
| `devices` | `name` | 디바이스 표시 이름 |
| | `address` | PLC 디바이스 주소 (D0, M0, X0, Y0 등) |
| | `type` | 데이터 타입 (Word 또는 Bit) |
| `zeromq` | `enabled` | ZeroMQ 활성화 여부 |
| | `publishEndpoint` | PUB 소켓 바인드 주소 |
| | `subscribeEndpoint` | SUB 소켓 연결 주소 |
| | `subscribeEnabled` | 구독(쓰기) 기능 활성화 |
| `mqtt` | `enabled` | MQTT 활성화 여부 |
| | `broker` | MQTT 브로커 주소 |
| | `port` | MQTT 브로커 포트 |
| | `subscribeEnabled` | 구독(쓰기) 기능 활성화 |

## 메시지 포맷

### 읽기 (Publish)

**토픽**: `{prefix}/{address}` (예: `plc/D0`)

```json
{
  "address": "D0",
  "name": "데이터1",
  "type": "Word",
  "value": 100,
  "timestamp": "2026-01-21T06:43:12.345Z"
}
```

### 쓰기 (Subscribe)

**토픽**: `{prefix}/{address}/set` (예: `plc/D0/set`)

```json
{
  "value": 100
}
```

### 시간대 규칙

| 용도 | 시간대 | 형식 |
|------|--------|------|
| 토픽 발행 (ZeroMQ/MQTT) | UTC | ISO 8601 (`2026-01-21T06:43:12.345Z`) |
| UI 표시 | KST (UTC+9) | 로컬 시간 |

## 빌드 방법

### 요구 사항

- Visual Studio 2022
- .NET Framework 4.8
- MX Component v4 (Mitsubishi)
- NuGet 패키지:
  - NetMQ 4.0.1.13
  - MQTTnet 4.3.7.1207

### 빌드 명령

```cmd
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ^
    ERFX_Q03UDV_20260121-01.csproj /t:Restore;Build /p:Configuration=Debug
```

> **주의**: COM 참조(ActUtlTypeLib)로 인해 `dotnet build`는 지원되지 않습니다.

### 빌드 오류 해결

실행 중인 애플리케이션으로 인한 파일 잠금 오류 시:
```cmd
taskkill /IM ERFX_Q03UDV_20260121-01.exe /F
```

## 테스트 방법

### MQTT 테스트

**읽기 테스트 (구독)**
```bash
mosquitto_sub -h localhost -t "plc/#" -v
```

**쓰기 테스트 (발행)**
```bash
mosquitto_pub -h localhost -t "plc/D0/set" -m '{"value": 123}'
```

### ZeroMQ 테스트 (Python)

**읽기 테스트**
```python
import zmq

context = zmq.Context()
socket = context.socket(zmq.SUB)
socket.connect("tcp://localhost:5555")
socket.setsockopt_string(zmq.SUBSCRIBE, "plc")

while True:
    topic = socket.recv_string()
    message = socket.recv_string()
    print(f"{topic}: {message}")
```

**쓰기 테스트**
```python
import zmq
import time

context = zmq.Context()
socket = context.socket(zmq.PUB)
socket.bind("tcp://*:5556")

time.sleep(1)  # 연결 대기
socket.send_string("plc/D0/set", zmq.SNDMORE)
socket.send_string('{"value": 123}')
```

## 보안 고려사항

1. **쓰기 화이트리스트**: `config.json`에 등록된 디바이스 주소만 원격 쓰기 허용
2. **네트워크 분리**: 프로덕션 환경에서는 PLC 네트워크 분리 권장
3. **인증**: MQTT 브로커 수준에서 사용자 인증 구성 권장

## 성능 최적화

| 항목 | 설명 |
|------|------|
| Serializer 캐싱 | `DataContractJsonSerializer`를 static으로 재사용 |
| 토픽 문자열 캐싱 | `DeviceItem`에 ZmqTopic/MqttTopic 미리 생성 |
| MQTT 비동기 발행 | Fire-and-forget 패턴 사용 |
| 최소화 시 UI 건너뛰기 | WindowState 체크 후 Refresh 호출 |
| 스레드 동기화 | PlcManager에 lock 적용 |

## 클래스 다이어그램

```
┌─────────────────────┐
│    <<interface>>    │
│  IMessagePublisher  │
├─────────────────────┤
│ + IsConnected       │
│ + Connect()         │
│ + ConnectAsync()    │
│ + Disconnect()      │
│ + Publish()         │
└─────────┬───────────┘
          │
    ┌─────┴─────┐
    │           │
    ▼           ▼
┌─────────┐ ┌──────────┐
│ ZeroMq  │ │   Mqtt   │
│Publisher│ │Publisher │
└─────────┘ └──────────┘

┌─────────────────────┐
│    <<interface>>    │
│  IMessageSubscriber │
├─────────────────────┤
│ + MessageReceived   │
│ + IsConnected       │
│ + Connect()         │
│ + ConnectAsync()    │
│ + Subscribe()       │
│ + SubscribeAsync()  │
└─────────┬───────────┘
          │
    ┌─────┴─────┐
    │           │
    ▼           ▼
┌──────────┐ ┌───────────┐
│ ZeroMq   │ │   Mqtt    │
│Subscriber│ │Subscriber │
└──────────┘ └───────────┘
```

## 의존성

| 패키지 | 버전 | 용도 |
|--------|------|------|
| NetMQ | 4.0.1.13 | ZeroMQ .NET 구현 |
| MQTTnet | 4.3.7.1207 | MQTT 클라이언트 |
| ActUtlTypeLib | - | MX Component COM 참조 |

## 라이선스

내부 프로젝트용

## 문의

프로젝트 관련 문의는 개발팀으로 연락하세요.
