using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private NumberDisplay _scoreDisplay;
    [SerializeField] private NumberDisplay _bestScoreDisplay;

    private int _bestScore;

    private void OnEnable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScoreUI;
            ScoreManager.Instance.OnResetScore += ResetScore;
        }
        UpdateBestScore();
    }

    private void UpdateBestScore()
    {
        if (LeaderboardManager.Instance != null)
        {
            _bestScore = LeaderboardManager.Instance.BestScore;
            if (_bestScore != 0)
            {
                _bestScoreDisplay.UpdateDisplay(LeaderboardManager.Instance.BestScore);
            }
        }
    }

    private void UpdateScoreUI(int newScore)
    {
        _scoreDisplay.UpdateDisplay(newScore);
        if (newScore > _bestScore)
        {
            _bestScore = newScore;
            _bestScoreDisplay.UpdateDisplay(_bestScore);
        }
    }

    private void ResetScore()
    {
        UpdateScoreUI(0);
        UpdateBestScore();
    }
}