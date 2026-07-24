using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 뷰포트에 보이는 범위에 맞춰 LevelNodeView / LevelRoadView를 풀링으로 생성·회수하는 가상 스크롤 엔진.
/// Content는 하단(pivot 0,0) 기준으로 위로 자라나며, 노드 0번이 맨 아래에 위치한다.
/// </summary>
public sealed class LevelMapVirtualizer
{
    private readonly RectTransform _content;
    private readonly RectTransform _viewport;
    private readonly LevelMapLayout _layout;
    private readonly float _viewportPadding;
    private readonly float _contentGrowthChunk;
    private readonly float _topPadding;
    private readonly int _totalLevelCount;

    private readonly LevelMissionTableData _missionTable;

    private readonly ObjectPool<LevelNodeView> _nodePool;
    private readonly ObjectPool<LevelRoadView> _roadPool;
    private readonly ObjectPool<LevelRoadView> _clearRoadPool;
    private readonly Dictionary<int, LevelNodeView> _activeNodes = new Dictionary<int, LevelNodeView>();
    private readonly Dictionary<int, LevelRoadView> _activeRoads = new Dictionary<int, LevelRoadView>();
    private readonly Dictionary<int, LevelRoadView> _activeClearRoads = new Dictionary<int, LevelRoadView>();
    private readonly List<int> _releaseBuffer = new List<int>();

    private readonly Vector3[] _cornerBuffer = new Vector3[4];

    public LevelMapVirtualizer(
        RectTransform content,
        RectTransform viewport,
        LevelNodeView nodePrefab,
        LevelRoadView roadPrefab,
        LevelRoadView clearRoadPrefab,
        Transform nodeContainer,
        Transform roadContainer,
        Transform clearRoadContainer,
        LevelMapLayout layout,
        float viewportPadding,
        float contentGrowthChunk,
        float topPadding,
        int totalLevelCount,
        LevelMissionTableData missionTable,
        System.Action<int> onNodeClicked)
    {
        _content = content;
        _viewport = viewport;
        _layout = layout;
        _viewportPadding = viewportPadding;
        _contentGrowthChunk = contentGrowthChunk;
        _topPadding = topPadding;
        _totalLevelCount = totalLevelCount;
        _missionTable = missionTable;

        _nodePool = new ObjectPool<LevelNodeView>(
            createFunc: () =>
            {
                LevelNodeView view = Object.Instantiate(nodePrefab, nodeContainer);
                view.OnClicked += onNodeClicked;
                return view;
            },
            actionOnGet: view => view.gameObject.SetActive(true),
            actionOnRelease: view => view.gameObject.SetActive(false),
            actionOnDestroy: view => Object.Destroy(view.gameObject),
            collectionCheck: false,
            defaultCapacity: 12,
            maxSize: 64);

        _roadPool = new ObjectPool<LevelRoadView>(
            createFunc: () => Object.Instantiate(roadPrefab, roadContainer),
            actionOnGet: view => view.gameObject.SetActive(true),
            actionOnRelease: view => view.gameObject.SetActive(false),
            actionOnDestroy: view => Object.Destroy(view.gameObject),
            collectionCheck: false,
            defaultCapacity: 6,
            maxSize: 32);

        if (clearRoadPrefab != null && clearRoadContainer != null)
        {
            _clearRoadPool = new ObjectPool<LevelRoadView>(
                createFunc: () => Object.Instantiate(clearRoadPrefab, clearRoadContainer),
                actionOnGet: view => view.gameObject.SetActive(true),
                actionOnRelease: view => view.gameObject.SetActive(false),
                actionOnDestroy: view => Object.Destroy(view.gameObject),
                collectionCheck: false,
                defaultCapacity: 6,
                maxSize: 32);
        }

        if (_totalLevelCount > 0)
        {
            float finalTopY = _layout.GetNodePosition(_totalLevelCount - 1).y + _topPadding;
            SetContentHeight(finalTopY);
        }
    }

    public void Refresh()
    {
        GrowContentIfNeeded();

        (float minY, float maxY) = GetVisibleContentYRange();

        int minNodeIndex = Mathf.Max(0, _layout.GetNodeIndexNearY(minY) - 2);
        int maxNodeIndex = _layout.GetNodeIndexNearY(maxY) + 2;
        if (_totalLevelCount > 0)
            maxNodeIndex = Mathf.Min(maxNodeIndex, _totalLevelCount - 1);

        int minPairIndex = Mathf.Max(0, minNodeIndex / 2 - 1);
        int maxPairIndex = Mathf.Max(minPairIndex, maxNodeIndex / 2);

        int lastRoadPairIndex = _totalLevelCount > 0 ? (_totalLevelCount - 1) / 2 : -1;
        bool lastRoadIsHalfFilled = _totalLevelCount > 0 && _totalLevelCount % 2 == 0;

        SyncActive(_activeNodes, _nodePool, minNodeIndex, maxNodeIndex,
            (index, view) =>
            {
                MissionData mission = _missionTable != null
                    ? _missionTable.GetMission(index)
                    : null;
                view.Bind(index, _layout.GetNodePosition(index), mission);
            });

        SyncActive(_activeRoads, _roadPool, minPairIndex, maxPairIndex,
            (index, view) =>
            {
                Vector2 position = _layout.GetRoadPosition(index, out bool mirrored);
                bool forceHalfFill = lastRoadIsHalfFilled && index == lastRoadPairIndex;
                view.Bind(index, position, mirrored, forceHalfFill);
            });

        RefreshClearRoads(minPairIndex, maxPairIndex);
    }

    private void RefreshClearRoads(int minPairIndex, int maxPairIndex)
    {
        if (_clearRoadPool == null)
            return;

        int lastCompletedLevelIndex = _missionTable != null
            ? _missionTable.GetLastCompletedLevelIndex()
            : -1;

        if (lastCompletedLevelIndex < 0)
        {
            SyncActive(_activeClearRoads, _clearRoadPool, 0, -1, (index, view) => BindClearRoad(index, view));
            return;
        }

        int maxClearPairIndex = lastCompletedLevelIndex / 2;
        int minClearPairIndex = Mathf.Max(minPairIndex, 0);
        int maxClearPairIndexInView = Mathf.Min(maxPairIndex, maxClearPairIndex);

        SyncActive(_activeClearRoads, _clearRoadPool, minClearPairIndex, maxClearPairIndexInView,
            (index, view) => BindClearRoad(index, view, lastCompletedLevelIndex));
    }

    private void BindClearRoad(int roadPairIndex, LevelRoadView view, int lastCompletedLevelIndex = -1)
    {
        Vector2 position = _layout.GetRoadPosition(roadPairIndex, out bool mirrored);
        bool forceHalfFill = lastCompletedLevelIndex >= 0
            && roadPairIndex == lastCompletedLevelIndex / 2
            && lastCompletedLevelIndex % 2 == 0;
        view.Bind(roadPairIndex, position, mirrored, forceHalfFill);
    }

    private void SyncActive<T>(Dictionary<int, T> active, ObjectPool<T> pool, int minIndex, int maxIndex, System.Action<int, T> bind)
        where T : Component
    {
        _releaseBuffer.Clear();
        foreach (KeyValuePair<int, T> kvp in active)
        {
            if (kvp.Key < minIndex || kvp.Key > maxIndex)
                _releaseBuffer.Add(kvp.Key);
        }

        for (int i = 0; i < _releaseBuffer.Count; i++)
        {
            int index = _releaseBuffer[i];
            pool.Release(active[index]);
            active.Remove(index);
        }

        for (int i = minIndex; i <= maxIndex; i++)
        {
            if (!active.TryGetValue(i, out T view))
            {
                view = pool.Get();
                active[i] = view;
            }

            bind(i, view);
        }
    }

    private (float minY, float maxY) GetVisibleContentYRange()
    {
        _viewport.GetWorldCorners(_cornerBuffer);

        Vector2 bottomLocal = _content.InverseTransformPoint(_cornerBuffer[0]);
        Vector2 topLocal = _content.InverseTransformPoint(_cornerBuffer[2]);

        float minY = Mathf.Min(bottomLocal.y, topLocal.y) - _viewportPadding;
        float maxY = Mathf.Max(bottomLocal.y, topLocal.y) + _viewportPadding;
        return (minY, maxY);
    }

    private void GrowContentIfNeeded()
    {
        if (_totalLevelCount > 0)
            return;

        _viewport.GetWorldCorners(_cornerBuffer);
        Vector2 topLocal = _content.InverseTransformPoint(_cornerBuffer[2]);

        if (topLocal.y > _content.sizeDelta.y - _contentGrowthChunk)
            SetContentHeight(_content.sizeDelta.y + _contentGrowthChunk);
    }

    private void SetContentHeight(float height)
    {
        _content.sizeDelta = new Vector2(_content.sizeDelta.x, height);
    }
}
