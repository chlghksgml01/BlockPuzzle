using UnityEngine;

/// <summary>
/// Classic 보드 상태. UI/연출 없이 점유와 줄 클리어만 처리한다.
/// 좌표 매핑은 BoardModel과 같다: tx = baseX + offset.x, ty = baseY - offset.y.
/// </summary>
public sealed class ClassicSimBoard
{
    private readonly int _size;
    private readonly bool[,] _filled;

    public ClassicSimBoard(int size)
    {
        _size = size;
        _filled = new bool[size, size];
    }

    public int Size => _size;
    public int TotalCells => _size * _size;

    public int CountFilled()
    {
        int count = 0;
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                if (_filled[x, y])
                    count++;
            }
        }

        return count;
    }

    public float FillRatio()
    {
        return (float)CountFilled() / TotalCells;
    }

    public void Clear()
    {
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
                _filled[x, y] = false;
        }
    }

    public void CopyFrom(ClassicSimBoard other)
    {
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
                _filled[x, y] = other._filled[x, y];
        }
    }

    public bool CanPlaceAt(int baseX, int baseY, Vector2Int[] offsets)
    {
        if (offsets == null || offsets.Length == 0)
            return false;

        for (int i = 0; i < offsets.Length; i++)
        {
            int tx = baseX + offsets[i].x;
            int ty = baseY - offsets[i].y;
            if (tx < 0 || tx >= _size || ty < 0 || ty >= _size)
                return false;
            if (_filled[tx, ty])
                return false;
        }

        return true;
    }

    public bool CanPlaceShape(Vector2Int[] offsets)
    {
        if (offsets == null || offsets.Length == 0)
            return false;

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                if (!_filled[x, y] && CanPlaceAt(x, y, offsets))
                    return true;
            }
        }

        return false;
    }

    public void PlaceAt(int baseX, int baseY, Vector2Int[] offsets)
    {
        for (int i = 0; i < offsets.Length; i++)
        {
            int tx = baseX + offsets[i].x;
            int ty = baseY - offsets[i].y;
            _filled[tx, ty] = true;
        }
    }

    /// <returns>클리어된 행 수 + 열 수.</returns>
    public int ClearFullLines()
    {
        bool[] fullRows = new bool[_size];
        bool[] fullCols = new bool[_size];
        int lines = 0;

        for (int y = 0; y < _size; y++)
        {
            bool full = true;
            for (int x = 0; x < _size; x++)
            {
                if (!_filled[x, y])
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                fullRows[y] = true;
                lines++;
            }
        }

        for (int x = 0; x < _size; x++)
        {
            bool full = true;
            for (int y = 0; y < _size; y++)
            {
                if (!_filled[x, y])
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                fullCols[x] = true;
                lines++;
            }
        }

        if (lines == 0)
            return 0;

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                if (fullRows[y] || fullCols[x])
                    _filled[x, y] = false;
            }
        }

        return lines;
    }

    /// <summary>클리어 후 6칸 이상 찬 행/열 개수. 봇 동점 처리용.</summary>
    public int CountNearFullLines(int minFilled)
    {
        int count = 0;
        for (int y = 0; y < _size; y++)
        {
            int filled = 0;
            for (int x = 0; x < _size; x++)
            {
                if (_filled[x, y])
                    filled++;
            }

            if (filled >= minFilled && filled < _size)
                count++;
        }

        for (int x = 0; x < _size; x++)
        {
            int filled = 0;
            for (int y = 0; y < _size; y++)
            {
                if (_filled[x, y])
                    filled++;
            }

            if (filled >= minFilled && filled < _size)
                count++;
        }

        return count;
    }
}
