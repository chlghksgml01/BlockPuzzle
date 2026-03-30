using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private NumberDisplay _scoreDisplay;
    [SerializeField] private NumberDisplay _bestScoreDisplay;
    [SerializeField] private float _animationDuration = 0.5f;

    private int _bestScore;

    private void OnEnable()
    {
        if (ScoreManager.HasInstance)
        {
            ScoreManager.Instance.OnScoreChanged += RollUpdateScoreUI;
            ScoreManager.Instance.OnResetScore += ResetScore;
        }
        UpdateBestScore();
    }

    private void UpdateBestScore()
    {
        if (LeaderboardManager.HasInstance)
        {
            _bestScore = LeaderboardManager.Instance.BestScore;
            if (_bestScore != 0)
            {
                _bestScoreDisplay.UpdateDisplay(LeaderboardManager.Instance.BestScore);
            }
        }
    }

    private void RollUpdateScoreUI(int currentScore, int newScore)
    {
        _scoreDisplay.ScoreRollUpdate(currentScore, newScore, _animationDuration);
        if (newScore > _bestScore)
        {
            _bestScore = newScore;
            _bestScoreDisplay.UpdateDisplay(_bestScore);
        }
    }

    private void ResetScore()
    {
        _scoreDisplay.UpdateDisplay(0);
        UpdateBestScore();
    }
}