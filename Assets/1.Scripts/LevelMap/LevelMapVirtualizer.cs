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

    private readonly ObjectPool<LevelNodeView> _nodePool;
    private readonly ObjectPool<LevelRoadView> _roadPool;
    private readonly Dictionary<int, LevelNodeView> _activeNodes = new Dictionary<int, LevelNodeView>();
    private readonly Dictionary<int, LevelRoadView> _activeRoads = new Dictionary<int, LevelRoadView>();
    private readonly List<int> _releaseBuffer = new List<int>();

    private readonly Vector3[] _cornerBuffer = new Vector3[4];

    public LevelMapVirtualizer(
        RectTransform content,
        RectTransform viewport,
        LevelNodeView nodePrefab,
        LevelRoadView roadPrefab,
        Transform nodeContainer,
        Transform roadContainer,
        LevelMapLayout layout,
        float viewportPadding,
        float contentGrowthChunk,
        float topPadding,
        int totalLevelCount,
        System.Action<int> onNodeClicked)
    {
        _content = content;
        _viewport = viewport;
        _layout = layout;
        _viewportPadding = viewportPadding;
        _contentGrowthChunk = contentGrowthChunk;
        _topPadding = topPadding;
        _totalLevelCount = totalLevelCount;

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
            (index, view) => view.Bind(index, _layout.GetNodePosition(index)));

        SyncActive(_activeRoads, _roadPool, minPairIndex, maxPairIndex,
            (index, view) =>
            {
                Vector2 position = _layout.GetRoadPosition(index, out bool mirrored);
                bool forceHalfFill = lastRoadIsHalfFilled && index == lastRoadPairIndex;
                view.Bind(index, position, mirrored, forceHalfFill);
            });
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
            if (active.ContainsKey(i))
                continue;

            T view = pool.Get();
            bind(i, view);
            active[i] = view;
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
