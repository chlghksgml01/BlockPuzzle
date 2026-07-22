using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUIButtonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button _levelButton;
    [SerializeField] private Button _startButton;
    [SerializeField] private TextMeshProUGUI _currentLevelText;
    [SerializeField] private MissionPopupUI _missionPopupUI;

    [Tooltip("레벨별 클리어 상태를 조회하는 미션 테이블")]
    [SerializeField] private LevelMissionTableData _missionTable;

    [Tooltip("노드 클릭으로 선택된 레벨 번호 (1-base). Start 시 사용")]
    private int _selectedLevel = 1;

    private void OnEnable()
    {
        RefreshHighestClearedLevelDisplay();
    }

    private void OnValidate()
    {
        RefreshHighestClearedLevelDisplay();
    }

    private void Start()
    {
        _levelButton.onClick.AddListener(MissionPopup);
        _startButton.onClick.AddListener(LoadScene);
    }

    /// <summary>LevelButton에 클리어 진행 레벨을 표시한다.</summary>
    private void RefreshHighestClearedLevelDisplay()
    {
        if (_currentLevelText == null)
            return;

        int currentLevelNumber = _missionTable != null
            ? _missionTable.GetLastConsecutiveClearLevel()
            : 0;

        int displayLevel = currentLevelNumber > 0 ? currentLevelNumber : 1;

        _currentLevelText.text = FormatLevelText(displayLevel);
        _selectedLevel = displayLevel;
    }

    /// <summary>레벨 노드 선택 시 호출. levelIndex는 0-base. LevelButton 텍스트는 변경하지 않는다.</summary>
    public void SelectLevel(int levelIndex)
    {
        _selectedLevel = levelIndex + 1;
    }

    private static string FormatLevelText(int level)
    {
        return $"Level {level}";
    }

    private void MissionPopup()
    {
        if (_missionTable == null || _missionPopupUI == null)
            return;

        int currentLevelNumber = _missionTable.GetLastConsecutiveClearLevel();
        if (currentLevelNumber <= 0)
            return;

        int missionIndex = currentLevelNumber - 1;

        LevelMissionData mission = _missionTable.GetMission<LevelMissionData>(missionIndex);
        if (mission == null)
            return;

        _missionPopupUI.Open(missionIndex, mission);
        SoundManager.Instance.PlayUISFX();
    }

    private void LoadScene()
    {
        if (_missionTable == null)
        {
            Debug.LogWarning("[LevelUIButtonController] MissionTable이 없어 레벨 인게임을 시작할 수 없습니다.");
            return;
        }

        int levelIndex = ResolveSelectedLevelIndex();
        LevelSessionContext.BeginLevel(levelIndex, _missionTable);
        SceneLoadManager.LoadScene(SceneName.LevelInGame);
    }

    /// <summary>UI에 선택된 레벨의 0-base 인덱스를 반환한다.</summary>
    private int ResolveSelectedLevelIndex()
    {
        int maxIndex = Mathf.Max(0, _missionTable.LevelCount - 1);
        int selectedLevel = Mathf.Max(1, _selectedLevel);
        return Mathf.Clamp(selectedLevel - 1, 0, maxIndex);
    }
}
