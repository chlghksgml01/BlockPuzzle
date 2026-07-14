using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 뷰포트에 걸치는 노드/경로 막대만 오브젝트 풀에서 꺼내 배치하고, 벗어난 항목은 풀로 반납한다.
/// 레벨 맵 전체를 미리 생성해두지 않고 스크롤에 따라 필요한 만큼만 동적으로 유지하기 위한 재사용 엔진.
/// </summary>
public class LevelMapVirtualizer
{
    private readonly LevelMapModel _model;
    private readonly ObjectPool<LevelNodeView> _nodePool;
    private readonly ObjectPool<LevelPathSegmentView> _segmentPool;
    private readonly Func<int, bool> _isLevelUnlocked;
    private readonly Action<int> _onLevelClicked;

    private readonly Dictionary<int, LevelNodeView> _activeNodes = new Dictionary<int, LevelNodeView>();
    private readonly Dictionary<int, LevelPathSegmentView> _activeSegments = new Dictionary<int, LevelPathSegmentView>();

    private readonly HashSet<int> _visibleNodeIndices = new HashSet<int>();
    private readonly HashSet<int> _visibleSegmentIndices = new HashSet<int>();
    private readonly List<int> _removeBuffer = new List<int>();

    public LevelMapVirtualizer(LevelMapModel model, RectTransform content, LevelNodeView nodePrefab, LevelPathSegmentView segmentPrefab,
        Func<int, bool> isLevelUnlocked, Action<int> onLevelClicked)
    {
        _model = model;
        _isLevelUnlocked = isLevelUnlocked;
        _onLevelClicked = onLevelClicked;

        _nodePool = new ObjectPool<LevelNodeView>(
            createFunc: () => UnityEngine.Object.Instantiate(nodePrefab, content),
            actionOnGet: view => view.gameObject.SetActive(true),
            actionOnRelease: view =>
            {
                view.ResetView();
                view.gameObject.SetActive(false);
            },
            actionOnDestroy: view => UnityEngine.Object.Destroy(view.gameObject),
            collectionCheck: false,
            defaultCapacity: 16,
            maxSize: 128);

        _segmentPool = new ObjectPool<LevelPathSegmentView>(
            createFunc: () => UnityEngine.Object.Instantiate(segmentPrefab, content),
            actionOnGet: view => view.gameObject.SetActive(true),
            actionOnRelease: view => view.gameObject.SetActive(false),
            actionOnDestroy: view => UnityEngine.Object.Destroy(view.gameObject),
            collectionCheck: false,
            defaultCapacity: 16,
            maxSize: 128);
    }

    public void UpdateVisible(float viewportMinY, float viewportMaxY)
    {
        _model.GetVisibleNodeIndices(viewportMinY, viewportMaxY, _visibleNodeIndices);
        SyncNodes();

        _model.GetVisibleSegmentIndices(viewportMinY, viewportMaxY, _visibleSegmentIndices);
        SyncSegments();
    }

    private void SyncNodes()
    {
        _removeBuffer.Clear();
        foreach (int key in _activeNodes.Keys)
        {
            if (!_visibleNodeIndices.Contains(key))
                _removeBuffer.Add(key);
        }

        for (int i = 0; i < _removeBuffer.Count; i++)
        {
            int key = _removeBuffer[i];
            _nodePool.Release(_activeNodes[key]);
            _activeNodes.Remove(key);
        }

        foreach (int index in _visibleNodeIndices)
        {
            if (_activeNodes.ContainsKey(index))
                continue;

            LevelNodeLayout layout = _model.NodeLayouts[index];
            bool unlocked = _isLevelUnlocked == null || _isLevelUnlocked(layout.LevelIndex);

            LevelNodeView view = _nodePool.Get();
            view.Bind(layout, unlocked, _onLevelClicked);
            _activeNodes[index] = view;
        }
    }

    private void SyncSegments()
    {
        _removeBuffer.Clear();
        foreach (int key in _activeSegments.Keys)
        {
            if (!_visibleSegmentIndices.Contains(key))
                _removeBuffer.Add(key);
        }

        for (int i = 0; i < _removeBuffer.Count; i++)
        {
            int key = _removeBuffer[i];
            _segmentPool.Release(_activeSegments[key]);
            _activeSegments.Remove(key);
        }

        foreach (int index in _visibleSegmentIndices)
        {
            if (_activeSegments.ContainsKey(index))
                continue;

            PathSegmentLayout layout = _model.SegmentLayouts[index];
            LevelPathSegmentView view = _segmentPool.Get();
            view.Bind(layout);
            _activeSegments[index] = view;
        }
    }
}
