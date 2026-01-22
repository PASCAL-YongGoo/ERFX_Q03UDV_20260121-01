# ERFX_Q03UDV Project Guidelines

## ERFX 시스템 연동 (필독)

이 프로젝트는 **ERFX 통합 시스템**의 일부입니다. 개발 시 반드시 공유 문서를 참조하세요.

### 공유 문서 위치

```
📁 ..\ERFX_Integration\
├── README.md                  # 개요
├── Integration_Plan.md        # 연동 계획서 (아키텍처, 시나리오, 구현 가이드)
├── Message_Specification.md   # 메시지 포맷 명세 (JSON 구조, 필드 정의)
└── Topic_Reference.md         # 토픽 레퍼런스 (MQTT/ZeroMQ 토픽 체계)
```

### 관련 프로젝트

| 프로젝트 | 역할 | 경로 |
|----------|------|------|
| **ERFX_Q03UDV** | PLC 모니터링 | 현재 프로젝트 |
| ERFX_R5050PMG | 바코드 리더 | `../ERFX_R5050PMG_20260121-01` |
| ERFX_BlueBird_FR900 | RFID 리더 | `../ERFX_BlueBird_FR900_20260112-01` |

### 개발 시 필수 규칙

1. **메시지 포맷 변경 시**: `ERFX_Integration/Message_Specification.md` 동기화 필수
2. **토픽 추가/변경 시**: `ERFX_Integration/Topic_Reference.md` 동기화 필수
3. **연동 로직 변경 시**: `ERFX_Integration/Integration_Plan.md` 업데이트

### 이 프로젝트의 연동 역할

| 기능 | 토픽 | 상태 |
|------|------|:----:|
| 센서 상태 발행 | `erfx/plc/sensor/{address}` | ⚠️ 토픽 변경 필요 |
| 디바이스 값 발행 | `erfx/plc/device/{address}` | ⚠️ 토픽 변경 필요 |
| 디바이스 쓰기 수신 | `erfx/plc/device/{address}/set` | ⚠️ 토픽 변경 필요 |
| 바코드 트리거 발행 | `erfx/barcode/trigger` | ❌ |
| RFID 트리거 발행 | `erfx/rfid/trigger` | ❌ |
| 박스 도착 이벤트 | `erfx/workflow/box_arrived` | ❌ |

---

## Project Structure

### Directory Organization
- **Project Root**: Keep clean - only essential files (README, CLAUDE.md, solution file, important documentation)
- **docs/**: General documentation and notes
- **temp/**: Temporary files (excluded from git)
- **reference/**: Reference materials (datasheets, manuals)

## Development Guidelines
- Always keep the project root directory clean and organized
- Temporary files must be stored in `temp/` directory
