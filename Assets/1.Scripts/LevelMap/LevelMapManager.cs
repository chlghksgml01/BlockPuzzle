using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨 맵 스크롤을 소유하고 LevelMapModel(레이아웃 계산)과 LevelMapVirtualizer(재사용 배치)를 조립한다.
/// Content RectTransform은 pivot/anchor가 상단 기준이어야 하며, LevelMapRouteData의 노드 Y좌표는
/// 0(상단)에서 아래로 갈수록 음수가 되도록 배치되어 있어야 한다.
/// </summary>
public class LevelMapManager : MonoBehaviour
{
    [Header("Scroll References")]
    [Tooltip("레벨 맵을 스크롤하는 ScrollRect")]
    [SerializeField] private ScrollRect _scrollRect;

    [Tooltip("노드/경로 막대가 배치되는 콘텐츠 RectTransform (pivot/anchor: 상단 기준)")]
    [SerializeField] private RectTransform _content;

    [Header("Route Data & Prefabs")]
    [Tooltip("레벨 노드 배치 데이터")]
    [SerializeField] private LevelMapRouteData _routeData;

    [Tooltip("레벨 노드 프리팹")]
    [SerializeField] private LevelNodeView _nodePrefab;

    [Tooltip("경로 막대 프리팹")]
    [SerializeField] private LevelPathSegmentView _segmentPrefab;

    [Header("Layout Settings")]
    [Tooltip("경로 막대의 두께(px)")]
    [SerializeField, Min(1f)] private float _pathThickness = 24f;

    [Tooltip("콘텐츠 상하 여백(px)")]
    [SerializeField, Min(0f)] private float _contentPaddingY = 200f;

    [Header("Virtualization Settings")]
    [Tooltip("뷰포트 위아래로 미리 활성화해둘 여유 영역(px). 너무 작으면 빠른 스크롤 시 노드가 팝인하는 게 보일 수 있음")]
    [SerializeField, Min(0f)] private float _recycleBufferPx = 400f;

    [Tooltip("이 값(px) 이상 스크롤되어야 표시 목록을 다시 계산함 (매 프레임 풀 재계산 방지)")]
    [SerializeField, Min(0f)] private float _updateThresholdPx = 80f;

    /// <summary>레벨 노드가 클릭되었을 때 (1-based 레벨 번호)</summary>
    public event Action<int> OnLevelSelected;

    /// <summary>레벨 잠금 해제 여부 조회 훅. 저장 데이터 연동 전까지는 null이면 전부 해제 상태로 표시된다.</summary>
    public Func<int, bool> IsLevelUnlocked;

    private LevelMapModel _model;
    private LevelMapVirtualizer _virtualizer;
    private float _lastUpdateViewportTopY = float.NaN;

    private void Awake()
    {
        if (_scrollRect == null || _content == null || _routeData == null || _nodePrefab == null || _segmentPrefab == null)
        {
            Debug.LogError("LevelMapManager - Missing required references: ScrollRect, Content, RouteData, NodePrefab, SegmentPrefab");
            enabled = false;
            return;
        }

        _model = new LevelMapModel(_routeData, _pathThickness, _contentPaddingY);
        _content.sizeDelta = new Vector2(_content.sizeDelta.x, _model.ContentSize.y);

        _virtualizer = new LevelMapVirtualizer(_model, _content, _nodePrefab, _segmentPrefab,
            levelIndex => IsLevelUnlocked == null || IsLevelUnlocked(levelIndex),
            HandleLevelClicked);
    }

    private void OnEnable()
    {
        _scrollRect.onValueChanged.AddListener(HandleScrollChanged);
        RefreshVisible(true);
    }

    private void OnDisable()
    {
        _scrollRect.onValueChanged.RemoveListener(HandleScrollChanged);
    }

    private void HandleScrollChanged(Vector2 _)
    {
        RefreshVisible(false);
    }

    private void RefreshVisible(bool force)
    {
        float viewportTopY = -_content.anchoredPosition.y;

        if (!force && !float.IsNaN(_lastUpdateViewportTopY) && Mathf.Abs(viewportTopY - _lastUpdateViewportTopY) < _updateThresholdPx)
            return;

        _lastUpdateViewportTopY = viewportTopY;

        float viewportHeight = _scrollRect.viewport != null ? _scrollRect.viewport.rect.height : ((RectTransform)_scrollRect.transform).rect.height;
        float viewportBottomY = viewportTopY - viewportHeight;

        _virtualizer.UpdateVisible(viewportBottomY - _recycleBufferPx, viewportTopY + _recycleBufferPx);
    }

    private void HandleLevelClicked(int levelIndex)
    {
        OnLevelSelected?.Invoke(levelIndex);
    }
}
