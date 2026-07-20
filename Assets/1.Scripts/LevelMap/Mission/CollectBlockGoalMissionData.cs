using UnityEngine;

public enum BlockType
{
    Ice,
    Grass
}

/// <summary>
/// "보드 위 특정 블록을 목표 개수만큼 제거/수집" 미션 정의.
/// ICE 제거, GRASS 제거, 보석 수집은 데이터 형태(대상 종류 + 목표 개수)가 동일하여 하나의 클래스로 표현한다.
/// </summary>
[CreateAssetMenu(menuName = "LevelMap/Mission/Collect Block Goal Mission", fileName = "Mission_CollectBlockGoal")]
public sealed class CollectBlockGoalMissionData : LevelMissionData
{
    [Tooltip("목표 블록 종류")]
    [SerializeField] private BlockType _targetBlockType;

    [Tooltip("목표 개수 (제거/수집해야 하는 블록 수)")]
    [SerializeField] private int _targetCount;

    public BlockType TargetBlockType => _targetBlockType;

    public int TargetCount => _targetCount;
}
