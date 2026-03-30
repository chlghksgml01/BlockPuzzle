using System;
using UnityEngine;

[DefaultExecutionOrder(-80)]
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

    public event Action<int, int> OnScoreChanged;
    public event Action OnResetScore;
    public event Action<int, int> OnComboScore;
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
        int comboBonusAddedScore = 0;
        int comboCountForUI = 0;
        if (_currentPlaceCount <= _comboRemainCount)
        {
            _currentPlaceCount = 0;

            if (_currentComboCount >= 1)
            {
                float comboBonus = Mathf.Clamp(_comboScoreMultiplier * _currentComboCount, 0f, 0.5f);
                comboMultiplier = 1f + comboBonus;
                comboCountForUI = _currentComboCount + 1;
            }
            _currentComboCount++;
        }
        else
            _currentComboCount = 0;

        float noComboScoreF = baseScore * 1f * multiLineBonusMultiplier;
        float totalScoreF = baseScore * comboMultiplier * multiLineBonusMultiplier;

        int totalScore = Mathf.FloorToInt(totalScoreF);
        if (comboMultiplier > 1f)
        {
            int noComboScore = Mathf.FloorToInt(noComboScoreF);
            comboBonusAddedScore = Mathf.Max(0, totalScore - noComboScore);
        }

        AddScore(totalScore);

        if (comboBonusAddedScore > 0)
        {
            OnComboScore?.Invoke(comboBonusAddedScore, comboCountForUI);
        }
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