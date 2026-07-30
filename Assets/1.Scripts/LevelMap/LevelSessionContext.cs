/// <summary>
/// 레벨 인게임 씬 진입 전 선택된 레벨 정보를 씬 간에 전달한다.
/// </summary>
public static class LevelSessionContext
{
    private static int _selectedLevelIndex = -1;
    private static LevelMissionTableData _missionTable;

    public static bool IsActive => _selectedLevelIndex >= 0 && _missionTable != null;

    public static int SelectedLevelIndex => _selectedLevelIndex;

    public static void BeginLevel(int levelIndex, LevelMissionTableData missionTable)
    {
        _selectedLevelIndex = levelIndex;
        _missionTable = missionTable;
    }

    /// <summary>씬 진입 시 전달된 미션 테이블. 비활성 시 null.</summary>
    public static LevelMissionTableData GetMissionTable()
    {
        return _missionTable;
    }

    public static MissionData GetSelectedMission()
    {
        if (!IsActive)
            return null;

        return _missionTable.GetMission(_selectedLevelIndex);
    }

    public static void Clear()
    {
        _selectedLevelIndex = -1;
        _missionTable = null;
    }
}
