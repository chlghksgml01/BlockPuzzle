using UnityEngine;

/// <summary>
/// 레벨 인덱스별 클리어 미션을 담는 테이블 데이터.
/// 배열 인덱스 i는 레벨 (i+1)에 대응하며, 길이는 LevelMapManager의 총 레벨 수와 일치해야 한다.
/// </summary>
[CreateAssetMenu(menuName = "LevelMap/Mission/Level Mission Table", fileName = "LevelMissionTable")]
public class LevelMissionTableData : ScriptableObject
{
    [Tooltip("레벨별 미션 목록. 인덱스 i는 레벨 (i+1)에 대응한다")]
    [SerializeField] private LevelMissionData[] _missions;

    public int LevelCount => _missions != null ? _missions.Length : 0;

    /// <summary>레벨 인덱스(0-base)에 해당하는 미션을 반환. 범위를 벗어나면 null.</summary>
    public LevelMissionData GetMission(int levelIndex)
    {
        if (_missions == null || levelIndex < 0 || levelIndex >= _missions.Length)
            return null;

        return _missions[levelIndex];
    }
}
