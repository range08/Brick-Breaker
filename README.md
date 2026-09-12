# Brick Breaker

Unity로 제작한 **모바일 세로형 2D 블록 브레이커 게임**입니다. 단순한 블록 깨기 구조를 라운드 기반 멀티볼 게임으로 확장하고, 조준·물리·공 회수·난이도·UI 흐름을 실제 플레이 테스트를 통해 반복 개선했습니다.

> **Portfolio build**: [`feature/round-system`](https://github.com/range08/Brick-Breaker/tree/feature/round-system)  
> **Reference commit**: [`e544791`](https://github.com/range08/Brick-Breaker/commit/e5447913fec3805ca31550179fa6aa3603998fbd)

## Overview

- Engine: **Unity 6.3 LTS (6000.3.23f1)**
- Language: **C#**
- Target: **Android / Portrait Mobile**
- Input: **Unity New Input System**
- Core: **Unity 2D Physics, Rigidbody2D, Collider2D, LineRenderer, Coroutine, Object Pooling**

게임의 기본 흐름은 다음과 같습니다.

**Aim → Launch → Break Blocks → Gather Balls → Move Blocks Down → Next Round**

## Features

### Aim & trajectory preview

- 클릭/탭으로 즉시 발사
- 드래그 중 예상 궤적 표시
- `CircleCast`로 충돌 지점 예측
- 충돌 법선을 이용한 반사 경로 계산
- `LineRenderer`로 다중 반사 궤적 시각화

### Multiball system

- `+1` 특수 블록 획득 시 영구 Ball Count 증가
- 증가한 공은 다음 라운드부터 적용
- Object Pool 기반 공 재사용
- Ball 전용 Layer로 공끼리의 물리 충돌 차단
- 여러 공을 한 번에 생성하지 않고 순차 발사
- `launchInterval = 0.08s`를 `Time.fixedDeltaTime` 기준 **4 physics ticks**로 처리해 첫 충돌 전 간격을 일정하게 유지

### Natural ball return

- 가장 먼저 바닥에 도착한 공의 X 좌표를 다음 발사 위치로 저장
- 첫 공은 화면에 남아 다음 라운드의 기준 Ball이 됨
- 나머지 공은 약 `0.14s` 동안 첫 공 위치로 모인 뒤 Pool로 복귀
- 모든 공의 회수 연출이 완료된 후 라운드 전환

### Round progression

- Score 대신 Round 중심 진행
- 모든 공 복귀 후 Round 증가
- 기존 블록은 한 줄 아래로 이동
- 새로운 Row 생성
- Dead Line 초과 시 Game Over
- 화면 중앙의 대형 반투명 Round 숫자를 플레이 영역의 핵심 시각 요소로 사용

### Block & difficulty system

- 일반 블록 HP가 높을수록 초록색이 진해지는 Green Gradient
- `+1` Bonus Block은 노란색으로 역할을 명확히 구분
- Row마다 Bonus Block은 최대 1개
- 플레이 테스트를 통해 Block 밀도, HP 증가, Bonus 확률을 함께 조정

최종 포트폴리오 기준 튜닝 값:

| Setting | Value |
| --- | ---: |
| Starting rows | 4 |
| Row occupancy | 0.72 |
| Minimum blocks per row | 4 |
| HP growth per round | 0.35 |
| HP variance | 1 |
| Max generated HP | 12 |
| Bonus row chance | 0.30 |

### Fever

- 일정 Block Hit 수 이후 Fever 활성화
- 현재 기본 Ball 수만큼 임시 Ball 추가
- 화면 가장자리 Rainbow Border 연출
- 라운드 종료 시 Fever 종료

### Gameplay flow polish

- Pause / Resume
- 장시간 진행 시 자동 `2x / 3x` 속도 증가
- Game Over / Restart
- Safe Area 대응 UI
- Grid 외부 측면 우회 경로를 막는 gameplay wall 배치
- 화면비가 달라도 동일한 월드 폭을 유지하는 모바일 카메라

## Technical Highlights

### 1. Physics-tick based sequential launch

초기에는 Coroutine의 `WaitForSeconds`를 사용해 멀티볼을 순차 발사했지만, 렌더 프레임과 Physics tick의 차이로 공 사이 간격이 불규칙하게 보일 수 있었습니다.

이를 `WaitForFixedUpdate` 기반으로 변경하고 `0.08 / 0.02 = 4` physics ticks 간격으로 발사해 첫 충돌 전 공 간격을 일정하게 맞췄습니다.

### 2. Ball lifecycle without destroy/instantiate

공이 바닥에 닿을 때 즉시 사라졌다가 다시 나타나는 흐름 대신, 첫 공을 다음 발사 위치에 유지하고 나머지 공을 해당 위치로 모으는 방식으로 변경했습니다. 공은 Destroy/Instantiate를 반복하지 않고 Pool에서 재사용합니다.

### 3. Color semantics

높은 HP 블록과 `+1` Bonus Block이 모두 노란색 계열로 보이던 문제를 수정했습니다.

- Normal Block: **Light Green → Dark Green**
- Bonus Block: **Yellow**

색만으로도 블록 역할과 HP 수준을 빠르게 구분할 수 있도록 정리했습니다.

### 4. Iterative difficulty tuning

난이도가 지나치게 쉽거나 어려워지는 문제를 단일 수치가 아니라 **Block density / HP growth / Bonus probability**의 관계로 보고 반복 플레이하며 조정했습니다.

- Round 1: HP 1–2 중심
- Round 5: HP 2–3 중심
- Round 10: HP 4–5 중심

## Main Scripts

| Script | Responsibility |
| --- | --- |
| `GameManager.cs` | 전체 게임 상태와 라운드 흐름 관리 |
| `BallManager.cs` | Ball Pool, 멀티볼 발사, 입력, 회수 관리 |
| `BallScript.cs` | 개별 Ball 물리 이동과 충돌 |
| `TrajectoryPreview.cs` | CircleCast 기반 예상 궤적 계산 |
| `BlockGridManager.cs` | 블록 생성, 이동, HP 및 난이도 관리 |
| `BrickBlock.cs` | 블록 충돌, HP, Bonus 처리 |
| `FeverController.cs` | Fever 상태와 Border 연출 |
| `TimeController.cs` | Pause 및 자동 게임 속도 제어 |
| `GameHud.cs` | Scene 기반 HUD 연결 및 갱신 |
| `WallLayoutController.cs` | Gameplay wall / Dead Zone 배치 |

## Controls

| Input | Action |
| --- | --- |
| Tap / Click | 해당 방향으로 즉시 발사 |
| Drag | 예상 궤적 확인 |
| Release | 드래그한 방향으로 발사 |
| Pause | 게임 일시정지 / 재개 |

## Run the Project

```bash
git clone https://github.com/range08/Brick-Breaker.git
cd Brick-Breaker
git checkout feature/round-system
```

1. Unity Hub에서 **Unity 6.3 LTS**로 프로젝트를 엽니다.
2. `Assets/Scenes/main.unity`를 엽니다.
3. Play Mode를 실행합니다.

## Repository Status

`main`은 프로젝트의 안정적인 기준 브랜치이며, 현재 포트폴리오 제출 기준 구현은 `feature/round-system` 브랜치에 있습니다. 제출 이후 추가 개선과 정리를 계속 진행할 예정입니다.

---

This repository is a gameplay programming portfolio project focused on **Unity/C# gameplay systems, 2D physics, mobile input, UI/UX iteration, and play-test driven balancing**.
