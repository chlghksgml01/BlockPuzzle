using System;
using UnityEngine;

/// <summary>
/// Classic 헤드리스 시뮬 설정. 런타임 기본값(9x9, 슬롯 3, 50% 점유 시 큰 블록 감쇠)을 따른다.
/// </summary>
public sealed class ClassicSimConfig
{
    public int BoardSize = 9;
    public int SlotCount = 3;
    public int GameCount = 100;
    public int MaxTurns = 1000;
    public int Seed;

    public int LargeShapeCellThreshold = 5;
    public float HighFillLargeShapeWeightMultiplier = 0.7f;
    public float LargeShapeSpawnReduceStartFillRatio = 0.5f;

    public float LineScoreMultiplier = 5f;
    public float LineBonusMultiplier = 0.5f;
    public float ComboScoreMultiplier = 0.1f;
    public int ComboRemainCount = 5;

    public ClassicSimShapeDef[] Shapes = Array.Empty<ClassicSimShapeDef>();
}

/// <summary>
/// 스폰 풀에 들어가는 블록 형태 정의.
/// </summary>
public sealed class ClassicSimShapeDef
{
    public string Name;
    public Vector2Int[] Offsets;
    public float Weight;
    public int CellCount;
}

/// <summary>
/// 슬롯에 올라간 블록 한 개.
/// </summary>
public sealed class ClassicSimPiece
{
    public string ShapeName;
    public Vector2Int[] Offsets;
    public int CellCount;
    public bool IsLarge;
}
