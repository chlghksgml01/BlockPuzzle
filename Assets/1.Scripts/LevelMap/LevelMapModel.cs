using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct LevelNodeLayout
{
    public readonly int LevelIndex;
    public readonly Vector2 Position;
    public readonly LevelNodeIconType IconType;

    public LevelNodeLayout(int levelIndex, Vector2 position, LevelNodeIconType iconType)
    {
        LevelIndex = levelIndex;
        Position = position;
        IconType = iconType;
    }
}

public readonly struct PathSegmentLayout
{
    public readonly Vector2 CenterPosition;
    public readonly Vector2 Size;
    public readonly float RotationZ;
    public readonly float MinY;
    public readonly float MaxY;

    public PathSegmentLayout(Vector2 centerPosition, Vector2 size, float rotationZ, float minY, float maxY)
    {
        CenterPosition = centerPosition;
        Size = size;
        RotationZ = rotationZ;
        MinY = minY;
        MaxY = maxY;
    }
}

/// <summary>
/// LevelMapRouteData로부터 노드/경로 막대의 배치 좌표를 계산하는 순수 데이터 모델.
/// Unity UI(RectTransform 등)에 직접 접근하지 않으며, 뷰포트에 걸치는 인덱스 범위 조회만 제공한다.
/// 노드 Y좌표가 인덱스 순으로 대체로 단조 변화한다는 전제 하에 선형 스캔으로 범위를 찾는다.
/// </summary>
public class LevelMapModel
{
    private readonly List<LevelNodeLayout> _nodeLayouts = new List<LevelNodeLayout>();
    private readonly List<PathSegmentLayout> _segmentLayouts = new List<PathSegmentLayout>();

    public IReadOnlyList<LevelNodeLayout> NodeLayouts => _nodeLayouts;
    public IReadOnlyList<PathSegmentLayout> SegmentLayouts => _segmentLayouts;
    public Vector2 ContentSize { get; }

    public LevelMapModel(LevelMapRouteData routeData, float pathThickness, float contentPaddingY)
    {
        IReadOnlyList<LevelMapNodeEntry> entries = routeData != null ? routeData.Nodes : Array.Empty<LevelMapNodeEntry>();

        float minY = 0f;
        float maxY = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            LevelMapNodeEntry entry = entries[i];
            Vector2 position = entry.LocalPosition;
            _nodeLayouts.Add(new LevelNodeLayout(i + 1, position, entry.IconType));

            if (i == 0)
            {
                minY = maxY = position.y;
                continue;
            }

            minY = Mathf.Min(minY, position.y);
            maxY = Mathf.Max(maxY, position.y);

            Vector2 previous = entries[i - 1].LocalPosition;
            BuildSegments(previous, position, entry.BendHorizontalFirst, pathThickness);
        }

        float height = entries.Count > 0 ? (maxY - minY) + contentPaddingY * 2f : 0f;
        ContentSize = new Vector2(0f, height);
    }

    public void GetVisibleNodeIndices(float viewportMinY, float viewportMaxY, HashSet<int> result)
    {
        result.Clear();
        for (int i = 0; i < _nodeLayouts.Count; i++)
        {
            float y = _nodeLayouts[i].Position.y;
            if (y >= viewportMinY && y <= viewportMaxY)
                result.Add(i);
        }
    }

    public void GetVisibleSegmentIndices(float viewportMinY, float viewportMaxY, HashSet<int> result)
    {
        result.Clear();
        for (int i = 0; i < _segmentLayouts.Count; i++)
        {
            PathSegmentLayout segment = _segmentLayouts[i];
            if (segment.MaxY >= viewportMinY && segment.MinY <= viewportMaxY)
                result.Add(i);
        }
    }

    private void BuildSegments(Vector2 from, Vector2 to, bool bendHorizontalFirst, float thickness)
    {
        if (Mathf.Approximately(from.x, to.x) || Mathf.Approximately(from.y, to.y))
        {
            AddStraightSegment(from, to, thickness);
            return;
        }

        Vector2 corner = bendHorizontalFirst ? new Vector2(to.x, from.y) : new Vector2(from.x, to.y);
        AddStraightSegment(from, corner, thickness);
        AddStraightSegment(corner, to, thickness);
    }

    private void AddStraightSegment(Vector2 from, Vector2 to, float thickness)
    {
        Vector2 diff = to - from;
        float length = diff.magnitude;
        if (length <= 0f)
            return;

        Vector2 center = (from + to) * 0.5f;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        float minY = Mathf.Min(from.y, to.y);
        float maxY = Mathf.Max(from.y, to.y);

        _segmentLayouts.Add(new PathSegmentLayout(center, new Vector2(length, thickness), angle, minY, maxY));
    }
}
