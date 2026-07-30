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
    [Tooltip("아이콘/카운트/시간이 배치되는 HorizontalLayoutGroup 루트")]
    [SerializeField] private Transform _contentRoot;

    [Tooltip("목표 아이콘 프리팹 (루트에 Image)")]
    [SerializeField] private GameObject _iconPrefab;

    [Tooltip("남은 개수 텍스트 프리팹 (루트에 TextMeshProUGUI)")]
    [SerializeField] private GameObject _countPrefab;

    [Tooltip("남은 시간 텍스트 프리팹 (루트에 TextMeshProUGUI). 비우면 Count 프리팹을 재사용")]
    [SerializeField] private GameObject _timePrefab;

    [Header("Icons")]
    [Tooltip("Ice 미션 아이콘")]
    [SerializeField] private Sprite _iceIcon;

    [Tooltip("Grass 미션 아이콘")]
    [SerializeField] private Sprite _grassIcon;

    [Tooltip("시간 제한 아이콘")]
    [SerializeField] private Sprite _timeIcon;

    [Tooltip("Pentagon 보석 아이콘")]
    [SerializeField] private Sprite _pentagonIcon;

    [Tooltip("Square 보석 아이콘")]
    [SerializeField] private Sprite _squareIcon;

    [Tooltip("Star 보석 아이콘")]
    [SerializeField] private Sprite _starIcon;

    private readonly List<GameObject> _spawnedViews = new List<GameObject>();
    private TextMeshProUGUI _timeText;
    private TextMeshProUGUI _collectCountText;
    private RectTransform _collectIcon;
    private readonly Dictionary<GemType, TextMeshProUGUI> _gemCountTexts = new Dictionary<GemType, TextMeshProUGUI>();
    private readonly Dictionary<GemType, RectTransform> _gemIcons = new Dictionary<GemType, RectTransform>();
    private MissionType _builtForType = MissionType.None;
    private bool _isBuilt;

    private void OnEnable()
    {
        MissionManager.OnMissionBound += HandleMissionBound;
        MissionManager.OnMissionCleared += HandleMissionCleared;
        MissionManager.OnProgressChanged += HandleProgressChanged;
        MissionManager.OnTimeChanged += HandleTimeChanged;
        RefreshAll();
    }

    private void OnDisable()
    {
        MissionManager.OnMissionBound -= HandleMissionBound;
        MissionManager.OnMissionCleared -= HandleMissionCleared;
        MissionManager.OnProgressChanged -= HandleProgressChanged;
        MissionManager.OnTimeChanged -= HandleTimeChanged;
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
    }

    private void HandleProgressChanged()
    {
        EnsureContentBuilt();
        UpdateProgressTexts();
    }

    private void HandleTimeChanged()
    {
        UpdateTimeText();
    }

    private void RefreshAll()
    {
        MissionManager manager = MissionManager.Instance;
        if (manager == null || !manager.IsActive || manager.CurrentMission == null)
        {
            SetRootActive(false);
            ClearContent();
            return;
        }

        SetRootActive(true);
        UpdateLevelText(manager.CurrentLevelNumber);
        RebuildContent(manager);
        UpdateProgressTexts();
        UpdateTimeText();
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

        switch (manager.CurrentMissionType)
        {
            case MissionType.ScoreGoal:
                SpawnIcon(_timeIcon);
                _timeText = SpawnText(GetTimePrefab());
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
                UpdateTimeText();
                break;
        }
    }

    private void UpdateTimeText()
    {
        if (_timeText == null)
            return;

        MissionManager manager = MissionManager.Instance;
        if (manager == null)
            return;

        _timeText.text = FormatTime(manager.RemainingTimeSeconds);
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

    private GameObject GetTimePrefab()
    {
        return _timePrefab != null ? _timePrefab : _countPrefab;
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
        _timeText = null;
        _collectCountText = null;
        _collectIcon = null;
        _builtForType = MissionType.None;
        _isBuilt = false;
    }

    private void SetRootActive(bool active)
    {
        if (_root != null)
            _root.SetActive(active);
    }

    private static string FormatTime(float timeSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(timeSeconds));
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }
}
