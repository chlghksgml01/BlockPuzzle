# MissionManager / MissionHUD 구조

LevelInGame 씬의 미션 세션·진행도·HUD 표시.

## 책임 분리

| 클래스 | 책임 |
|--------|------|
| `LevelSessionContext` | 씬 간 전달 (정적, 진입 전 BeginLevel) |
| `MissionManager` | 세션 캐시 + **진행도** (남은 Ice/Grass/Gem, 점수 목표) |
| `MissionHUD` | 레벨/남은 수/목표 점수 **표시만** |
| `MissionBoardController` | 보드 레이아웃·팔레트 적용 |
| `InGameManager` | 게임 루프, 인트로 후 `BeginProgressTracking` 호출 |

## 흐름

```mermaid
flowchart LR
  MAP[LevelMap]
  CTX[LevelSessionContext]
  MM[MissionManager]
  HUD[MissionHUD]
  BM[BoardManager]
  SS[ScoreSystem]
  IGM[InGameManager]

  MAP -->|"BeginLevel"| CTX
  CTX -->|"BindFromSession"| MM
  IGM -->|"BeginProgressTracking"| MM
  BM -->|"CountIce/Grass"| MM
  IGM -->|"GemSlotSpawn"| MM
  SS -->|"OnScoreChanged"| MM
  MM -->|"OnProgressChanged"| HUD
```

## 진행도 규칙

| MissionType | 남은 목표 | 갱신 시점 |
|-------------|-----------|-----------|
| Ice | 보드 ice 셀 수 | 블록 배치 후 (스테이지 제거 연출 포함) |
| Grass | 보드 grass 셀 수 (전파 반영) | 동일 |
| Gem | 종류별 목표 개수 (`gemTargets`) | 줄 제거 시 수집 (슬롯 DraggableBlock에서 스폰) |
| ScoreGoal | `CurrentScore` / `TargetScore` (시간 제한 없음) | 점수 이벤트 |

인트로·레이아웃 적용이 끝난 뒤 `BeginProgressTracking()`을 호출해야 진행 추적이 시작된다.

## 클래스

```mermaid
classDiagram
  class MissionManager {
    +Instance MissionManager$
    +OnMissionBound Action$
    +OnProgressChanged Action$
    +OnScoreGoalProgressChanged Action~int,int~$
    +OnObjectiveCompleted Action$
    +int RemainingCollectCount
    +IReadOnlyList~GemTargetInfo~ RemainingGems
    +int TargetScore
    +int CurrentScore
    +BeginProgressTracking()
    +StopProgressTracking()
  }

  class MissionHUD {
    -TextMeshProUGUI _levelText
    -Transform _contentRoot
    -GameObject _currentScoreRoot
    -NumberDisplay _currentScoreDisplay
    -RebuildContent()
    -UpdateProgressTexts()
    -HandleScoreGoalProgressChanged(int,int)
    -SetCurrentScoreActive(bool)
  }

  MissionHUD --> MissionManager : subscribe events
  MissionHUD --> NumberDisplay : ScoreGoal current score
  MissionManager --> BoardManager : count cells
  MissionManager --> ScoreSystem : score / target
```

## 수집 비행 연출

미션 블록(Ice/Grass) 또는 슬롯 Gem 블록이 줄 제거로 사라지면 HUD 아이콘으로 날아간 뒤에만 Count가 줄고,
모든 비행이 끝난 뒤 목표가 0이면 ResultPopup이 열린다.

```mermaid
flowchart TD
  CLEAR[보드에서 미션 블록 제거]
  FLY[MissionCollectFlyEffect 비행]
  COUNT[HUD Count 감소]
  EVAL{남은 목표 0?}
  POPUP[ResultPopup]

  CLEAR --> FLY --> COUNT --> EVAL
  EVAL -->|Yes| POPUP
  EVAL -->|No| WAIT[계속 플레이]
```

### 인스펙터

**MissionManager**
- `_testMissionData` → 테스트용 MissionData (연결 시 레벨맵 없이 LevelInGame Play)
- `_testLevelNumber` → HUD/결과에 표시할 레벨 번호
- `_missionHud` → MissionHUD
- `_flyEffect` → MissionCollectFlyEffect
- `_resultPopup` → ResultPopupUI

**MissionHUD**
- `_root` → Mission 루트
- `_levelText` → 레벨 번호 텍스트
- `_contentRoot` → LayoutGroup (아이콘/목표 동적 스폰)
- `_currentScoreRoot` → Mission/Score (ScoreGoal일 때만 활성)
- `_currentScoreDisplay` → Score의 NumberDisplay (현재 점수 롤 표시)
- `_scoreRollDuration` → 점수 롤 애니메이션 시간(초)

**MissionCollectFlyEffect**
- `_flyRoot` → Canvas RectTransform
- `_flyIconPrefab` → IconImage
- `_flyDuration` → 비행 시간(초). **작을수록 빠름** (0.05~1.5)
- `_arcHeight` → 포물선 높이
- `_spinTurns` → 비행 중 회전 횟수 (0 = 회전 없음)
- `_endScale` → 도착 시 크기

| 결과 | ResultText | Level | 트리거 |
|------|------------|-------|--------|
| 성공 | SUCCESS | Level  N | 목표 달성 |
| 실패 | FAIL | Level  N | 배치 불가 게임오버 |

### 실패 연출 순서

실패 확정(`FailMission`)과 ResultPopup 표시(`ShowFailResultPopup`)를 분리한다.
팝업은 항상 보드 그레이스케일 연출 이후에 연다.

```mermaid
flowchart TD
  FAIL[미션 실패 조건]
  FAIL -->|배치 불가| DELAY[GameOverDelayCoroutine 대기]
  DELAY --> GRAY[ActivateGrayscale]
  GRAY --> POPUP[ShowFailResultPopup]
```

- Retry → 같은 레벨 `ResetGame`
  - 보드 레이아웃 복원 + `BeginProgressTracking` (Gem 수집량·비행 pending 초기화, 비행 연출 CancelAll)
  - `ScoreSystem.ResetScore` → `OnScoreChanged`로 ScoreGoal HUD 0 동기화
  - 슬롯 입력 재활성화 (`EnableInteraction(true)`)
- Next → `LevelSessionContext.BeginLevel(nextIndex)` 후 `LevelInGame` 씬 재로드
  - 성공이고 다음 `MissionData`가 있을 때만 버튼 표시
  - 마지막 레벨(다음 미션 없음)이면 Next 숨김, 클릭 시에도 Level 맵으로 폴백
- Quit → `ClearSession` 후 Level 맵 씬 이동
- 성공 시 다음 레벨 `MissionData.isClear = true` (플레이 가능 해금)

## 인스펙터 설정 (ResultPopup)

1. ResultPopup에 `ResultPopupUI` 추가
2. `_canvasGroup` → ResultPopup CanvasGroup
3. `_popupTransform` → Result
4. `_resultText` → ResultText, `_levelText` → Level
5. `_retryButton` / `_nextButton` / `_quitButton` 연결
