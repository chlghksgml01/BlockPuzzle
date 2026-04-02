using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour, IInitializable
{
    [Header("Score Display")]
    [SerializeField] private NumberDisplay _scoreDisplay;
    [SerializeField] private NumberDisplay _bestScoreDisplay;
    [SerializeField] private float _animationDuration = 0.5f;

    private int _bestScore;
    private ScoreSystem _scoreSystem;

    public void Initialize(InitializeContext context)
    {
        _scoreSystem = context.ScoreSystem;
    }

    private void OnEnable()
    {
        if (_scoreSystem != null)
        {
            _scoreSystem.OnScoreChanged += RollUpdateScoreUI;
            _scoreSystem.OnResetScore += ResetScore;
        }
        UpdateBestScore();
    }

    private void OnDisable()
    {
        if (_scoreSystem != null)
        {
            _scoreSystem.OnScoreChanged -= RollUpdateScoreUI;
            _scoreSystem.OnResetScore -= ResetScore;
        }
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
        if (_scoreDisplay == null || _bestScoreDisplay == null)
            return;

        _scoreDisplay.ScoreRollUpdate(currentScore, newScore, _animationDuration);
        if (newScore > _bestScore)
        {
            _bestScore = newScore;
            _bestScoreDisplay.UpdateDisplay(_bestScore);
        }
    }

    private void ResetScore()
    {
        if (_scoreDisplay == null)
            return;

        _scoreDisplay.UpdateDisplay(0);
        UpdateBestScore();
    }
}