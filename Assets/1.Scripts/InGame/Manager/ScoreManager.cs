using System;
using UnityEngine;

public class ScoreManager : Singleton<ScoreManager>
{
    [SerializeField]
    private float _lineScoreMultiplier = 2f;
    [SerializeField]
    private float _lineBonusMultiplier = 0.5f;
    [SerializeField]
    private float _comboScoreMultiplier = 0.1f;
    [SerializeField]
    private int _comboRemainCount = 5;

    private int _currentPlaceCount = 0;
    private int _currentComboCount = 0;

    public event Action<int> OnScoreChanged;

    public int CurrentScore { get; private set; }

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
        // 지워진 라인 기본 점수
        float baseScore = BoardManager.Instance._width * lines * _lineScoreMultiplier;

        // 여러 줄 동시 제거
        float multiLineBonusMultiplier = 1 + (lines - 1) * _lineBonusMultiplier;

        // 콤보 
        float comboMultiplier = 1f;
        if (_currentPlaceCount <= _comboRemainCount)
        {
            _currentPlaceCount = 0;

            if (_currentComboCount >= 1)
            {
                float comboBonus = Mathf.Clamp(_comboScoreMultiplier * _currentComboCount, 0f, 0.5f);
                comboMultiplier = 1f + comboBonus;
            }
            _currentComboCount++;
        }
        else
            _currentComboCount = 0;

        int totalScore = Mathf.FloorToInt(baseScore * comboMultiplier * multiLineBonusMultiplier);

        AddScore(totalScore);
    }

    public void AddScore(int score)
    {
        CurrentScore += score;
        OnScoreChanged?.Invoke(CurrentScore);
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        OnScoreChanged?.Invoke(CurrentScore);
    }
}