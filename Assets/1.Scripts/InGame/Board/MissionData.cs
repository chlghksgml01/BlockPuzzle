using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BlockPuzzle/Mission Data", fileName = "MissionData")]
public class MissionData : ScriptableObject
{
    [Header("Board Layout")]
    [Tooltip("보드 한 변의 칸 수")]
    public int boardSize = 9;

    [Tooltip("채워진 보드 칸 목록")]
    public List<FilledCellData> filledCells = new List<FilledCellData>();

    [Header("Mission Info")]
    [Tooltip("하드 미션 여부")]
    public bool isHard;

    [Tooltip("클리어 여부")]
    public bool isClear;

    [Tooltip("미션 종류")]
    public MissionType missionType;

    [Header("Score Goal")]
    [Tooltip("클리어에 필요한 목표 점수 (ScoreGoal 미션용)")]
    public int targetScore;

    [Tooltip("클리어 제한 시간 초 (ScoreGoal 미션용)")]
    public float timeLimitSeconds;

    public bool IsHard => isHard;
    public bool IsClear => isClear;
    public MissionType MissionType => missionType;
    public int TargetScore => targetScore;
    public float TimeLimitSeconds => timeLimitSeconds;

    /// <summary>보드에 배치된 ice 셀 개수.</summary>
    public int CountIceCells()
    {
        return CountCellsByPredicate(BoardCell.IsIceSpriteName);
    }

    /// <summary>보드에 배치된 grass 셀 개수.</summary>
    public int CountGrassCells()
    {
        return CountCellsByPredicate(BoardCell.IsGrassSpriteName);
    }

    /// <summary>보드의 Pentagon/Square/Star 개수로 Gem 목표 목록을 만든다. 0개인 종류는 제외.</summary>
    public List<GemTargetInfo> BuildGemTargets()
    {
        int pentagon = 0;
        int square = 0;
        int star = 0;

        if (filledCells != null)
        {
            for (int i = 0; i < filledCells.Count; i++)
            {
                FilledCellData cell = filledCells[i];
                if (cell == null || string.IsNullOrEmpty(cell.spriteName))
                    continue;

                string spriteName = cell.spriteName;
                if (spriteName.IndexOf("pentagon", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    pentagon++;
                else if (spriteName.IndexOf("square", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    square++;
                else if (spriteName.IndexOf("star", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    star++;
            }
        }

        List<GemTargetInfo> result = new List<GemTargetInfo>(3);
        if (pentagon > 0)
            result.Add(new GemTargetInfo { gemType = GemType.Pentagon, count = pentagon });
        if (square > 0)
            result.Add(new GemTargetInfo { gemType = GemType.Square, count = square });
        if (star > 0)
            result.Add(new GemTargetInfo { gemType = GemType.Star, count = star });

        return result;
    }

    private int CountCellsByPredicate(System.Func<string, bool> predicate)
    {
        if (filledCells == null || predicate == null)
            return 0;

        int count = 0;
        for (int i = 0; i < filledCells.Count; i++)
        {
            FilledCellData cell = filledCells[i];
            if (cell == null)
                continue;

            if (predicate(cell.spriteName))
                count++;
        }

        return count;
    }
}
