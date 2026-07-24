using UnityEngine;

public enum BlockType
{
    Ice,
    Grass
}

/// <summary>
/// 보드 위 Ice 또는 Grass를 목표 개수만큼 제거하는 미션.
/// 한 미션에는 Ice / Grass 중 하나만 설정한다.
/// </summary>
[CreateAssetMenu(menuName = "LevelMap/Mission/Collect Block Goal Mission", fileName = "Mission_CollectBlockGoal")]
public sealed class CollectBlockGoalMissionData : LevelMissionData
{
    [Tooltip("목표 블록 종류 (Ice 또는 Grass, 혼합 없음)")]
    [SerializeField] private BlockType _targetBlockType;

    [Tooltip("목표 개수 (제거/수집해야 하는 블록 수)")]
    [SerializeField] private int _targetCount;

    public override MissionType MissionType =>
        _targetBlockType == BlockType.Ice ? MissionType.Ice : MissionType.Grass;

    public BlockType TargetBlockType => _targetBlockType;

    public int TargetCount => _targetCount;
}
