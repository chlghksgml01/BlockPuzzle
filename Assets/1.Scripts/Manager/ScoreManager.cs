using JetBrains.Annotations;
using System;
using UnityEngine;

public class ScoreManager : Singleton<ScoreManager>
{
    [Header("Score Multipliers & Settings")]
    [SerializeField] private float _lineScoreMultiplier = 2f;
    [SerializeField] private float _lineBonusMultiplier = 0.5f;
    [SerializeField] private float _comboScoreMultiplier = 0.1f;
    [SerializeField] private int _comboRemainCount = 5;

    public int CurrentScore { get; private set; }
    private int _currentPlaceCount = 0;
    private int _currentComboCount = 0;
    private int _boardWidth;

    public event Action<int> OnScoreChanged;
    public event Action OnResetScore;
    public static event Action<int> OnHighScoreUpdated;

    private void OnEnable()
    {
        InGameManager.OnBlockSettled += HandleBlockPlaced;
        InGameManager.OnResetGame += ResetScore;
        InGameManager.OnGameOver += CheckHighScore;
        BoardManager.OnLinesCleared += CalculateLineScore;
    }

    private void Start()
    {
        if (BoardManager.Instance != null)
            _boardWidth = BoardManager.Instance.Width;
        else
        {
            Debug.LogError("ScoreManager - BoardManager 없음, _boardWidth 초기화 안됨");
        }
    }

    private void OnDisable()
    {
        InGameManager.OnBlockSettled -= HandleBlockPlaced;
        InGameManager.OnResetGame -= ResetScore;
        InGameManager.OnGameOver -= CheckHighScore;
        BoardManager.OnLinesCleared -= CalculateLineScore;
    }

    private void HandleBlockPlaced(int blockCount)
    {
        _currentPlaceCount++;

        if (_currentPlaceCount > _comboRemainCount)
        {
            _currentComboCount = 0;
        }

        AddScore(blockCount);
    }

    private void CalculateLineScore(int lines)
    {
        // 지워진 라인 기본 점수
        float baseScore = _boardWidth * lines * _lineScoreMultiplier;

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

    private void ResetScore()
    {
        _currentPlaceCount = 0;
        _currentComboCount = 0;
        CurrentScore = 0;
        OnResetScore?.Invoke();
    }

    private void CheckHighScore()
    {
        if (CurrentScore > LeaderboardManager.Instance.BestScore)
        {
            OnHighScoreUpdated?.Invoke(CurrentScore);
        }
        else
        {
            OnHighScoreUpdated?.Invoke(-1);
        }
    }
}