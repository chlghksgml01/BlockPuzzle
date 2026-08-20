using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LevelInGame 미션 HUD.
/// MissionManager의 레벨/진행도 이벤트를 구독해 표시만 담당한다.
/// </summary>
public class MissionHUD : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("미션 HUD 루트. 비레벨 세션이면 비활성화한다.")]
    [SerializeField] private GameObject _root;

    [Tooltip("레벨 번호를 표시하는 텍스트 (예: 'Level 5')")]
    [SerializeField] private TextMeshProUGUI _levelText;

    [Header("Content")]
    [Tooltip("아이콘/카운트가 배치되는 HorizontalLayoutGroup 루트")]
    [SerializeField] private Transform _contentRoot;

    [Tooltip("목표 아이콘 프리팹 (루트에 Image)")]
    [SerializeField] private GameObject _iconPrefab;

    [Tooltip("남은 개수/목표 점수 텍스트 프리팹 (루트에 TextMeshProUGUI)")]
    [SerializeField] private GameObject _countPrefab;

    [Tooltip("목표 점수 텍스트 프리팹 (ScoreGoal LayoutGroup용)")]
    [SerializeField] private GameObject _scorePrefab;

    [Header("ScoreGoal Current Score")]
    [Tooltip("ScoreGoal 미션일 때만 활성화하는 현재 점수 루트 (Mission/Score)")]
    [SerializeField] private GameObject _currentScoreRoot;

    [Tooltip("현재 점수를 표시하는 NumberDisplay")]
    [SerializeField] private NumberDisplay _currentScoreDisplay;

    [Tooltip("점수 롤 애니메이션 시간(초)")]
    [SerializeField] private float _scoreRollDuration = 0.5f;

    [Header("Icons")]
    [Tooltip("Ice 미션 아이콘")]
    [SerializeField] private Sprite _iceIcon;

    [Tooltip("Grass 미션 아이콘")]
    [SerializeField] private Sprite _grassIcon;

    [Tooltip("ScoreGoal 미션 아이콘")]
    [SerializeField] private Sprite _timeIcon;

    [Tooltip("Pentagon 보석 아이콘")]
    [SerializeField] private Sprite _pentagonIcon;

    [Tooltip("Square 보석 아이콘")]
    [SerializeField] private Sprite _squareIcon;

    [Tooltip("Star 보석 아이콘")]
    [SerializeField] private Sprite _starIcon;

    private readonly List<GameObject> _spawnedViews = new List<GameObject>();
    private TextMeshProUGUI _scoreGoalText;
    private TextMeshProUGUI _collectCountText;
    private RectTransform _collectIcon;
    private readonly Dictionary<GemType, TextMeshProUGUI> _gemCountTexts = new Dictionary<GemType, TextMeshProUGUI>();
    private readonly Dictionary<GemType, RectTransform> _gemIcons = new Dictionary<GemType, RectTransform>();
    private MissionType _builtForType = MissionType.None;
    private bool _isBuilt;
    private int _displayedScore;

    private void OnEnable()
    {
        EnsureCurrentScoreDisplay();
        MissionManager.OnMissionBound += HandleMissionBound;
        MissionManager.OnMissionCleared += HandleMissionCleared;
        MissionManager.OnProgressChanged += HandleProgressChanged;
        MissionManager.OnScoreGoalProgressChanged += HandleScoreGoalProgressChanged;
        RefreshAll();
    }

    private void OnDisable()
    {
        MissionManager.OnMissionBound -= HandleMissionBound;
        MissionManager.OnMissionCleared -= HandleMissionCleared;
        MissionManager.OnProgressChanged -= HandleProgressChanged;
        MissionManager.OnScoreGoalProgressChanged -= HandleScoreGoalProgressChanged;
    }

    private void EnsureCurrentScoreDisplay()
    {
        if (_currentScoreDisplay != null)
            return;

        if (_currentScoreRoot != null)
            _currentScoreDisplay = _currentScoreRoot.GetComponent<NumberDisplay>();
    }

    /// <summary>Ice/Grass 수집 아이콘의 월드 좌표.</summary>
    public bool TryGetCollectIconWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = default;
        if (_collectIcon == null)
            return false;

        worldPosition = _collectIcon.position;
        return true;
    }

    /// <summary>Gem 종류별 아이콘의 월드 좌표.</summary>
    public bool TryGetGemIconWorldPosition(GemType gemType, out Vector3 worldPosition)
    {
        worldPosition = default;
        if (!_gemIcons.TryGetValue(gemType, out RectTransform icon) || icon == null)
            return false;

        worldPosition = icon.position;
        return true;
    }

    private void HandleMissionBound()
    {
        RefreshAll();
    }

    private void HandleMissionCleared()
    {
        SetRootActive(false);
        ClearContent();
        SetCurrentScoreActive(false);
    }

    private void HandleProgressChanged()
    {
        EnsureContentBuilt();
        UpdateProgressTexts();
        RebuildLayout();
    }

    private void HandleScoreGoalProgressChanged(int previousScore, int newScore)
    {
        EnsureContentBuilt();
        if (_scoreGoalText != null && MissionManager.Instance != null)
            _scoreGoalText.text = MissionManager.Instance.TargetScore.ToString();

        RollCurrentScoreDisplay(previousScore, newScore);
        RebuildLayout();
    }

    private void RefreshAll()
    {
        MissionManager manager = MissionManager.Instance;
        if (manager == null || !manager.IsActive || manager.CurrentMission == null)
        {
            SetRootActive(false);
            ClearContent();
            SetCurrentScoreActive(false);
            return;
        }

        SetRootActive(true);
        UpdateLevelText(manager.CurrentLevelNumber);
        RebuildContent(manager);
        UpdateProgressTexts();
        RebuildLayout();
    }

    private void UpdateLevelText(int levelNumber)
    {
        if (_levelText == null)
            return;

        _levelText.text = $"Level  {levelNumber}";
    }

    private void EnsureContentBuilt()
    {
        MissionManager manager = MissionManager.Instance;
        if (manager == null || !manager.IsActive)
            return;

        if (!_isBuilt || _builtForType != manager.CurrentMissionType)
            RebuildContent(manager);
    }

    private void RebuildContent(MissionManager manager)
    {
        ClearContent();
        _builtForType = manager.CurrentMissionType;
        _isBuilt = true;

        bool isScoreGoal = manager.CurrentMissionType == MissionType.ScoreGoal;
        SetCurrentScoreActive(isScoreGoal);
        if (isScoreGoal)
            ResetCurrentScoreDisplay(manager.CurrentScore);

        switch (manager.CurrentMissionType)
        {
            case MissionType.ScoreGoal:
                SpawnIcon(_timeIcon);
                _scoreGoalText = SpawnText(_scorePrefab);
                break;

            case MissionType.Ice:
                _collectIcon = SpawnIcon(_iceIcon);
                _collectCountText = SpawnText(_countPrefab);
                break;

            case MissionType.Grass:
                _collectIcon = SpawnIcon(_grassIcon);
                _collectCountText = SpawnText(_countPrefab);
                break;

            case MissionType.Gem:
                SpawnGemRows(manager.RemainingGems);
                break;

            default:
                _isBuilt = false;
                break;
        }
    }

    private void SpawnGemRows(IReadOnlyList<GemTargetInfo> gems)
    {
        if (gems == null)
            return;

        for (int i = 0; i < gems.Count; i++)
        {
            GemTargetInfo gem = gems[i];
            RectTransform icon = SpawnIcon(GetGemSprite(gem.gemType));
            if (icon != null)
                _gemIcons[gem.gemType] = icon;

            TextMeshProUGUI countText = SpawnText(_countPrefab);
            if (countText != null)
                _gemCountTexts[gem.gemType] = countText;
        }
    }

    private void UpdateProgressTexts()
    {
        MissionManager manager = MissionManager.Instance;
        if (manager == null || !manager.IsActive)
            return;

        switch (manager.CurrentMissionType)
        {
            case MissionType.Ice:
            case MissionType.Grass:
                if (_collectCountText != null)
                    _collectCountText.text = manager.RemainingCollectCount.ToString();
                break;

            case MissionType.Gem:
                IReadOnlyList<GemTargetInfo> gems = manager.RemainingGems;
                for (int i = 0; i < gems.Count; i++)
                {
                    GemTargetInfo gem = gems[i];
                    if (_gemCountTexts.TryGetValue(gem.gemType, out TextMeshProUGUI text) && text != null)
                        text.text = gem.count.ToString();
                }
                break;

            case MissionType.ScoreGoal:
                if (_scoreGoalText != null)
                    _scoreGoalText.text = manager.TargetScore.ToString();
                // 현재 점수는 OnScoreGoalProgressChanged에서 롤 갱신한다.
                // 여기선 바인드/리셋 직후 표시값만 맞춘다.
                if (_displayedScore != manager.CurrentScore)
                    ResetCurrentScoreDisplay(manager.CurrentScore);
                break;
        }
    }

    private void SetCurrentScoreActive(bool active)
    {
        if (_currentScoreRoot != null)
            _currentScoreRoot.SetActive(active);

        if (active)
            EnsureCurrentScoreDisplay();
    }

    private void ResetCurrentScoreDisplay(int score)
    {
        EnsureCurrentScoreDisplay();
        _displayedScore = score;
        if (_currentScoreDisplay != null)
            _currentScoreDisplay.UpdateDisplay(score);
    }

    private void RollCurrentScoreDisplay(int previousScore, int newScore)
    {
        EnsureCurrentScoreDisplay();
        if (_currentScoreDisplay == null)
            return;

        _displayedScore = newScore;
        if (previousScore == newScore)
        {
            _currentScoreDisplay.UpdateDisplay(newScore);
            return;
        }

        _currentScoreDisplay.ScoreRollUpdate(previousScore, newScore, _scoreRollDuration);
    }

    private RectTransform SpawnIcon(Sprite sprite)
    {
        if (_iconPrefab == null || _contentRoot == null)
            return null;

        GameObject icon = Instantiate(_iconPrefab, _contentRoot);
        Image image = icon.GetComponent<Image>();
        if (image == null)
            image = icon.GetComponentInChildren<Image>();
        if (image != null)
        {
            image.sprite = sprite;
            image.SetNativeSize();
        }

        _spawnedViews.Add(icon);
        return icon.transform as RectTransform;
    }

    private TextMeshProUGUI SpawnText(GameObject prefab)
    {
        if (prefab == null || _contentRoot == null)
            return null;

        GameObject view = Instantiate(prefab, _contentRoot);
        _spawnedViews.Add(view);

        TextMeshProUGUI text = view.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = view.GetComponentInChildren<TextMeshProUGUI>();
        return text;
    }

    private Sprite GetGemSprite(GemType gemType)
    {
        switch (gemType)
        {
            case GemType.Pentagon: return _pentagonIcon;
            case GemType.Square: return _squareIcon;
            case GemType.Star: return _starIcon;
            default: return null;
        }
    }

    private void ClearContent()
    {
        if (_contentRoot != null)
        {
            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_contentRoot.GetChild(i).gameObject);
            }
        }

        _spawnedViews.Clear();
        _gemCountTexts.Clear();
        _gemIcons.Clear();
        _scoreGoalText = null;
        _collectCountText = null;
        _collectIcon = null;
        _builtForType = MissionType.None;
        _isBuilt = false;
        _displayedScore = 0;
    }

    private void SetRootActive(bool active)
    {
        if (_root != null)
            _root.SetActive(active);
    }

    /// <summary>
    /// ContentSizeFitter 텍스트 너비가 LayoutGroup spacing에 반영되도록 즉시 재배치한다.
    /// </summary>
    private void RebuildLayout()
    {
        if (_contentRoot is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }
}
