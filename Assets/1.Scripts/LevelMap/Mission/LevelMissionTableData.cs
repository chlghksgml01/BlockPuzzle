using UnityEngine;

/// <summary>
/// 레벨 인덱스별 클리어 미션을 담는 테이블 데이터.
/// 배열 인덱스 i는 레벨 (i+1)에 대응하며, 배열 길이(LevelCount)가 곧 총 레벨 수이다.
/// </summary>
[CreateAssetMenu(menuName = "LevelMap/Mission/Level Mission Table", fileName = "LevelMissionTable")]
public class LevelMissionTableData : ScriptableObject
{
    [Tooltip("레벨별 미션 목록. 인덱스 i는 레벨 (i+1)에 대응한다")]
    [SerializeField] private MissionData[] _missions;

    public int LevelCount => _missions != null ? _missions.Length : 0;

    /// <summary>레벨 인덱스(0-base)에 해당하는 미션을 반환. 범위를 벗어나면 null.</summary>
    public MissionData GetMission(int levelIndex)
    {
        if (_missions == null || levelIndex < 0 || levelIndex >= _missions.Length)
            return null;

        return _missions[levelIndex];
    }

    /// <summary>
    /// 레벨 1부터 연속으로 해금된 마지막 레벨 (1-base). 없으면 -1.
    /// currentLevel은 1-base 현재 플레이 레벨이며, 인덱스 i는 i &lt; currentLevel 이면 해금.
    /// </summary>
    public int GetLastConsecutiveClearLevel(int currentLevel)
    {
        if (_missions == null)
            return -1;

        for (int i = 0; i < _missions.Length; i++)
        {
            MissionData mission = _missions[i];
            if (mission == null || i >= currentLevel)
                return i;
        }

        return _missions.Length;
    }

    /// <summary>
    /// 실제 클리어 완료된 마지막 레벨 인덱스 (0-base).
    /// 해금 구간은 '플레이 가능'까지 포함하므로, 연속 구간 끝(현재 플레이 레벨)은 제외한다.
    /// </summary>
    public int GetLastCompletedLevelIndex(int currentLevel)
    {
        int lastConsecutiveClearLevel = GetLastConsecutiveClearLevel(currentLevel);

        if (lastConsecutiveClearLevel <= 0)
            return -1;

        int consecutiveClearIndex = lastConsecutiveClearLevel - 1;

        if (consecutiveClearIndex >= _missions.Length - 1)
            return consecutiveClearIndex;

        return consecutiveClearIndex - 1;
    }

    /// <summary>
    /// 다음에 플레이할 레벨(해금되었지만 아직 완료되지 않음)의 인덱스 (0-base).
    /// 전체 레벨을 모두 클리어해 다음 레벨이 없으면 -1.
    /// </summary>
    public int GetCurrentPlayableLevelIndex(int currentLevel)
    {
        if (_missions == null || _missions.Length == 0)
            return -1;

        int lastConsecutiveClearLevel = GetLastConsecutiveClearLevel(currentLevel);
        int consecutiveClearIndex = lastConsecutiveClearLevel - 1;

        if (consecutiveClearIndex < 0)
            return 0;

        if (consecutiveClearIndex >= _missions.Length - 1)
            return -1;

        return consecutiveClearIndex;
    }
}
