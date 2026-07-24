# MissionBoardController 구조

미션 레이아웃·팔레트는 `MissionBoardController`가 담당하고,
보드 코어(배치/프리뷰/힌트/라인 클리어)는 `BoardManager`가 담당한다.
grass 전파는 `GrassSpreadController`가 담당하며 **MissionType.Grass일 때만** 동작한다.

## 미션 종류

| MissionType | SO |
|-------------|-----|
| ScoreGoal | `ScoreGoalMissionData` |
| Ice | `CollectBlockGoalMissionData` (Target=Ice) |
| Grass | `CollectBlockGoalMissionData` (Target=Grass) |
| Gem | `CollectGemMissionData` |
| None | 레벨 세션 아님 |

한 미션에 Ice/Grass가 섞이지 않는다.

## 의존 관계

```mermaid
flowchart LR
  IGM[InGameManager]
  MBC[MissionBoardController]
  GSC[GrassSpreadController]
  BM[BoardManager]
  MODEL[BoardModel]
  DATA[LevelMissionData]
  PAL[BlockSpritePalette]

  IGM -->|"OnBlockSettled"| BM
  IGM -->|"OnBlockSettled"| GSC
  DATA -->|"MissionType"| MBC
  MBC -->|"CurrentMissionType"| GSC
  MBC --> PAL
  MBC -->|"ApplyLayout"| BM
  GSC -->|"Grass만 TrySpreadGrass"| BM
  BM --> MODEL
```

## grass 전파

```mermaid
flowchart TD
  SETTLE[블록 배치 완료]
  TYPE{CurrentMissionType<br/>== Grass?}
  HAS{보드에 grass 있음?}
  HIT{이번 클리어에<br/>grass 줄 포함?}
  MISS[미스 카운트 +1]
  RESET[미스 카운트 0]
  TH{미스 >= 3?}
  SPREAD[인접 비미션 칸 → grass01]

  SETTLE --> TYPE
  TYPE -->|No| STOP[무시]
  TYPE -->|Yes| HAS
  HAS -->|No| RESET
  HAS -->|Yes| HIT
  HIT -->|Yes| RESET
  HIT -->|No| MISS --> TH
  TH -->|Yes| SPREAD
```

## 클래스

```mermaid
classDiagram
  class MissionType {
    <<enum>>
    None
    ScoreGoal
    Ice
    Grass
    Gem
  }

  class LevelMissionData {
    <<abstract>>
    +MissionType MissionType*
    +BoardLayoutData BoardLayoutData
  }

  class ScoreGoalMissionData {
    +MissionType MissionType => ScoreGoal
  }

  class CollectBlockGoalMissionData {
    -BlockType _targetBlockType
    +MissionType MissionType => Ice or Grass
  }

  class CollectGemMissionData {
    +MissionType MissionType => Gem
  }

  class MissionBoardController {
    +MissionType CurrentMissionType
    +LevelMissionData CurrentMission
    -CacheCurrentMission()
    +ApplyMissionLayout()
  }

  class GrassSpreadController {
    -int _missedClearsBeforeSpread
    +ResetMissCounter()
    -HandleBlockSettled()
  }

  LevelMissionData <|-- ScoreGoalMissionData
  LevelMissionData <|-- CollectBlockGoalMissionData
  LevelMissionData <|-- CollectGemMissionData
  LevelMissionData --> MissionType
  MissionBoardController --> LevelMissionData
  GrassSpreadController --> MissionBoardController : RequireComponent
  GrassSpreadController --> BoardManager : RequireComponent
```

## 인스펙터 설정

1. BoardManager에 `MissionBoardController` + `GrassSpreadController` 추가
2. Collect Block 미션 SO의 **Target Block Type**으로 Ice/Grass 구분 (MissionType은 자동)
3. 팔레트에 `grass01~03` 포함 (Grass 미션 전파용)
