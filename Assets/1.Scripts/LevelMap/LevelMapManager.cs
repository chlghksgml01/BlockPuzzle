using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨맵 ScrollView를 소유하고 LevelMapLayout/LevelMapVirtualizer를 연결하는 진입점.
/// 스크롤이 변경될 때마다(이벤트 기반, Update 폴링 없음) 보이는 범위의 노드/도로만 생성·회수한다.
/// </summary>
public class LevelMapManager : MonoBehaviour
{
    [Header("Scroll References")]
    [Tooltip("레벨맵 스크롤을 담당하는 ScrollRect")]
    [SerializeField] private ScrollRect _scrollRect;

    [Tooltip("스크롤 뷰의 보이는 영역(Viewport)")]
    [SerializeField] private RectTransform _viewport;

    [Tooltip("노드/도로가 배치되는 Content. Pivot(0.5, 0) / AnchorMin,Max(0~1, 0)으로 하단 고정되어 있어야 함")]
    [SerializeField] private RectTransform _content;


    [Header("Containers")]
    [Tooltip("생성된 LevelNode들이 자식으로 들어갈 Content 하위 트랜스폼")]
    [SerializeField] private RectTransform _nodeContainer;

    [Tooltip("생성된 Road들이 자식으로 들어갈 Content 하위 트랜스폼")]
    [SerializeField] private RectTransform _roadContainer;

    [Tooltip("클리어 구간 Road들이 자식으로 들어갈 Content 하위 트랜스폼")]
    [SerializeField] private RectTransform _clearRoadContainer;


    [Header("Mission Popup")]
    [SerializeField] private MissionPopupUI _missionPopup;

    [Tooltip("노드 선택 시 현재 레벨을 갱신할 UI 컨트롤러")]
    [SerializeField] private LevelUIButtonController _levelUIButtonController;


    [Header("Prefabs")]
    [Tooltip("레벨 노드 프리팹 (LevelNodeView 포함)")]
    [SerializeField] private LevelNodeView _nodePrefab;

    [Tooltip("노드 사이를 잇는 Road 프리팹 (LevelRoadView 포함)")]
    [SerializeField] private LevelRoadView _roadPrefab;

    [Tooltip("클리어한 구간에 표시할 Road 프리팹 (LevelRoadView 포함)")]
    [SerializeField] private LevelRoadView _clearRoadPrefab;


    [Header("Layout Data")]
    [Tooltip("노드/도로 간격을 정의하는 패턴 데이터 에셋")]
    [SerializeField] private LevelMapPatternData _patternData;

    [Tooltip("레벨별 클리어 미션 테이블. 배열 길이는 총 레벨 수와 일치해야 함")]
    [SerializeField] private LevelMissionTableData _missionTable;


    [Header("Virtualization Settings")]
    [Tooltip("전체 레벨 수. 0 이하로 두면 스크롤이 끝에 가까워질 때마다 Content가 절차적으로 늘어남")]
    [SerializeField] private int _totalLevelCount = 0;

    [Tooltip("뷰포트 밖으로 미리 스폰해 둘 여유 영역 (px)")]
    [SerializeField] private float _viewportPadding = 300f;

    [Tooltip("무제한 모드에서 Content 높이를 한 번에 늘리는 단위 (px)")]
    [SerializeField] private float _contentGrowthChunk = 2000f;

    [Tooltip("마지막 노드 위쪽으로 남겨둘 여백 (px). 전체 레벨 수가 정해져 있을 때만 사용")]
    [SerializeField] private float _topPadding = 300f;

    private LevelMapLayout _layout;
    private LevelMapVirtualizer _virtualizer;

    private void OnValidate()
    {
        if (_missionTable != null && _totalLevelCount > 0 && _missionTable.LevelCount != _totalLevelCount)
        {
            Debug.LogWarning(
                $"[LevelMapManager] MissionTable 길이({_missionTable.LevelCount})가 총 레벨 수({_totalLevelCount})와 다릅니다.",
                this);
        }
    }

    private void Awake()
    {
        _layout = new LevelMapLayout(_patternData);
        _virtualizer = new LevelMapVirtualizer(
            _content,
            _viewport,
            _nodePrefab,
            _roadPrefab,
            _clearRoadPrefab,
            _nodeContainer,
            _roadContainer,
            _clearRoadContainer,
            _layout,
            _viewportPadding,
            _contentGrowthChunk,
            _topPadding,
            _totalLevelCount,
            _missionTable,
            OpenMissionPopup);

        _scrollRect.verticalNormalizedPosition = 0f;
    }

    private void OnEnable()
    {
        _scrollRect.onValueChanged.AddListener(OnScrollChanged);
        _virtualizer.Refresh();
    }

    private void OnDisable()
    {
        _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
    }

    private void OnScrollChanged(Vector2 _)
    {
        _virtualizer.Refresh();
    }

    public void OpenMissionPopup(int levelIndex)
    {
        LevelMissionData mission = _missionTable.GetMission<LevelMissionData>(levelIndex);
        if (mission == null)
            return;

        if (_levelUIButtonController != null)
            _levelUIButtonController.SelectLevel(levelIndex);

        _missionPopup.Open(levelIndex, mission);
        SoundManager.Instance.PlayUISFX();
    }
}
