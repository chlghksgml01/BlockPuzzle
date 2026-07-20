using UnityEngine;

/// <summary>
/// 레벨별 클리어 미션 정의의 공통 베이스.
/// 새로운 미션 타입 추가 시 이 클래스를 수정하지 않고 하위 클래스를 추가한다(OCP).
/// </summary>
public abstract class LevelMissionData : ScriptableObject
{
    [Header("Mission Info")]
    [Tooltip("미션 UI에 표시할 설명 문구 (예: '점수 5000 달성', 'ICE 블록 모두 제거')")]
    [SerializeField] private string _missionDescription;
    [SerializeField] private bool _isHard = false;

    public string MissionDescription => _missionDescription;
    public bool IsHard => _isHard;
}
