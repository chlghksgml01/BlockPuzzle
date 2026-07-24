using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨 클리어 미션을 안내하는 팝업.
/// 미션 데이터 타입(점수 목표 / 블록 수집 / 보석 수집)에 따라 해당 UI 그룹만 활성화하고 내용을 채운다.
/// </summary>
public class MissionPopupUI : BasePopupUI
{
    [Header("Common")]
    [Tooltip("레벨 번호를 표시하는 텍스트 (예: 'Level  5')")]
    [SerializeField] private TextMeshProUGUI _levelText;

    [Tooltip("하드 미션일 때 표시되는 해골 아이콘")]
    [SerializeField] private GameObject _skull;

    [Header("Score Goal Mission")]
    [Tooltip("점수 목표 미션 UI 그룹 루트")]
    [SerializeField] private GameObject _scoreGoalMission;

    [Tooltip("제한 시간을 'm:ss' 형식으로 표시하는 텍스트")]
    [SerializeField] private TextMeshProUGUI _scoreTimeText;

    [Header("Collect Block Mission")]
    [Tooltip("블록 수집 미션 UI 그룹 루트")]
    [SerializeField] private GameObject _iceGrassMission;

    [Tooltip("목표 블록 종류를 표시하는 아이콘 이미지")]
    [SerializeField] private Image _blockIconImage;

    [Tooltip("목표 블록 개수를 표시하는 텍스트")]
    [SerializeField] private TextMeshProUGUI _goalBlockCount;

    [Tooltip("Ice 블록 목표일 때 사용할 아이콘 스프라이트")]
    [SerializeField] private Sprite _iceIcon;

    [Tooltip("Grass 블록 목표일 때 사용할 아이콘 스프라이트")]
    [SerializeField] private Sprite _grassIcon;

    [Header("Collect Gem Mission")]
    [Tooltip("보석 수집 미션 UI 그룹 루트 (HorizontalLayoutGroup, 아이콘/개수 프리팹이 동적으로 채워짐)")]
    [SerializeField] private GameObject _collectGemMission;

    [Tooltip("Pentagon 보석 아이콘 스프라이트")]
    [SerializeField] private Sprite _pentagonImage;

    [Tooltip("Square 보석 아이콘 스프라이트")]
    [SerializeField] private Sprite _squareImage;

    [Tooltip("Star 보석 아이콘 스프라이트")]
    [SerializeField] private Sprite _starImage;

    [Tooltip("보석 목표 개수를 표시하는 텍스트 프리팹 (루트에 TextMeshProUGUI)")]
    [SerializeField] private GameObject _countPrefab;

    [Tooltip("보석 종류 아이콘 프리팹 (루트에 Image)")]
    [SerializeField] private GameObject _iconPrefab;

    private readonly List<GameObject> _spawnedGemViews = new List<GameObject>();

    /// <summary>레벨 번호와 해당 레벨의 미션 데이터로 팝업을 연다.</summary>
    public void Open(int levelIndex, LevelMissionData missionData)
    {
        base.Open();

        if (_levelText != null)
            _levelText.text = $"Level  {levelIndex + 1}";

        _skull.SetActive(missionData.IsHard);

        _scoreGoalMission.SetActive(false);
        _iceGrassMission.SetActive(false);
        _collectGemMission.SetActive(false);

        switch (missionData)
        {
            case ScoreGoalMissionData score:
                _scoreGoalMission.SetActive(true);
                _scoreTimeText.text = FormatTime(score.TimeLimitSeconds);
                break;

            case CollectBlockGoalMissionData collect:
                _iceGrassMission.SetActive(true);
                _blockIconImage.sprite = collect.MissionType == MissionType.Ice ? _iceIcon : _grassIcon;
                _goalBlockCount.text = collect.TargetCount.ToString();
                break;

            case CollectGemMissionData gem:
                _collectGemMission.SetActive(true);
                SpawnGemTargets(gem.GemTargets);
                break;

            default:
                Debug.LogWarning($"[MissionPopupUI] Unknown mission data type: {missionData.GetType().Name}", this);
                break;
        }
    }

    private static string FormatTime(float timeLimitSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(timeLimitSeconds));
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }

    private void SpawnGemTargets(IReadOnlyList<GemTargetInfo> gemTargets)
    {
        ClearGemTargets();

        for (int i = 0; i < gemTargets.Count; i++)
        {
            GemTargetInfo target = gemTargets[i];

            GameObject icon = Instantiate(_iconPrefab, _collectGemMission.transform);
            icon.GetComponent<Image>().sprite = GetGemSprite(target.gemType);
            _spawnedGemViews.Add(icon);

            GameObject count = Instantiate(_countPrefab, _collectGemMission.transform);
            count.GetComponent<TextMeshProUGUI>().text = target.count.ToString();
            _spawnedGemViews.Add(count);
        }
    }

    private void ClearGemTargets()
    {
        for (int i = 0; i < _spawnedGemViews.Count; i++)
            Destroy(_spawnedGemViews[i]);

        _spawnedGemViews.Clear();
    }

    private Sprite GetGemSprite(GemType gemType)
    {
        switch (gemType)
        {
            case GemType.Pentagon: return _pentagonImage;
            case GemType.Square: return _squareImage;
            case GemType.Star: return _starImage;
            default:
                Debug.LogWarning($"[MissionPopupUI] Unknown gem type: {gemType}", this);
                return null;
        }
    }
}
