# LevelMap 가상 스크롤 구조

Level 씬의 ScrollView(Content > LoadPreview)에서 손으로 배치했던 Road/LevelNode 반복 패턴을
동적으로 생성하도록 가상화한 구조. Content는 하단(pivot 0,0) 기준으로 위로 자라나며,
스크롤을 위로 올릴수록 상위 레벨이 드러난다.

## 클래스 다이어그램

```mermaid
classDiagram
    class LevelMapPatternData {
        -float _connectedNodeGap
        -float _directNodeGap
        -float _horizontalAmplitude
        -float _roadHorizontalOffset
        -float _roadVerticalOffsetFromLowerNode
        -float _originYOffset
        +float ConnectedNodeGap
        +float DirectNodeGap
        +float HorizontalAmplitude
        +float RoadHorizontalOffset
        +float RoadVerticalOffsetFromLowerNode
        +float OriginYOffset
        +float PairCycleHeight
    }

    class LevelMapLayout {
        -LevelMapPatternData _pattern
        +LevelMapLayout(pattern)
        +Vector2 GetNodePosition(nodeIndex)
        +Vector2 GetRoadPosition(roadPairIndex, out mirrored)
        +int GetNodeIndexNearY(y)
    }

    class LevelMapVirtualizer {
        -RectTransform _content
        -RectTransform _viewport
        -LevelMapLayout _layout
        -ObjectPool~LevelNodeView~ _nodePool
        -ObjectPool~LevelRoadView~ _roadPool
        -Dictionary~int,LevelNodeView~ _activeNodes
        -Dictionary~int,LevelRoadView~ _activeRoads
        +Refresh()
        -SyncActive~T~(active, pool, minIndex, maxIndex, bind)
        -GetVisibleContentYRange() (float,float)
        -GrowContentIfNeeded()
    }

    class LevelMapManager {
        -ScrollRect _scrollRect
        -RectTransform _viewport
        -RectTransform _content
        -RectTransform _nodeContainer
        -RectTransform _roadContainer
        -MissionPopupUI _missionPopup
        -LevelNodeView _nodePrefab
        -LevelRoadView _roadPrefab
        -LevelMapPatternData _patternData
        -LevelMissionTableData _missionTable
        -int _totalLevelCount
        -LevelMapLayout _layout
        -LevelMapVirtualizer _virtualizer
        +Awake()
        +OnEnable()
        +OnDisable()
        -OnScrollChanged(Vector2)
        +OpenMissionPopup(levelIndex)
    }

    class LevelNodeView {
        -TMP_Text _levelText
        -Button _nodeButton
        +RectTransform RectTransform
        +int NodeIndex
        +event Action~int~ OnClicked
        +Bind(nodeIndex, anchoredPosition)
    }

    class LevelRoadView {
        +RectTransform RectTransform
        +int RoadPairIndex
        +Bind(roadPairIndex, anchoredPosition, mirrored)
    }

    LevelMapManager --> LevelMapLayout : creates
    LevelMapManager --> LevelMapVirtualizer : creates
    LevelMapManager --> LevelMapPatternData : reads
    LevelMapManager --> LevelMissionTableData : reads
    LevelMapManager --> MissionPopupUI : opens
    LevelMapVirtualizer --> LevelMapLayout : uses
    LevelMapVirtualizer --> LevelNodeView : pools
    LevelMapVirtualizer --> LevelRoadView : pools
    LevelMapVirtualizer ..> LevelNodeView : subscribes OnClicked
    LevelMapLayout --> LevelMapPatternData : reads
```

노드 클릭 시 `LevelNodeView.OnClicked(nodeIndex)` 이벤트가 발생하고, `LevelMapVirtualizer`가 노드 생성 시점(풀링되므로 1회만)에 이를 `LevelMapManager.OpenMissionPopup`으로 구독시켜 전달한다. `LevelNodeView`는 `LevelMapManager`를 직접 참조하지 않는다(Action 기반 설계).

## 레벨 클리어 미션 데이터

레벨마다 클리어 조건이 다르므로, 미션 타입별로 데이터 형태가 다른 부분만 하위 클래스로 확장한다(OCP).
ICE/GRASS 제거는 "대상 블록 종류 + 목표 개수"로 `CollectBlockGoalMissionData` 하나로 표현하고,
보석 수집은 보석 종류별 목표 개수가 여러 개일 수 있어 `CollectGemMissionData`로 분리했다.
점수+시간 조합인 목표 점수 미션은 `ScoreGoalMissionData`로 별도 분리했다.

```mermaid
classDiagram
    class LevelMissionData {
        <<abstract>>
        -string _missionDescription
        -bool _isHard
        +string MissionDescription
        +bool IsHard
    }

    class ScoreGoalMissionData {
        -int _targetScore
        -float _timeLimitSeconds
        +int TargetScore
        +float TimeLimitSeconds
    }

    class CollectBlockGoalMissionData {
        -BlockType _targetBlockType
        -int _targetCount
        +BlockType TargetBlockType
        +int TargetCount
    }

    class CollectGemMissionData {
        -List~GemTargetInfo~ _gemTargets
        +IReadOnlyList~GemTargetInfo~ GemTargets
    }

    class BlockType {
        <<enumeration>>
        Ice
        Grass
    }

    class GemType {
        <<enumeration>>
        Pentagon
        Square
        Star
    }

    class GemTargetInfo {
        +GemType gemType
        +int count
    }

    class LevelMissionTableData {
        -LevelMissionData[] _missions
        +int LevelCount
        +GetMission~T~(levelIndex) T
    }

    class MissionPopupUI {
        -TextMeshProUGUI _levelText
        -GameObject _skull
        -GameObject _scoreGoalMission
        -GameObject _iceGrassMission
        -GameObject _collectGemMission
        +Open(levelIndex, missionData)
    }

    LevelMissionData <|-- ScoreGoalMissionData
    LevelMissionData <|-- CollectBlockGoalMissionData
    LevelMissionData <|-- CollectGemMissionData
    CollectBlockGoalMissionData --> BlockType : uses
    CollectGemMissionData --> GemTargetInfo : uses
    GemTargetInfo --> GemType : uses
    LevelMissionTableData --> LevelMissionData : holds
    MissionPopupUI --> LevelMissionData : reads (switch by type)
```

> `MissionPopupUI.Open`은 미션 데이터 타입에 따라 점수 목표/블록 수집/보석 수집 UI 그룹 중
> 하나만 활성화하고 내용을 채운다. 다만 실제 인게임 보드에서 ICE/GRASS/보석 블록 타입과
> 클리어 판정 로직(예: `MissionEvaluator`)은 아직 구현되어 있지 않다. 해당 블록 시스템을
> 추가할 때 이 미션 데이터를 참조해 클리어 여부를 판정하도록 연동해야 한다.

## 배치 규칙 (기존 씬 실측값 기반)

- 노드 인덱스 `n`의 X좌표: `amplitude * [0, +1, 0, -1][n % 4]` (4개 주기 지그재그)
- 노드 인덱스 `n`의 Y좌표: 2개 노드 + Road 1개가 한 주기(`PairCycleHeight = connectedGap + directGap`)
  - 짝수→홀수: `connectedGap`(180.5) 간격, 그 사이에 Road 배치
  - 홀수→다음 짝수: `directGap`(190.5) 간격, Road 없이 직결
- Road는 자신이 연결하는 두 노드 중 아래쪽(짝수 인덱스) 노드보다 `roadVerticalOffsetFromLowerNode`(178.5)만큼 위,
  가로로는 `roadHorizontalOffset`(217)만큼 좌우로 벌어진 위치에 놓이고, 좌회전 구간에서는 동일 스프라이트를
  Y축 180도 회전시켜 재사용한다.

## 가상 스크롤 동작

1. `LevelMapManager.Awake()`가 `LevelMapLayout`과 `LevelMapVirtualizer`를 구성하고, ScrollRect를 하단(0)으로 초기화한다.
   생성되는 LevelNode는 `_nodeContainer`(Content 하위 "LevelNode" 트랜스폼) 자식으로, Road는 `_roadContainer`
   ("Road" 트랜스폼) 자식으로 각각 나뉘어 들어간다.
2. `ScrollRect.onValueChanged` 이벤트가 발생할 때만 `LevelMapVirtualizer.Refresh()`가 호출된다(Update 폴링 없음).
3. `Refresh()`는 Viewport의 월드 코너를 Content 로컬 좌표로 변환해 현재 보이는 Y 범위를 구하고,
   그 범위(+여유값) 밖의 노드/도로는 `ObjectPool`로 반환, 범위 안에 없는 것은 새로 스폰한다.
4. 전체 레벨 수가 정해지지 않은 경우(`_totalLevelCount <= 0`), 스크롤이 상단 여유값에 가까워질 때마다
   Content의 `sizeDelta.y`를 `_contentGrowthChunk`만큼 늘려 계속 위로 스크롤할 수 있게 한다.
