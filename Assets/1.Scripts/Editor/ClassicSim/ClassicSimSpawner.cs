using System;
using UnityEngine;

/// <summary>
/// BlockShape 가중치 + 랜덤 회전 + 점유율 감쇠를 DraggableBlock / InGameManager와 같은 규칙으로 복제한다.
/// </summary>
public sealed class ClassicSimSpawner
{
    private readonly ClassicSimConfig _config;
    private readonly System.Random _random;

    public ClassicSimSpawner(ClassicSimConfig config, System.Random random)
    {
        _config = config;
        _random = random;
    }

    public bool ShouldReduceLargeShape(ClassicSimBoard board)
    {
        return board.FillRatio() >= _config.LargeShapeSpawnReduceStartFillRatio;
    }

    public ClassicSimPiece Spawn(bool reduceLargeShape)
    {
        ClassicSimShapeDef def = PickWeighted(_config.Shapes, reduceLargeShape);
        Vector2Int[] offsets = CloneOffsets(def.Offsets);
        ApplyRandomRotation(offsets);

        return new ClassicSimPiece
        {
            ShapeName = def.Name,
            Offsets = offsets,
            CellCount = offsets.Length,
            IsLarge = offsets.Length >= _config.LargeShapeCellThreshold
        };
    }

    private ClassicSimShapeDef PickWeighted(ClassicSimShapeDef[] shapes, bool reduceLargeShape)
    {
        float total = 0f;
        for (int i = 0; i < shapes.Length; i++)
        {
            float weight = GetAdjustedWeight(shapes[i], reduceLargeShape);
            if (weight > 0f)
                total += weight;
        }

        if (total <= 0f)
            return shapes[_random.Next(0, shapes.Length)];

        double roll = _random.NextDouble() * total;
        float acc = 0f;
        for (int i = 0; i < shapes.Length; i++)
        {
            float weight = GetAdjustedWeight(shapes[i], reduceLargeShape);
            if (weight <= 0f)
                continue;

            acc += weight;
            if (roll <= acc)
                return shapes[i];
        }

        for (int i = shapes.Length - 1; i >= 0; i--)
        {
            if (GetAdjustedWeight(shapes[i], reduceLargeShape) > 0f)
                return shapes[i];
        }

        return shapes[shapes.Length - 1];
    }

    public float GetAdjustedWeight(ClassicSimShapeDef shape, bool reduceLargeShape)
    {
        if (shape == null)
            return 0f;

        float weight = shape.Weight;
        if (!reduceLargeShape)
            return weight;

        if (shape.CellCount >= _config.LargeShapeCellThreshold)
            weight *= _config.HighFillLargeShapeWeightMultiplier;

        return weight;
    }

    private void ApplyRandomRotation(Vector2Int[] offsets)
    {
        int randomRot = _random.Next(0, 4);
        if (randomRot == 0)
            return;

        for (int i = 0; i < offsets.Length; i++)
            offsets[i] = Rotate(randomRot, offsets[i]);

        NormalizeOffsets(offsets);
    }

    private static Vector2Int Rotate(int randomRot, Vector2Int offset)
    {
        switch (randomRot)
        {
            case 1:
                return new Vector2Int(offset.y, -offset.x);
            case 2:
                return new Vector2Int(-offset.x, -offset.y);
            case 3:
                return new Vector2Int(-offset.y, offset.x);
            default:
                return offset;
        }
    }

    private static void NormalizeOffsets(Vector2Int[] offsets)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        for (int i = 0; i < offsets.Length; i++)
        {
            if (offsets[i].x < minX)
                minX = offsets[i].x;
            if (offsets[i].y < minY)
                minY = offsets[i].y;
        }

        for (int i = 0; i < offsets.Length; i++)
            offsets[i] = new Vector2Int(offsets[i].x - minX, offsets[i].y - minY);
    }

    private static Vector2Int[] CloneOffsets(Vector2Int[] source)
    {
        Vector2Int[] clone = new Vector2Int[source.Length];
        Array.Copy(source, clone, source.Length);
        return clone;
    }
}
