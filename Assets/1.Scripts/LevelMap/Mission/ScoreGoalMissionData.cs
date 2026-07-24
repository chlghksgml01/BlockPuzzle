using UnityEngine;

/// <summary>
/// "제한 시간 내 목표 점수 달성" 미션 정의.
/// </summary>
[CreateAssetMenu(menuName = "LevelMap/Mission/Score Goal Mission", fileName = "Mission_ScoreGoal")]
public sealed class ScoreGoalMissionData : LevelMissionData
{
    [Header("Score Goal")]
    [Tooltip("클리어에 필요한 목표 점수")]
    [SerializeField] private int _targetScore;

    [Tooltip("클리어 제한 시간 (초)")]
    [SerializeField] private float _timeLimitSeconds;

    public override MissionType MissionType => MissionType.ScoreGoal;

    public int TargetScore => _targetScore;
    public float TimeLimitSeconds => _timeLimitSeconds;
}
