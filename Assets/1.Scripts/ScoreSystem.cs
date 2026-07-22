using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ScoreSystem", menuName = "Game")]
public class ScoreSystem : ScriptableObject
{
    [Header("Score Multipliers & Settings")]
    [SerializeField] private float _lineScoreMultiplier = 5f;
    [SerializeField] private float _lineBonusMultiplier = 0.5f;
    [SerializeField] private float _comboScoreMultiplier = 0.1f;
    [SerializeField] private int _comboRemainCount = 5;

    public int CurrentScore { get; private set; }
    private int _currentPlaceCount = 0;
    private int _currentComboCount = 0;
    private int _boardWidth;

    public event Action<int, int> OnScoreChanged;
    public event Action<bool, int, int> OnBonusScore;
    public event Action<int> OnHighScoreUpdated;

    public void Initialize(int width)
    {
        _boardWidth = width;
    }

    public void ResetRuntimeState()
    {
        CurrentScore = 0;
        _currentPlaceCount = 0;
        _currentComboCount = 0;
        _boardWidth = 0;
    }

    public void HandleBlockPlaced(int blockCount)
    {
        _currentPlaceCount++;

        if (_currentPlaceCount > _comboRemainCount)
        {
            _currentComboCount = 0;
        }

        AddScore(blockCount);
    }

    public void CalculateLineScore(int lines)
    {
        float baseScore = _boardWidth * lines * _lineScoreMultiplier;
        float multiLineBonusMultiplier = 1 + (lines - 1) * _lineBonusMultiplier;

        float comboMultiplier = 1f;
        int comboBonusAddedScore = 0;
        int comboCountForUI = 0;

        if (_currentPlaceCount <= _comboRemainCount)
        {
            if (_currentComboCount >= 1)
            {
                float comboBonus = Mathf.Clamp(_comboScoreMultiplier * _currentComboCount, 0f, 0.5f);
                comboMultiplier = 1f + comboBonus;
                comboCountForUI = _currentComboCount + 1;
            }
            _currentComboCount++;
        }
        else
        {
            _currentComboCount = 1;
            comboMultiplier = 1f;
        }

        _currentPlaceCount = 0;

        float noComboScoreF = baseScore * multiLineBonusMultiplier;
        float totalScoreF = baseScore * comboMultiplier * multiLineBonusMultiplier;

        int baseLineScore = Mathf.FloorToInt(baseScore);
        int noComboScore = Mathf.FloorToInt(noComboScoreF);
        int totalScore = Mathf.FloorToInt(totalScoreF);
        int lineBonusAddedScore = Mathf.Max(0, noComboScore - baseLineScore);

        if (comboMultiplier > 1f)
        {
            comboBonusAddedScore = Mathf.Max(0, totalScore - noComboScore);
        }

        AddScore(totalScore);

        if (comboBonusAddedScore > 0)
        {
            OnBonusScore?.Invoke(true, totalScore, comboCountForUI);
        }
        else
        {
            OnBonusScore?.Invoke(false, totalScore, 0);
        }

        SoundManager.Instance.PlaySFX(SFXType.ClearLine, _currentComboCount);
    }

    public void AddScore(int score)
    {
        int newScore = CurrentScore + score;
        OnScoreChanged?.Invoke(CurrentScore, newScore);
        CurrentScore = newScore;
    }

    public void ExportState(out int score, out int currentPlaceCount, out int currentComboCount)
    {
        score = CurrentScore;
        currentPlaceCount = _currentPlaceCount;
        currentComboCount = _currentComboCount;
    }

    public void RestoreState(int score, int currentPlaceCount, int currentComboCount)
    {
        int prevScore = CurrentScore;
        CurrentScore = Mathf.Max(0, score);
        _currentPlaceCount = Mathf.Max(0, currentPlaceCount);
        _currentComboCount = Mathf.Max(0, currentComboCount);

        OnScoreChanged?.Invoke(prevScore, CurrentScore);
    }

    public void ResetScore()
    {
        CheckHighScore(LeaderboardManager.Instance.BestScore);
        _currentPlaceCount = 0;
        _currentComboCount = 0;
        CurrentScore = 0;
    }

    public void CheckHighScore(int bestScore)
    {
        OnHighScoreUpdated?.Invoke(CurrentScore);
    }
}