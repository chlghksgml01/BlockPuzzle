using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : BaseOptionUI
{
    [Header("Displays")]
    [SerializeField] private NumberDisplay _scoreDisplay;
    [SerializeField] private NumberDisplay _bestScoreDisplay;

    [Header("Banner")]
    [SerializeField] private Image _titleBanner;
    [SerializeField] private Sprite _defaultBanner;
    [SerializeField] private Sprite _newBestBanner;

    private void OnEnable()
    {
        _scoreDisplay.UpdateDisplay(ScoreManager.Instance.CurrentScore, 0);
        _bestScoreDisplay.UpdateDisplay(LeaderboardManager.Instance.BestScore, 2);
    }

    public void UpdateBanner(bool isNewBest)
    {
        _titleBanner.sprite = isNewBest ? _newBestBanner : _defaultBanner;
    }
}
