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

    public void ResetBoard()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _cells[x, y].SetFilled(false);
                _cells[x, y].UpdateCellVisual(false);
            }
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
            bool isFull = true;
            for (int x = 0; x < _width; x++)
            {
                if (!_cells[x, y].IsFilled && !_cells[x, y].IsPreviewFilled)
                {
                    isFull = false;
                    break;
                }
            }

            if (isFull)
            {
                for (int x = 0; x < _width; x++)
                    _cells[x, y].SetLinePreview(true, blockSprite);
            }
        }

        foreach (int x in colsToCheck)
        {
            bool isFull = true;
            for (int y = 0; y < _height; y++)
            {
                if (!_cells[x, y].IsFilled && !_cells[x, y].IsPreviewFilled)
                {
                    isFull = false;
                    break;
                }
            }

            if (isFull)
            {
                for (int y = 0; y < _height; y++)
                    _cells[x, y].SetLinePreview(true, blockSprite);
            }
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
            bool isFull = true;
            for (int x = 0; x < _width; x++)
            {
                if (!_cells[x, y].IsFilled)
                {
                    isFull = false;
                    break;
                }
            }

            if (isFull)
                _fullRow.Add(y);
        }

        for (int x = 0; x < _width; x++)
        {
            bool isFull = true;
            for (int y = 0; y < _height; y++)
            {
                if (!_cells[x, y].IsFilled)
                {
                    isFull = false;
                    break;
                }
            }

            if (isFull)
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
            {
                _cells[x, row].SetFilled(false);
                _cells[x, row].UpdateCellVisual(false);
            }
        }

        foreach (int col in _fullCol)
        {
            for (int y = 0; y < _height; y++)
            {
                _cells[col, y].SetFilled(false);
                _cells[col, y].UpdateCellVisual(false);
            }
        }
    }

    public bool CanPlaceShape(Vector2Int[] shapeOffset)
    {
        if (shapeOffset == null || shapeOffset.Length == 0)
            return false;

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (!_cells[x, y].IsFilled && CanPlaceAt(x, y, shapeOffset))
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

            if (_cells[tx, ty].IsFilled)
                return false;
        }

        LastPlaceableBasePos = new Vector2Int(baseX, baseY);
        return true;
    }

    public bool CanPlaceAt(int baseX, int baseY, Vector2Int[] shapeOffset, out List<BoardCell> cells)
    {
        cells = new List<BoardCell>(shapeOffset?.Length ?? 0);

        if (shapeOffset == null || shapeOffset.Length == 0)
            return false;

        foreach (Vector2Int offset in shapeOffset)
        {
            int tx = baseX + offset.x;
            int ty = baseY - offset.y;

            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                return false;

            if (_cells[tx, ty].IsFilled)
                return false;

            cells.Add(_cells[tx, ty]);
        }

        return true;
    }
}

