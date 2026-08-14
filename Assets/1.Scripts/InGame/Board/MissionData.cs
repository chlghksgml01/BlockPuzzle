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

    [Tooltip("미션 종류")]
    public MissionType missionType;

    [Header("Gem Mission")]
    [Tooltip("수집할 Gem 종류별 목표 개수 (슬롯 DraggableBlock에 스폰됨)")]
    public List<GemTargetInfo> gemTargets = new List<GemTargetInfo>();

    [Header("Score Goal")]
    [Tooltip("클리어에 필요한 목표 점수 (ScoreGoal 미션용)")]
    public int targetScore;

    [HideInInspector]
    [Tooltip("레거시 필드. ScoreGoal은 시간 제한 없이 목표 점수만 사용한다.")]
    public float timeLimitSeconds;

    public bool IsHard => isHard;
    public MissionType MissionType => missionType;
    public int TargetScore => targetScore;

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

    /// <summary>Gem 미션 목표 목록. gemTargets 우선, 없으면 레거시 filledCells에서 집계.</summary>
    public List<GemTargetInfo> BuildGemTargets()
    {
        List<GemTargetInfo> result = new List<GemTargetInfo>(3);
        if (gemTargets != null)
        {
            for (int i = 0; i < gemTargets.Count; i++)
            {
                GemTargetInfo target = gemTargets[i];
                if (target.count <= 0)
                    continue;

                result.Add(target);
            }
        }

        if (result.Count > 0)
            return result;

        return BuildGemTargetsFromFilledCellsLegacy();
    }

    /// <summary>레거시: 보드 filledCells에 배치된 Gem 개수로 목표를 만든다.</summary>
    private List<GemTargetInfo> BuildGemTargetsFromFilledCellsLegacy()
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
