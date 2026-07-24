using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BoardModel
{
    private readonly int _width;
    private readonly int _height;
    private readonly BoardCell[,] _cells;

    private readonly List<int> _fullRow = new List<int>();
    private readonly List<int> _fullCol = new List<int>();

    public Vector2Int LastPlaceableBasePos { get; private set; }

    private readonly Action<int, IReadOnlyList<int>, IReadOnlyList<int>> _onLinesCleared;

    public BoardModel(int width, int height, BoardCell[,] cells, Action<int, IReadOnlyList<int>, IReadOnlyList<int>> onLinesCleared)
    {
        _width = width;
        _height = height;
        _cells = cells;
        _onLinesCleared = onLinesCleared;
        LastPlaceableBasePos = new Vector2Int(-1, -1);
    }

    public BoardCell[,] Cells => _cells;

    public bool IsFilled(int x, int y) => _cells[x, y].IsFilled;
    public bool IsOccupied(int x, int y) => _cells[x, y].IsOccupied;

    public void ResetBoard()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
                _cells[x, y].ClearAllState();
        }
    }

    public void ProcessFullLines()
    {
        UpdateFullLinesState();
        RemoveFullLines();
    }

    public void PreviewLineClears(List<BoardCell> previewCells, Sprite blockSprite)
    {
        ClearAllLinePreviews();

        if (previewCells == null || previewCells.Count == 0)
            return;

        HashSet<int> rowsToCheck = new HashSet<int>();
        HashSet<int> colsToCheck = new HashSet<int>();

        foreach (BoardCell cell in previewCells)
        {
            rowsToCheck.Add(cell._y);
            colsToCheck.Add(cell._x);
        }

        foreach (int y in rowsToCheck)
        {
            if (!IsLineClearableRow(y, includePreview: true))
                continue;

            for (int x = 0; x < _width; x++)
                _cells[x, y].SetLinePreview(true, blockSprite);
        }

        foreach (int x in colsToCheck)
        {
            if (!IsLineClearableCol(x, includePreview: true))
                continue;

            for (int y = 0; y < _height; y++)
                _cells[x, y].SetLinePreview(true, blockSprite);
        }
    }

    private void ClearAllLinePreviews()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _cells[x, y].SetLinePreview(false);
            }
        }
    }

    private void UpdateFullLinesState()
    {
        _fullRow.Clear();
        _fullCol.Clear();

        for (int y = 0; y < _height; y++)
        {
            if (IsLineClearableRow(y, includePreview: false))
                _fullRow.Add(y);
        }

        for (int x = 0; x < _width; x++)
        {
            if (IsLineClearableCol(x, includePreview: false))
                _fullCol.Add(x);
        }

        int cleared = _fullRow.Count + _fullCol.Count;
        if (cleared > 0)
            _onLinesCleared?.Invoke(cleared, new List<int>(_fullRow), new List<int>(_fullCol));
    }

    private void RemoveFullLines()
    {
        foreach (int row in _fullRow)
        {
            for (int x = 0; x < _width; x++)
                _cells[x, row].ClearAllState();
        }

        foreach (int col in _fullCol)
        {
            for (int y = 0; y < _height; y++)
                _cells[col, y].ClearAllState();
        }
    }

    /// <summary>
    /// 라인이 가득 차면 클리어 대상 (stone 포함).
    /// </summary>
    private bool IsLineClearableRow(int y, bool includePreview)
    {
        for (int x = 0; x < _width; x++)
        {
            BoardCell cell = _cells[x, y];
            bool occupied = cell.IsOccupied || (includePreview && cell.IsPreviewFilled);
            if (!occupied)
                return false;
        }

        return true;
    }

    private bool IsLineClearableCol(int x, bool includePreview)
    {
        for (int y = 0; y < _height; y++)
        {
            BoardCell cell = _cells[x, y];
            bool occupied = cell.IsOccupied || (includePreview && cell.IsPreviewFilled);
            if (!occupied)
                return false;
        }

        return true;
    }

    public bool CanPlaceShape(Vector2Int[] shapeOffset)
    {
        if (shapeOffset == null || shapeOffset.Length == 0)
            return false;

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (!_cells[x, y].IsOccupied && CanPlaceAt(x, y, shapeOffset))
                    return true;
            }
        }

        return false;
    }

    public bool CanPlaceAt(int baseX, int baseY, Vector2Int[] shapeOffset)
    {
        if (shapeOffset == null || shapeOffset.Length == 0)
            return false;

        // DraggableBlock: offset.y 클수록 UI에서 위쪽(anchoredPosition.y↑) / 보드는 y 클수록 아래행 → Y만 반대로 매핑
        foreach (Vector2Int offset in shapeOffset)
        {
            int tx = baseX + offset.x;
            int ty = baseY - offset.y;

            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                return false;

            if (_cells[tx, ty].IsOccupied)
                return false;
        }

        LastPlaceableBasePos = new Vector2Int(baseX, baseY);
        return true;
    }

    public bool CanPlaceAt(int baseX, int baseY, Vector2Int[] shapeOffset, List<BoardCell> cells)
    {
        if (cells == null)
            return false;

        cells.Clear();

        if (shapeOffset == null || shapeOffset.Length == 0)
            return false;

        if (cells.Capacity < shapeOffset.Length)
            cells.Capacity = shapeOffset.Length;

        foreach (Vector2Int offset in shapeOffset)
        {
            int tx = baseX + offset.x;
            int ty = baseY - offset.y;

            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                return false;

            if (_cells[tx, ty].IsOccupied)
                return false;

            cells.Add(_cells[tx, ty]);
        }

        return true;
    }
}

