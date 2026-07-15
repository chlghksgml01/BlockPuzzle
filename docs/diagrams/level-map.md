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
        -LevelNodeView _nodePrefab
        -LevelRoadView _roadPrefab
        -LevelMapPatternData _patternData
        -int _totalLevelCount
        -LevelMapLayout _layout
        -LevelMapVirtualizer _virtualizer
        +Awake()
        +OnEnable()
        +OnDisable()
        -OnScrollChanged(Vector2)
    }

    class LevelNodeView {
        +RectTransform RectTransform
        +int NodeIndex
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
    LevelMapVirtualizer --> LevelMapLayout : uses
    LevelMapVirtualizer --> LevelNodeView : pools
    LevelMapVirtualizer --> LevelRoadView : pools
    LevelMapLayout --> LevelMapPatternData : reads
```

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
