using UnityEngine;

/// <summary>
/// 보드 점유율이 높을 때 블록 형태(모양) 출현 가중치를 조절하는 설정값.
/// InGameManager가 소유하며, 블록 스폰 시 DraggableBlock에 전달된다.
/// </summary>
[System.Serializable]
public class HighFillShapeWeightSettings
{
    [Tooltip("보드 점유율이 이 값 이상이면 가중치 감쇠를 적용한다")]
    [SerializeField, Range(0f, 1f)] private float _startFillRatio = 0.5f;

    [Tooltip("이 칸 수 이상이면 고점유 시 대형으로 보고 출현 가중치를 낮춘다")]
    [SerializeField, Min(1)] private int _largeShapeCellThreshold = 5;

    [Tooltip("이 칸 수 이하면 고점유 시 소형으로 보고 출현 가중치를 높인다")]
    [SerializeField, Min(1)] private int _smallShapeCellThreshold = 3;

    [Tooltip("보드 점유율이 높을 때 대형 블록 가중치에 곱하는 값")]
    [SerializeField, Range(0f, 1f)] private float _highFillLargeShapeWeightMultiplier = 0.7f;

    [Tooltip("보드 점유율이 높을 때 소형 블록 가중치에 곱하는 값")]
    [SerializeField, Range(1f, 2f)] private float _highFillSmallShapeWeightMultiplier = 1.3f;

    public float StartFillRatio => _startFillRatio;
    public int LargeShapeCellThreshold => _largeShapeCellThreshold;
    public int SmallShapeCellThreshold => _smallShapeCellThreshold;
    public float HighFillLargeShapeWeightMultiplier => _highFillLargeShapeWeightMultiplier;
    public float HighFillSmallShapeWeightMultiplier => _highFillSmallShapeWeightMultiplier;
}
