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
        if (SaveManager.Instance != null)
            _bestScroreText.text = SaveManager.Instance.BestScore.ToString();
    }

    private void UpdateScoreUI(int newScore)
    {
        _currentScroreText.text = newScore.ToString();
    }

    private void ResetScore()
    {
        UpdateScoreUI(0);
        _bestScroreText.text = SaveManager.Instance.BestScore.ToString();
    }
}