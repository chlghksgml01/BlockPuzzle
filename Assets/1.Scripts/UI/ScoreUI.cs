using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI _bestScroreText;
    [SerializeField] private TextMeshProUGUI _currentScroreText;

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
            int bestScore = LeaderboardManager.Instance.BestScore;
            if (bestScore != 0)
                _bestScroreText.text = LeaderboardManager.Instance.BestScore.ToString();
        }
    }

    private void UpdateScoreUI(int newScore)
    {
        _currentScroreText.text = newScore.ToString();
    }

    private void ResetScore()
    {
        UpdateScoreUI(0);
        UpdateBestScore();
    }
}