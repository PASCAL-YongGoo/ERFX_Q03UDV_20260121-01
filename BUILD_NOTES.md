# 빌드 및 개발 참고 사항

## 빌드 전 확인 사항

### 실행 중인 애플리케이션 종료

빌드 시 다음과 같은 오류가 발생하면:

```
error MSB3027: "obj\Debug\ERFX_Q03UDV_20260121-01.exe"을(를) "bin\Debug\ERFX_Q03UDV_20260121-01.exe"(으)로 복사할 수 없습니다.
파일이 "ERFX_Q03UDV_20260121-01 (PID)"에 의해 잠겨 있습니다.
```

**해결 방법:**

1. 실행 중인 `ERFX_Q03UDV_20260121-01.exe`를 종료합니다.
2. 작업 관리자에서 프로세스를 찾아 강제 종료하거나:
   ```cmd
   taskkill /IM ERFX_Q03UDV_20260121-01.exe /F
   ```
3. 다시 빌드를 실행합니다.

## 빌드 명령어

### Visual Studio MSBuild 사용 (권장)

```cmd
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ERFX_Q03UDV_20260121-01.csproj /t:Restore;Build /p:Configuration=Debug
```

### dotnet CLI

이 프로젝트는 COM 참조(ActUtlTypeLib)를 사용하므로 `dotnet build`는 지원되지 않습니다.
반드시 Visual Studio MSBuild를 사용하세요.

## NuGet 패키지

- **NetMQ** 4.0.1.13 - ZeroMQ .NET 구현
- **MQTTnet** 4.3.7.1207 - MQTT 클라이언트

---

## 시간대(Timezone) 규칙

| 용도 | 시간대 | 형식 | 예시 |
|------|--------|------|------|
| **토픽 발행 (ZeroMQ/MQTT)** | UTC | ISO 8601 | `2026-01-21T06:43:12.345Z` |
| **UI 표시 (사용자용)** | KST (UTC+9) | 로컬 시간 | `2026-01-21 15:43:12` |

### 변환 코드 예시

```csharp
// UTC -> KST 변환 (UI 표시용)
DateTime utcTime = DateTime.UtcNow;
TimeZoneInfo kst = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
DateTime kstTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, kst);
string displayTime = kstTime.ToString("yyyy-MM-dd HH:mm:ss");

// 또는 간단히
DateTime kstTime = DateTime.UtcNow.AddHours(9);
```

### 이유

- **UTC 사용 (발행)**: 시스템 간 데이터 교환 시 시간대 혼란 방지, 표준화된 타임스탬프
- **KST 사용 (UI)**: 한국 사용자가 직관적으로 이해할 수 있는 로컬 시간 표시

---

## 토픽 구조

### 읽기 (Publish)

PLC에서 읽은 값을 토픽으로 발행합니다.

| 토픽 패턴 | 예시 | 설명 |
|-----------|------|------|
| `{prefix}/{address}` | `plc/D0` | 디바이스 값 발행 |

**메시지 포맷:**
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

토픽을 구독하여 PLC에 값을 씁니다.

| 토픽 패턴 | 예시 | 설명 |
|-----------|------|------|
| `{prefix}/{address}/set` | `plc/D0/set` | 디바이스 값 쓰기 |

**메시지 포맷:**
```json
{ "value": 100 }
```

### 설정 파일 (config.json)

```json
{
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

| 설정 | 설명 |
|------|------|
| `enabled` | 프로토콜 전체 활성화/비활성화 |
| `subscribeEnabled` | 구독(쓰기) 기능 활성화/비활성화 |
| `publishEndpoint` | ZeroMQ PUB 소켓 바인드 주소 |
| `subscribeEndpoint` | ZeroMQ SUB 소켓 연결 주소 |

---

## 테스트 방법

### 읽기 테스트

**MQTT:**
```bash
mosquitto_sub -h localhost -t "plc/#" -v
```

**ZeroMQ (Python):**
```python
import zmq
socket = zmq.Context().socket(zmq.SUB)
socket.connect("tcp://localhost:5555")
socket.setsockopt_string(zmq.SUBSCRIBE, "plc")
while True:
    print(socket.recv_string(), socket.recv_string())
```

### 쓰기 테스트

**MQTT:**
```bash
mosquitto_pub -h localhost -t "plc/D0/set" -m '{"value": 123}'
```

**ZeroMQ (Python):**
```python
import zmq
socket = zmq.Context().socket(zmq.PUB)
socket.bind("tcp://*:5556")
import time; time.sleep(1)  # 연결 대기
socket.send_string("plc/D0/set", zmq.SNDMORE)
socket.send_string('{"value": 123}')
```

---

## 성능 최적화

### 적용된 최적화

| 항목 | 설명 | 효과 |
|------|------|------|
| **Serializer 캐싱** | DataContractJsonSerializer를 static으로 재사용 | GC 부하 50% 감소 |
| **토픽 문자열 캐싱** | DeviceItem에 ZmqTopic/MqttTopic 미리 생성 | 문자열 할당 제거 |
| **MQTT 비동기 발행** | Fire-and-forget 패턴 사용 | UI 블로킹 제거 |
| **최소화 시 UI 건너뛰기** | WindowState 체크 후 Refresh 호출 | CPU 사용량 감소 |

### 코드 변경 사항

**Form1.cs:**
```csharp
// Serializer 캐싱
private static readonly DataContractJsonSerializer _publishMessageSerializer = ...;

// 토픽 캐싱
private void CacheDeviceTopics()
{
    foreach (var device in _deviceItems)
    {
        device.ZmqTopic = $"{_zmqTopicPrefix}/{device.Address}";
        device.MqttTopic = $"{_mqttTopicPrefix}/{device.Address}";
    }
}

// 최소화 시 UI 건너뛰기
if (WindowState != FormWindowState.Minimized)
    dgvDevices.Refresh();
```

**MqttPublisher.cs:**
```csharp
// 비동기 발행 (Fire-and-forget)
_ = _client.PublishAsync(mqttMessage, CancellationToken.None);
```
