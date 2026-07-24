using UnityEngine;

/// <summary>
/// 레벨맵 노드/도로의 좌표를 계산하는 순수 로직.
/// Unity UI에 의존하지 않으며 LevelMapPatternData의 간격 값만으로 위치를 산출한다.
/// </summary>
public sealed class LevelMapLayout
{
    private static readonly float[] XAxisPattern = { 0f, 1f, 0f, -1f };

    private readonly LevelMapPatternData _pattern;

    public LevelMapLayout(LevelMapPatternData pattern)
    {
        _pattern = pattern;
    }

    /// <summary>0번부터 시작하는 노드 인덱스의 앵커 좌표</summary>
    public Vector2 GetNodePosition(int nodeIndex)
    {
        int pairIndex = nodeIndex / 2;
        int remainder = nodeIndex % 2;

        float y = _pattern.OriginYOffset
            + pairIndex * _pattern.PairCycleHeight
            + (remainder == 1 ? _pattern.ConnectedNodeGap : 0f);

        float x = _pattern.HorizontalAmplitude * XAxisPattern[nodeIndex % 4];

        return new Vector2(x, y);
    }

    /// <summary>roadPairIndex번째 Road(노드 2*roadPairIndex와 2*roadPairIndex+1을 연결)의 앵커 좌표와 좌/우 반전 여부</summary>
    public Vector2 GetRoadPosition(int roadPairIndex, out bool mirrored)
    {
        Vector2 lowerNodePosition = GetNodePosition(roadPairIndex * 2);
        mirrored = roadPairIndex % 2 == 1;

        float x = mirrored ? -_pattern.RoadHorizontalOffset : _pattern.RoadHorizontalOffset;
        float y = lowerNodePosition.y + _pattern.RoadVerticalOffsetFromLowerNode;

        return new Vector2(x, y);
    }

    /// <summary>주어진 Y좌표 근방의 노드 인덱스를 역산 (가상화 범위 계산용, 근사치)</summary>
    public int GetNodeIndexNearY(float y)
    {
        float localY = y - _pattern.OriginYOffset;
        int pairIndex = Mathf.FloorToInt(localY / _pattern.PairCycleHeight);
        return Mathf.Max(0, pairIndex * 2);
    }
}
