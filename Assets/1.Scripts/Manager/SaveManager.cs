using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SaveManager : Singleton<SaveManager>
{
    private const string BestScoreKey = "BestScore";
    private int _bestScore = 0;
    public int BestScore => _bestScore;

    protected override void OnAwake()
    {
        _bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    private void OnEnable() => ScoreManager.OnHighScoreUpdated += UpdateBestScore;
    private void OnDisable() => ScoreManager.OnHighScoreUpdated -= UpdateBestScore;

    private void UpdateBestScore(int newScore)
    {
        _bestScore = newScore;
        PlayerPrefs.SetInt(BestScoreKey, _bestScore);
        PlayerPrefs.Save();
    }
}