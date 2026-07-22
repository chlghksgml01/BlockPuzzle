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

    [Tooltip("현재 표시 중인 최고 클리어 레벨 번호 (1-base)")]
    private int _currentLevel = 1;

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnCurrentLevelTextChanged);
        RefreshHighestClearedLevelDisplay();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnCurrentLevelTextChanged);
    }

    private void OnValidate()
    {
        RefreshHighestClearedLevelDisplay();
    }

    private void Start()
    {
        _levelButton.onClick.AddListener(MissionPopup);
        //_startButton.onClick.AddListener(() => SceneLoadManager.LoadScene(SceneName.Classic));
    }

    private void OnCurrentLevelTextChanged(Object changedObject)
    {
        if (changedObject != _currentLevelText)
            return;

        SyncCurrentLevelFromText();
    }

    /// <summary>클리어한 가장 높은 레벨을 텍스트에 반영한다.</summary>
    private void RefreshHighestClearedLevelDisplay()
    {
        if (_currentLevelText == null)
            return;

        int currentLevelNumber = _missionTable != null
            ? _missionTable.GetLastConsecutiveClearLevel()
            : 0;

        int displayLevel = currentLevelNumber > 0 ? currentLevelNumber : 1;

        _currentLevelText.text = FormatLevelText(displayLevel);
        _currentLevel = displayLevel;
    }

    private static string FormatLevelText(int level)
    {
        return $"Level  {level}";
    }

    /// <summary>_currentLevelText 내용을 파싱해 _currentLevel에 반영한다.</summary>
    private void SyncCurrentLevelFromText()
    {
        if (_currentLevelText == null)
            return;

        if (TryParseLevelFromText(_currentLevelText.text, out int level))
            _currentLevel = level;
    }

    private static bool TryParseLevelFromText(string text, out int level)
    {
        level = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (int.TryParse(text.Trim(), out level))
            return true;

        int lastStart = -1;
        int lastEnd = -1;
        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsDigit(text[i]))
                continue;

            if (lastStart == -1 || i > lastEnd + 1)
                lastStart = i;

            lastEnd = i;
        }

        return lastStart >= 0
            && int.TryParse(text.Substring(lastStart, lastEnd - lastStart + 1), out level);
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
}
