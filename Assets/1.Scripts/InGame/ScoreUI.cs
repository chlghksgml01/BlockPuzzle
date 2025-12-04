using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    private TextMeshProUGUI _scroreText;

    private void OnEnable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged += UpdateScoreUI;
    }
    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= UpdateScoreUI;
    }

    private void Awake()
    {
        _scroreText = GetComponent<TextMeshProUGUI>();
    }

    private void UpdateScoreUI(int newScore)
    {
        _scroreText.text = newScore.ToString();
    }
}