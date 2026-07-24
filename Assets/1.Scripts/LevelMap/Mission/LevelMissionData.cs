using UnityEngine;

/// <summary>
/// 레벨별 클리어 미션 정의의 공통 베이스.
/// 새로운 미션 타입 추가 시 이 클래스를 수정하지 않고 하위 클래스를 추가한다(OCP).
/// </summary>
public abstract class LevelMissionData : ScriptableObject
{
    [Header("Mission Info")]
    [SerializeField] private bool _isHard = false;
    [SerializeField] private bool _isClear = false;
    [Tooltip("보드 초기 배치 데이터")]
    [SerializeField] private BoardLayoutData _boardLayoutData;

    public bool IsHard => _isHard;
    public bool IsClear => _isClear;
    public BoardLayoutData BoardLayoutData => _boardLayoutData;
}

