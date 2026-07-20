using UnityEngine;

/// <summary>
/// 보드 위 특정 블록을 대상으로 하는 미션의 목표 종류.
/// </summary>
public enum BlockGoalType
{
    /// <summary>ICE 블록을 모두 제거</summary>
    ClearIce,

    /// <summary>GRASS 블록을 모두 제거</summary>
    ClearGrass,

    /// <summary>보석(GEM) 블록을 목표 개수만큼 수집</summary>
    CollectGem,
}

/// <summary>
/// "보드 위 특정 블록을 목표 개수만큼 제거/수집" 미션 정의.
/// ICE 제거, GRASS 제거, 보석 수집은 데이터 형태(대상 종류 + 목표 개수)가 동일하여 하나의 클래스로 표현한다.
/// </summary>
[CreateAssetMenu(menuName = "LevelMap/Mission/Board Block Goal Mission", fileName = "Mission_BoardBlockGoal")]
public sealed class BoardBlockGoalMissionData : LevelMissionData
{
    [Header("Block Goal")]
    [Tooltip("미션의 대상이 되는 블록 종류")]
    [SerializeField] private BlockGoalType _goalType;

    [Tooltip("목표 개수 (제거/수집해야 하는 블록 수)")]
    [SerializeField] private int _targetCount;

    public BlockGoalType GoalType => _goalType;
    public int TargetCount => _targetCount;
}
