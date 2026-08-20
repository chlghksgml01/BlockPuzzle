using UnityEngine;

/// <summary>
/// ScoreSystem 공식을 SoundManager 없이 복제한다.
/// 호출 순서도 런타임과 같다: 줄 점수(CalculateLineScore) → 배치 점수(HandleBlockPlaced).
/// </summary>
public sealed class ClassicSimScore
{
    private readonly int _boardWidth;
    private readonly float _lineScoreMultiplier;
    private readonly float _lineBonusMultiplier;
    private readonly float _comboScoreMultiplier;
    private readonly int _comboRemainCount;

    public int CurrentScore { get; private set; }
    private int _currentPlaceCount;
    private int _currentComboCount;

    public ClassicSimScore(ClassicSimConfig config)
    {
        _boardWidth = config.BoardSize;
        _lineScoreMultiplier = config.LineScoreMultiplier;
        _lineBonusMultiplier = config.LineBonusMultiplier;
        _comboScoreMultiplier = config.ComboScoreMultiplier;
        _comboRemainCount = config.ComboRemainCount;
    }

    public void Reset()
    {
        CurrentScore = 0;
        _currentPlaceCount = 0;
        _currentComboCount = 0;
    }

    public void CalculateLineScore(int lines)
    {
        float baseScore = _boardWidth * lines * _lineScoreMultiplier;
        float multiLineBonusMultiplier = 1f + (lines - 1) * _lineBonusMultiplier;

        float comboMultiplier = 1f;
        if (_currentPlaceCount <= _comboRemainCount)
        {
            if (_currentComboCount >= 1)
            {
                float comboBonus = Mathf.Clamp(_comboScoreMultiplier * _currentComboCount, 0f, 0.5f);
                comboMultiplier = 1f + comboBonus;
            }

            _currentComboCount++;
        }
        else
        {
            _currentComboCount = 1;
            comboMultiplier = 1f;
        }

        _currentPlaceCount = 0;
        int totalScore = Mathf.FloorToInt(baseScore * comboMultiplier * multiLineBonusMultiplier);
        CurrentScore += totalScore;
    }

    public void HandleBlockPlaced(int blockCount)
    {
        _currentPlaceCount++;
        if (_currentPlaceCount > _comboRemainCount)
            _currentComboCount = 0;

        CurrentScore += blockCount;
    }
}
