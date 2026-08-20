using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 탐욕 봇. 남은 슬롯 블록 × 모든 좌표 중 점수가 가장 높은 수를 고른다.
/// 사람 실력 추정이 아니라 스폰 테이블 A/B용이다.
/// </summary>
public sealed class ClassicSimBot
{
    private const long DeathPenalty = -1000000000L;
    private const long LineWeight = 1000000L;
    private const long NearFullWeight = 1000L;

    private readonly ClassicSimBoard _scratch;

    public ClassicSimBot(int boardSize)
    {
        _scratch = new ClassicSimBoard(boardSize);
    }

    public bool TryPickMove(ClassicSimBoard board, List<ClassicSimPiece> slots, out int slotIndex, out int baseX, out int baseY)
    {
        slotIndex = -1;
        baseX = -1;
        baseY = -1;
        long bestScore = long.MinValue;
        bool found = false;
        int size = board.Size;

        for (int i = 0; i < slots.Count; i++)
        {
            ClassicSimPiece piece = slots[i];
            Vector2Int[] offsets = piece.Offsets;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (!board.CanPlaceAt(x, y, offsets))
                        continue;

                    long score = Evaluate(board, slots, i, x, y);
                    if (!found || score > bestScore)
                    {
                        found = true;
                        bestScore = score;
                        slotIndex = i;
                        baseX = x;
                        baseY = y;
                    }
                }
            }
        }

        return found;
    }

    private long Evaluate(ClassicSimBoard board, List<ClassicSimPiece> slots, int slotIndex, int baseX, int baseY)
    {
        ClassicSimPiece piece = slots[slotIndex];
        _scratch.CopyFrom(board);
        _scratch.PlaceAt(baseX, baseY, piece.Offsets);
        int lines = _scratch.ClearFullLines();

        long score = lines * LineWeight;
        score += _scratch.CountNearFullLines(6) * NearFullWeight;
        score -= _scratch.CountFilled();

        if (slots.Count > 1 && RemainingWouldBeStuck(slots, slotIndex))
            score += DeathPenalty;

        return score;
    }

    private bool RemainingWouldBeStuck(List<ClassicSimPiece> slots, int placedIndex)
    {
        bool anyRemaining = false;
        for (int i = 0; i < slots.Count; i++)
        {
            if (i == placedIndex)
                continue;

            anyRemaining = true;
            if (_scratch.CanPlaceShape(slots[i].Offsets))
                return false;
        }

        return anyRemaining;
    }
}
