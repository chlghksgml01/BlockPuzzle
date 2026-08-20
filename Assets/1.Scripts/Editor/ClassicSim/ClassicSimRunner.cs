using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Classic 한 판의 결과.
/// </summary>
public sealed class ClassicSimGameResult
{
    public int Turns;
    public int Score;
    public float DeathFillRatio;
    public bool TimedOut;
}

/// <summary>
/// N판 집계 결과. 스폰 비율과 생존 통계를 담는다.
/// </summary>
public sealed class ClassicSimRunResult
{
    public int RequestedGames;
    public int CompletedGames;
    public int TimedOutGames;
    public int Seed;
    public float ElapsedSeconds;

    public float AvgTurns;
    public float MedianTurns;
    public int MinTurns;
    public int MaxTurns;

    public float AvgScore;
    public float MedianScore;
    public int MinScore;
    public int MaxScore;

    public float AvgDeathFillRatio;
    public ClassicSimShapeStat[] ShapeStats = Array.Empty<ClassicSimShapeStat>();
}

public sealed class ClassicSimShapeStat
{
    public string Name;
    public int CellCount;
    public float BaseWeight;
    public float ExpectedEmptyPercent;
    public float ExpectedHighFillPercent;
    public int SpawnCount;
    public int SpawnCountLowFill;
    public int SpawnCountHighFill;
    public float ActualPercent;
}

/// <summary>
/// Classic 게임을 N판 돌리고 통계를 모은다.
/// </summary>
public sealed class ClassicSimRunner
{
    public ClassicSimRunResult Run(ClassicSimConfig config, Func<int, int, bool> shouldCancel = null)
    {
        int seed = config.Seed != 0 ? config.Seed : Environment.TickCount;
        System.Random random = new System.Random(seed);
        ClassicSimSpawner spawner = new ClassicSimSpawner(config, random);
        ClassicSimBot bot = new ClassicSimBot(config.BoardSize);
        ClassicSimBoard board = new ClassicSimBoard(config.BoardSize);
        ClassicSimScore score = new ClassicSimScore(config);
        List<ClassicSimPiece> slots = new List<ClassicSimPiece>(config.SlotCount);

        Dictionary<string, int> spawnCounts = new Dictionary<string, int>();
        Dictionary<string, int> spawnLow = new Dictionary<string, int>();
        Dictionary<string, int> spawnHigh = new Dictionary<string, int>();
        EnsureShapeKeys(config, spawnCounts, spawnLow, spawnHigh);

        List<int> turnsList = new List<int>(config.GameCount);
        List<int> scoreList = new List<int>(config.GameCount);
        float fillSum = 0f;
        int timedOut = 0;
        DateTime started = DateTime.UtcNow;

        for (int gameIndex = 0; gameIndex < config.GameCount; gameIndex++)
        {
            if (shouldCancel != null && shouldCancel(gameIndex, config.GameCount))
                break;

            ClassicSimGameResult game = PlayOne(
                config, board, score, spawner, bot, slots,
                spawnCounts, spawnLow, spawnHigh);

            turnsList.Add(game.Turns);
            scoreList.Add(game.Score);
            fillSum += game.DeathFillRatio;
            if (game.TimedOut)
                timedOut++;
        }

        ClassicSimRunResult result = BuildResult(config, seed, started, turnsList, scoreList, fillSum, timedOut);
        result.ShapeStats = BuildShapeStats(config, spawner, spawnCounts, spawnLow, spawnHigh);
        return result;
    }

    private static ClassicSimGameResult PlayOne(
        ClassicSimConfig config,
        ClassicSimBoard board,
        ClassicSimScore score,
        ClassicSimSpawner spawner,
        ClassicSimBot bot,
        List<ClassicSimPiece> slots,
        Dictionary<string, int> spawnCounts,
        Dictionary<string, int> spawnLow,
        Dictionary<string, int> spawnHigh)
    {
        board.Clear();
        score.Reset();
        slots.Clear();
        RefillSlots(config, board, spawner, slots, spawnCounts, spawnLow, spawnHigh);

        int turns = 0;
        bool timedOut = false;
        while (turns < config.MaxTurns)
        {
            if (!HasAnyPlaceable(board, slots))
                break;

            int slotIndex;
            int baseX;
            int baseY;
            if (!bot.TryPickMove(board, slots, out slotIndex, out baseX, out baseY))
                break;

            ClassicSimPiece piece = slots[slotIndex];
            board.PlaceAt(baseX, baseY, piece.Offsets);
            int lines = board.ClearFullLines();
            if (lines > 0)
                score.CalculateLineScore(lines);
            score.HandleBlockPlaced(piece.CellCount);

            slots.RemoveAt(slotIndex);
            turns++;

            if (slots.Count == 0)
                RefillSlots(config, board, spawner, slots, spawnCounts, spawnLow, spawnHigh);
        }

        if (turns >= config.MaxTurns && HasAnyPlaceable(board, slots))
            timedOut = true;

        return new ClassicSimGameResult
        {
            Turns = turns,
            Score = score.CurrentScore,
            DeathFillRatio = board.FillRatio(),
            TimedOut = timedOut
        };
    }

    private static void RefillSlots(
        ClassicSimConfig config,
        ClassicSimBoard board,
        ClassicSimSpawner spawner,
        List<ClassicSimPiece> slots,
        Dictionary<string, int> spawnCounts,
        Dictionary<string, int> spawnLow,
        Dictionary<string, int> spawnHigh)
    {
        bool reduceLarge = spawner.ShouldReduceLargeShape(board);
        for (int i = 0; i < config.SlotCount; i++)
        {
            ClassicSimPiece piece = spawner.Spawn(reduceLarge);
            slots.Add(piece);
            spawnCounts[piece.ShapeName] = spawnCounts[piece.ShapeName] + 1;
            if (reduceLarge)
                spawnHigh[piece.ShapeName] = spawnHigh[piece.ShapeName] + 1;
            else
                spawnLow[piece.ShapeName] = spawnLow[piece.ShapeName] + 1;
        }
    }

    private static bool HasAnyPlaceable(ClassicSimBoard board, List<ClassicSimPiece> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (board.CanPlaceShape(slots[i].Offsets))
                return true;
        }

        return false;
    }

    private static void EnsureShapeKeys(
        ClassicSimConfig config,
        Dictionary<string, int> spawnCounts,
        Dictionary<string, int> spawnLow,
        Dictionary<string, int> spawnHigh)
    {
        for (int i = 0; i < config.Shapes.Length; i++)
        {
            string name = config.Shapes[i].Name;
            spawnCounts[name] = 0;
            spawnLow[name] = 0;
            spawnHigh[name] = 0;
        }
    }

    private static ClassicSimRunResult BuildResult(
        ClassicSimConfig config,
        int seed,
        DateTime started,
        List<int> turnsList,
        List<int> scoreList,
        float fillSum,
        int timedOut)
    {
        int completed = turnsList.Count;
        ClassicSimRunResult result = new ClassicSimRunResult
        {
            RequestedGames = config.GameCount,
            CompletedGames = completed,
            TimedOutGames = timedOut,
            Seed = seed,
            ElapsedSeconds = (float)(DateTime.UtcNow - started).TotalSeconds
        };

        if (completed == 0)
            return result;

        result.AvgTurns = Average(turnsList);
        result.MedianTurns = Median(turnsList);
        result.MinTurns = Min(turnsList);
        result.MaxTurns = Max(turnsList);
        result.AvgScore = Average(scoreList);
        result.MedianScore = Median(scoreList);
        result.MinScore = Min(scoreList);
        result.MaxScore = Max(scoreList);
        result.AvgDeathFillRatio = fillSum / completed;
        return result;
    }

    private static ClassicSimShapeStat[] BuildShapeStats(
        ClassicSimConfig config,
        ClassicSimSpawner spawner,
        Dictionary<string, int> spawnCounts,
        Dictionary<string, int> spawnLow,
        Dictionary<string, int> spawnHigh)
    {
        float emptyTotal = 0f;
        float highTotal = 0f;
        int spawnTotal = 0;
        for (int i = 0; i < config.Shapes.Length; i++)
        {
            ClassicSimShapeDef shape = config.Shapes[i];
            emptyTotal += spawner.GetAdjustedWeight(shape, false);
            highTotal += spawner.GetAdjustedWeight(shape, true);
            spawnTotal += spawnCounts[shape.Name];
        }

        ClassicSimShapeStat[] stats = new ClassicSimShapeStat[config.Shapes.Length];
        for (int i = 0; i < config.Shapes.Length; i++)
        {
            ClassicSimShapeDef shape = config.Shapes[i];
            int spawned = spawnCounts[shape.Name];
            stats[i] = new ClassicSimShapeStat
            {
                Name = shape.Name,
                CellCount = shape.CellCount,
                BaseWeight = shape.Weight,
                ExpectedEmptyPercent = emptyTotal > 0f ? spawner.GetAdjustedWeight(shape, false) / emptyTotal * 100f : 0f,
                ExpectedHighFillPercent = highTotal > 0f ? spawner.GetAdjustedWeight(shape, true) / highTotal * 100f : 0f,
                SpawnCount = spawned,
                SpawnCountLowFill = spawnLow[shape.Name],
                SpawnCountHighFill = spawnHigh[shape.Name],
                ActualPercent = spawnTotal > 0 ? spawned / (float)spawnTotal * 100f : 0f
            };
        }

        return stats;
    }

    public static string FormatReport(ClassicSimRunResult result, ClassicSimConfig config)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Classic 스폰 시뮬 리포트");
        sb.AppendLine("(탐욕 봇 기준. 사람 점수/생존과 직접 비교하지 말 것. 가중치 A/B용.)");
        sb.AppendLine();
        sb.AppendLine($"시드: {result.Seed}");
        sb.AppendLine($"보드: {config.BoardSize}x{config.BoardSize}, 슬롯 {config.SlotCount}");
        sb.AppendLine($"시행: {result.CompletedGames}/{result.RequestedGames}판, 상한 {config.MaxTurns}턴");
        sb.AppendLine($"소요: {result.ElapsedSeconds:0.00}초");
        sb.AppendLine($"턴 상한 도달: {result.TimedOutGames}");
        sb.AppendLine();
        sb.AppendLine($"평균 생존 턴: {result.AvgTurns:0.0}  (중앙 {result.MedianTurns:0.0}, 최소 {result.MinTurns}, 최대 {result.MaxTurns})");
        sb.AppendLine($"평균 점수: {result.AvgScore:0.0}  (중앙 {result.MedianScore:0.0}, 최소 {result.MinScore}, 최대 {result.MaxScore})");
        sb.AppendLine($"종료 시 평균 보드 점유율: {result.AvgDeathFillRatio * 100f:0.0}%");
        sb.AppendLine();
        sb.AppendLine("스폰 비율  실제%  | 빈보드기대% | 50%이상기대% | 칸수  가중치  이름");
        sb.AppendLine("--------------------------------------------------------------");

        for (int i = 0; i < result.ShapeStats.Length; i++)
        {
            ClassicSimShapeStat stat = result.ShapeStats[i];
            sb.AppendLine(
                $"{stat.ActualPercent,7:0.0}% | {stat.ExpectedEmptyPercent,11:0.0}% | {stat.ExpectedHighFillPercent,12:0.0}% | " +
                $"{stat.CellCount,3}  {stat.BaseWeight,6:0.#}  {stat.Name}  " +
                $"(저점유 {stat.SpawnCountLowFill}, 고점유 {stat.SpawnCountHighFill})");
        }

        return sb.ToString();
    }

    private static float Average(List<int> values)
    {
        long sum = 0;
        for (int i = 0; i < values.Count; i++)
            sum += values[i];
        return sum / (float)values.Count;
    }

    private static float Median(List<int> values)
    {
        int[] copy = values.ToArray();
        Array.Sort(copy);
        int mid = copy.Length / 2;
        if (copy.Length % 2 == 1)
            return copy[mid];
        return (copy[mid - 1] + copy[mid]) / 2f;
    }

    private static int Min(List<int> values)
    {
        int min = values[0];
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] < min)
                min = values[i];
        }

        return min;
    }

    private static int Max(List<int> values)
    {
        int max = values[0];
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] > max)
                max = values[i];
        }

        return max;
    }
}
