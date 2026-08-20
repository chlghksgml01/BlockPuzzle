using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 드래그 프리뷰 중 클리어될 행/열을 파티클 프리팹으로 감싸 표시한다.
/// </summary>
[DefaultExecutionOrder(-55)]
public class LineClearSparkleController : MonoBehaviour, IInitializable
{
    [Header("Prefab")]
    [Tooltip("ParticleSystem + LineClearSparkleSegment 가 붙은 프리팹")]
    [SerializeField] private LineClearSparkleSegment _segmentPrefab;

    [Header("Visual Settings")]
    [Tooltip("셀 바운드 바깥으로 파티클 박스를 확장할 월드 단위 패딩")]
    [SerializeField, Min(0f)] private float _padding = 0.08f;

    [Tooltip("풀 프리웜 개수")]
    [SerializeField, Min(0)] private int _prewarmCount = 4;

    private readonly List<LineClearSparkleSegment> _activeSegments = new List<LineClearSparkleSegment>();
    private readonly Stack<LineClearSparkleSegment> _pool = new Stack<LineClearSparkleSegment>();
    private readonly List<(int start, int end)> _groupsBuffer = new List<(int start, int end)>();
    private readonly List<int> _sortBuffer = new List<int>();
    private readonly Vector3[] _cornersBuffer = new Vector3[4];

    private IBoardQuery _boardQuery;
    private IBoardInfo _boardInfo;
    private Transform _worldEffectRoot;
    private Canvas _canvas;
    private bool _subscriptionsBound;
    private bool _prewarmed;

    public void Initialize(InitializeContext context)
    {
        _boardQuery = context.BoardManager;
        _boardInfo = context.BoardManager;
        TryBindSubscriptions();
    }

    public void Configure(
        IBoardQuery boardQuery,
        IBoardInfo boardInfo,
        Transform worldEffectRoot,
        LineClearSparkleSegment segmentPrefab,
        float padding)
    {
        _boardQuery = boardQuery;
        _boardInfo = boardInfo;
        _worldEffectRoot = worldEffectRoot != null ? worldEffectRoot : transform;
        _segmentPrefab = segmentPrefab;
        _padding = Mathf.Max(0f, padding);
        _canvas = _worldEffectRoot != null
            ? _worldEffectRoot.GetComponentInParent<Canvas>()
            : GetComponentInParent<Canvas>();

        _prewarmed = false;
        ClearPool();
        EnsurePrewarm();
        TryBindSubscriptions();
    }

    private void Awake()
    {
        if (_worldEffectRoot == null)
            _worldEffectRoot = transform;

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        EnsurePrewarm();
    }

    private void OnEnable()
    {
        TryBindSubscriptions();
    }

    private void OnDisable()
    {
        if (!_subscriptionsBound)
            return;

        if (_boardQuery != null)
            _boardQuery.OnLineClearPreviewChanged -= HandleLineClearPreviewChanged;

        _subscriptionsBound = false;
        ClearActive();
    }

    private void TryBindSubscriptions()
    {
        if (_subscriptionsBound || _boardQuery == null || !isActiveAndEnabled)
            return;

        _boardQuery.OnLineClearPreviewChanged += HandleLineClearPreviewChanged;
        _subscriptionsBound = true;
    }

    private void HandleLineClearPreviewChanged(IReadOnlyList<int> rows, IReadOnlyList<int> cols)
    {
        ClearActive();

        if (rows != null && rows.Count > 0)
        {
            BuildConsecutiveGroups(rows);
            for (int i = 0; i < _groupsBuffer.Count; i++)
                ShowRowGroup(_groupsBuffer[i].start, _groupsBuffer[i].end);
        }

        if (cols != null && cols.Count > 0)
        {
            BuildConsecutiveGroups(cols);
            for (int i = 0; i < _groupsBuffer.Count; i++)
                ShowColGroup(_groupsBuffer[i].start, _groupsBuffer[i].end);
        }
    }

    private void ShowRowGroup(int startY, int endY)
    {
        if (!TryGetRowWorldBounds(startY, endY, out Vector3 worldMin, out Vector3 worldMax))
            return;

        LineClearSparkleSegment segment = RentSegment();
        if (segment == null)
            return;

        segment.Show(worldMin, worldMax);
        _activeSegments.Add(segment);
    }

    private void ShowColGroup(int startX, int endX)
    {
        if (!TryGetColWorldBounds(startX, endX, out Vector3 worldMin, out Vector3 worldMax))
            return;

        LineClearSparkleSegment segment = RentSegment();
        if (segment == null)
            return;

        segment.Show(worldMin, worldMax);
        _activeSegments.Add(segment);
    }

    private bool TryGetRowWorldBounds(int startY, int endY, out Vector3 worldMin, out Vector3 worldMax)
    {
        worldMin = default;
        worldMax = default;

        if (_boardQuery == null || _boardInfo == null)
            return false;

        int width = _boardInfo.Width;
        if (!_boardQuery.TryGetCellWorldCorners(0, startY, _cornersBuffer))
            return false;

        EncapsulateCorners(ref worldMin, ref worldMax, _cornersBuffer, isFirst: true);

        if (!_boardQuery.TryGetCellWorldCorners(width - 1, startY, _cornersBuffer))
            return false;
        EncapsulateCorners(ref worldMin, ref worldMax, _cornersBuffer, isFirst: false);

        if (endY != startY)
        {
            if (!_boardQuery.TryGetCellWorldCorners(0, endY, _cornersBuffer))
                return false;
            EncapsulateCorners(ref worldMin, ref worldMax, _cornersBuffer, isFirst: false);

            if (!_boardQuery.TryGetCellWorldCorners(width - 1, endY, _cornersBuffer))
                return false;
            EncapsulateCorners(ref worldMin, ref worldMax, _cornersBuffer, isFirst: false);
        }

        return true;
    }

    private bool TryGetColWorldBounds(int startX, int endX, out Vector3 worldMin, out Vector3 worldMax)
    {
        worldMin = default;
        worldMax = default;

        if (_boardQuery == null || _boardInfo == null)
            return false;

        int height = _boardInfo.Height;
        if (!_boardQuery.TryGetCellWorldCorners(startX, 0, _cornersBuffer))
            return false;

        EncapsulateCorners(ref worldMin, ref worldMax, _cornersBuffer, isFirst: true);

        if (!_boardQuery.TryGetCellWorldCorners(startX, height - 1, _cornersBuffer))
            return false;
        EncapsulateCorners(ref worldMin, ref worldMax, _cornersBuffer, isFirst: false);

        if (endX != startX)
        {
            if (!_boardQuery.TryGetCellWorldCorners(endX, 0, _cornersBuffer))
                return false;
            EncapsulateCorners(ref worldMin, ref worldMax, _cornersBuffer, isFirst: false);

            if (!_boardQuery.TryGetCellWorldCorners(endX, height - 1, _cornersBuffer))
                return false;
            EncapsulateCorners(ref worldMin, ref worldMax, _cornersBuffer, isFirst: false);
        }

        return true;
    }

    private static void EncapsulateCorners(ref Vector3 worldMin, ref Vector3 worldMax, Vector3[] corners, bool isFirst)
    {
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = corners[i];
            if (isFirst && i == 0)
            {
                worldMin = p;
                worldMax = p;
                continue;
            }

            worldMin = Vector3.Min(worldMin, p);
            worldMax = Vector3.Max(worldMax, p);
        }
    }

    private void BuildConsecutiveGroups(IReadOnlyList<int> indices)
    {
        _groupsBuffer.Clear();
        _sortBuffer.Clear();

        for (int i = 0; i < indices.Count; i++)
            _sortBuffer.Add(indices[i]);

        _sortBuffer.Sort();

        if (_sortBuffer.Count == 0)
            return;

        int start = _sortBuffer[0];
        int end = _sortBuffer[0];
        for (int i = 1; i < _sortBuffer.Count; i++)
        {
            int value = _sortBuffer[i];
            if (value == end + 1)
            {
                end = value;
                continue;
            }

            _groupsBuffer.Add((start, end));
            start = value;
            end = value;
        }

        _groupsBuffer.Add((start, end));
    }

    private LineClearSparkleSegment RentSegment()
    {
        if (_pool.Count > 0)
            return _pool.Pop();

        return CreateSegment();
    }

    private void ClearActive()
    {
        for (int i = 0; i < _activeSegments.Count; i++)
        {
            LineClearSparkleSegment segment = _activeSegments[i];
            if (segment == null)
                continue;

            segment.Hide();
            _pool.Push(segment);
        }

        _activeSegments.Clear();
    }

    private void ClearPool()
    {
        ClearActive();
        while (_pool.Count > 0)
        {
            LineClearSparkleSegment segment = _pool.Pop();
            if (segment != null)
                Destroy(segment.gameObject);
        }
    }

    private void EnsurePrewarm()
    {
        if (_prewarmed || _worldEffectRoot == null || _segmentPrefab == null)
            return;

        for (int i = 0; i < _prewarmCount; i++)
        {
            LineClearSparkleSegment segment = CreateSegment();
            if (segment == null)
                break;

            segment.Hide();
            _pool.Push(segment);
        }

        _prewarmed = true;
    }

    private LineClearSparkleSegment CreateSegment()
    {
        if (_segmentPrefab == null)
        {
            Debug.LogWarning("LineClearSparkleController: Segment Prefab이 비어 있습니다.", this);
            return null;
        }

        LineClearSparkleSegment segment = Instantiate(_segmentPrefab, _worldEffectRoot);
        segment.name = "LineClearSparkleSegment";
        segment.Bind(_worldEffectRoot, _canvas, _padding);
        return segment;
    }
}
