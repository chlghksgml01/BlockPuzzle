# MissionBoardController 구조

미션 레이아웃·팔레트는 `MissionBoardController`가 담당하고,
보드 코어(배치/프리뷰/힌트/라인 클리어)는 `BoardManager`가 담당한다.

## 의존 관계

```mermaid
flowchart LR
  IGM[InGameManager]
  MBC[MissionBoardController]
  BM[BoardManager]
  MODEL[BoardModel]
  PAL[BlockSpritePalette]
  LAY[BoardLayoutData]

  IGM -->|"레벨 시작/리셋 ApplyMissionLayout"| MBC
  MBC --> PAL
  MBC -->|"mission BoardLayoutData"| LAY
  MBC -->|"PrepareSize / ApplyLayout"| BM
  MBC -->|"SetMissionSpriteResolver"| BM
  BM --> MODEL
  MODEL -->|"ice/grass 단계"| PAL
  BlockSlot --> BM
```

## ice / grass 단계 클리어

```mermaid
flowchart TD
  CLEAR[라인 클리어]
  DMG["데미지 = 속한 행 수 + 열 수<br/>동시 행·열이면 2"]
  STEP["한 단계씩 DOTween<br/>squash → 스프라이트 교체 → pop"]
  STAGE{stage + 데미지}
  UP[다음 단계 스프라이트]
  RM[스케일 0 후 셀 제거]

  CLEAR --> DMG --> STEP --> STAGE
  STAGE -->|"<= 3"| UP
  STAGE -->|"> 3"| RM
```

- ice01/grass01 + 줄 1개 → 02 (DoTween)
- 가로·세로 동시 → 데미지 +2, 단계를 한 칸씩 연출
- 03 + 추가 데미지 → 제거 연출

## 클래스

```mermaid
classDiagram
  class BoardManager {
    -Func~string,Sprite~ _missionSpriteResolver
    +SetMissionSpriteResolver(resolver)
    +PrepareBoardSizeFromLayout(layout)
    +ApplyBoardLayout(layout, spriteResolver)
  }

  class BoardModel {
    -Func~string,Sprite~ _spriteResolver
    +SetSpriteResolver(resolver)
    +ProcessFullLines()
    -ProcessClearedCell(x, y)
  }

  class BoardCell {
    +MaxStagedBlockStage$ int
    +IsIce
    +IsGrass
    +TryPlayStagedDamage(damage, resolver) bool
    +SetStageSprite(sprite)
    +TryGetStagedBlockInfo$(name, keyword, stage)$ bool
    +GetStagedSpriteName$(keyword, stage)$ string
  }

  class MissionBoardController {
    -BlockSpritePalette _missionSpritePalette
    -Dictionary~string,Sprite~ _spriteByName
    +PrepareFromSelectedMission()
    +ApplyMissionLayout()
    -ResolveSprite(spriteName) Sprite
  }

  class BlockSpritePalette {
    +Sprite[] sprites
  }

  MissionBoardController --> BoardManager : RequireComponent
  MissionBoardController --> BlockSpritePalette
  BoardManager --> BoardModel
  BoardModel --> BoardCell
```

## 실행 순서

1. `MissionBoardController` (`DefaultExecutionOrder -95`) Awake  
   - 팔레트 룩업 구축 후 `SetMissionSpriteResolver` 등록
   - 레벨 세션이면 `PrepareBoardSizeFromLayout`
2. `BoardManager` (`-90`) Awake  
   - `GenerateBoard` + 리졸버를 `BoardModel`에 전달
3. `InGameManager` Start  
   - 레벨이면 `ApplyMissionLayout` → 셀 채움/stone/ice/grass 적용

## 인스펙터 설정

1. `BoardManager`에 `MissionBoardController` 추가
2. **Mission Sprite Palette**에 `Assets/3.ScriptableObjects/Level/BlockSpritePalette` 연결
3. 팔레트 `Sprites`에 `ice01~03`, `grass01~03` 포함 (단계 전환·연출용)
