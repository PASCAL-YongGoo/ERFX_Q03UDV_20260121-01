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
| ERFX_R5050PMG | 바코드 리더 | `..\ERFX_R5050PMG_20260121-01` |
| ERFX_BlueBird_FR900 | RFID 리더 | `..\ERFX_BlueBird_FR900_20260112-01` |

### 개발 시 필수 규칙

1. **메시지 포맷 변경 시**: `..\ERFX_Integration\Message_Specification.md` 동기화 필수
2. **토픽 추가/변경 시**: `..\ERFX_Integration\Topic_Reference.md` 동기화 필수
3. **연동 로직 변경 시**: `..\ERFX_Integration\Integration_Plan.md` 업데이트

### 이 프로젝트의 연동 역할

| 기능 | 토픽 | 상태 |
|------|------|:----:|
| 센서 상태 발행 | `erfx/plc/sensor/{address}` | ⚠️ 토픽 변경 필요 |
| 디바이스 값 발행 | `erfx/plc/device/{address}` | ⚠️ 토픽 변경 필요 |
| 디바이스 쓰기 수신 | `erfx/plc/device/{address}/set` | ⚠️ 토픽 변경 필요 |
| 바코드 트리거 발행 | `erfx/barcode/trigger` | ➖ 제거됨 (바코드 리더 프로그램에서 처리) |
| RFID 트리거 발행 | `erfx/rfid/trigger` | ❌ |
| 박스 도착 이벤트 | `erfx/workflow/box_arrived` | ❌ |

---

## Project Structure

### Directory Organization

```
📁 프로젝트 루트/
├── README.md                          # 프로젝트 메인 문서
├── CLAUDE.md                          # Claude 프로젝트 설정
├── ERFX_Q03UDV_20260121-01.sln        # 솔루션 파일
├── ERFX_Q03UDV_20260121-01/           # 소스 코드
├── docs/                              # 일반 문서
│   ├── BUILD_NOTES.md                 # 빌드 노트
│   └── PLC_Address_Description.md
├── reference/                         # 참조 자료 (데이터시트, 매뉴얼)
│   └── 이랜드 PLC 어드레스 구현 설명_*.xlsx/csv
└── temp/                              # 임시 파일 (git 제외)
```

### 정리 원칙
- **프로젝트 루트**: 필수 파일만 (README, CLAUDE.md, 솔루션 파일)
- **docs/**: 일반 문서 및 노트
- **reference/**: 참조 자료 (데이터시트, 매뉴얼)
- **temp/**: 임시 파일 (git에서 제외)

## Development Guidelines
- Always keep the project root directory clean and organized
- Temporary files must be stored in `temp/` directory
- **경로는 항상 상대 경로로 기록** (여러 PC에서 개발하므로 절대 경로 사용 금지)
- **경로 구분자는 `\` 사용** (C# 프로젝트 규칙)

---

## Change Log

### 2026-01-23
- 프로젝트 폴더 정리
  - `BUILD_NOTES.md` → `docs/`로 이동
  - `nul` 파일 삭제 (불필요한 파일)
- 바코드 트리거 기능 제거
  - 바코드 리더 프로그램(R5050PMG)과 포트 충돌 문제로 제거
  - 트리거는 바코드 리더 프로그램에서 처리하도록 변경
  - 제거된 파일: `BarcodeReaderClient.cs`, `TriggerMessage.cs`, `docs\BARCODE_TRIGGER_IMPLEMENTATION.md`
  - `config.json`에서 `barcode` 섹션 제거
