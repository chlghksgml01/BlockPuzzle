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
    private readonly HashSet<int> _fullRowSet = new HashSet<int>();
    private readonly HashSet<int> _fullColSet = new HashSet<int>();
    private readonly HashSet<Vector2Int> _clearedCellKeys = new HashSet<Vector2Int>();
    private readonly List<BoardCell> _grassCandidates = new List<BoardCell>();
    private readonly List<BoardCell> _spreadTargets = new List<BoardCell>();

    private static readonly Vector2Int[] OrthogonalOffsets =
    {
        new Vector2Int(0, -1),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0)
    };

    public Vector2Int LastPlaceableBasePos { get; private set; }

    private readonly Action<int, IReadOnlyList<int>, IReadOnlyList<int>> _onLinesCleared;
    private Func<string, Sprite> _spriteResolver;

    public BoardModel(int width, int height, BoardCell[,] cells, Action<int, IReadOnlyList<int>, IReadOnlyList<int>> onLinesCleared)
    {
        _width = width;
        _height = height;
        _cells = cells;
        _onLinesCleared = onLinesCleared;
        LastPlaceableBasePos = new Vector2Int(-1, -1);
    }

    public void SetSpriteResolver(Func<string, Sprite> spriteResolver)
    {
        _spriteResolver = spriteResolver;
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

    /// <returns>클리어된 줄에 grass가 하나라도 포함되었으면 true.</returns>
    public bool ProcessFullLines()
    {
        UpdateFullLinesState();
        bool containedGrass = ClearedLinesContainGrass();
        RemoveFullLines();
        return containedGrass;
    }

    public bool HasAnyGrass()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_cells[x, y].IsGrass)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 보드의 grass 하나를 골라, 상하좌우 중 미션 블록이 아닌 칸을 grass01로 바꾼다.
    /// </summary>
    public bool TrySpreadGrass(float appearDuration)
    {
        if (_spriteResolver == null)
            return false;

        string grass01Name = BoardCell.GetStagedSpriteName("grass", 1);
        Sprite grassSprite = _spriteResolver.Invoke(grass01Name);
        if (grassSprite == null)
        {
            Debug.LogWarning($"[BoardModel] grass 전파 스프라이트를 찾지 못했습니다: {grass01Name}");
            return false;
        }

        CollectGrassCellsWithSpreadTargets();
        if (_grassCandidates.Count == 0)
            return false;

        BoardCell source = _grassCandidates[UnityEngine.Random.Range(0, _grassCandidates.Count)];
        CollectSpreadTargetsAround(source);
        if (_spreadTargets.Count == 0)
            return false;

        BoardCell target = _spreadTargets[UnityEngine.Random.Range(0, _spreadTargets.Count)];
        target.SetStageSprite(grassSprite);
        if (appearDuration > 0f)
            target.PlayAppearTween(appearDuration);

        return true;
    }

    private bool ClearedLinesContainGrass()
    {
        for (int i = 0; i < _fullRow.Count; i++)
        {
            int row = _fullRow[i];
            for (int x = 0; x < _width; x++)
            {
                if (_cells[x, row].IsGrass)
                    return true;
            }
        }

        for (int i = 0; i < _fullCol.Count; i++)
        {
            int col = _fullCol[i];
            for (int y = 0; y < _height; y++)
            {
                if (_cells[col, y].IsGrass)
                    return true;
            }
        }

        return false;
    }

    private void CollectGrassCellsWithSpreadTargets()
    {
        _grassCandidates.Clear();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                BoardCell cell = _cells[x, y];
                if (!cell.IsGrass)
                    continue;

                CollectSpreadTargetsAround(cell);
                if (_spreadTargets.Count > 0)
                    _grassCandidates.Add(cell);
            }
        }
    }

    private void CollectSpreadTargetsAround(BoardCell source)
    {
        _spreadTargets.Clear();
        if (source == null)
            return;

        for (int i = 0; i < OrthogonalOffsets.Length; i++)
        {
            Vector2Int offset = OrthogonalOffsets[i];
            int tx = source._x + offset.x;
            int ty = source._y + offset.y;
            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                continue;

            BoardCell neighbor = _cells[tx, ty];
            if (neighbor.IsMissionBlock)
                continue;

            _spreadTargets.Add(neighbor);
        }
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
        _fullRowSet.Clear();
        _fullColSet.Clear();
        _clearedCellKeys.Clear();

        for (int i = 0; i < _fullRow.Count; i++)
            _fullRowSet.Add(_fullRow[i]);
        for (int i = 0; i < _fullCol.Count; i++)
            _fullColSet.Add(_fullCol[i]);

        foreach (int row in _fullRow)
        {
            for (int x = 0; x < _width; x++)
                ProcessClearedCell(x, row);
        }

        foreach (int col in _fullCol)
        {
            for (int y = 0; y < _height; y++)
                ProcessClearedCell(col, y);
        }
    }

    /// <summary>
    /// 클리어된 행/열에 속한 셀을 한 번만 처리한다.
    /// ice/grass는 속한 줄 수만큼 단계가 오르고(행+열이면 +2), 그 외는 즉시 제거.
    /// </summary>
    private void ProcessClearedCell(int x, int y)
    {
        Vector2Int key = new Vector2Int(x, y);
        if (!_clearedCellKeys.Add(key))
            return;

        BoardCell cell = _cells[x, y];
        int damage = 0;
        if (_fullRowSet.Contains(y))
            damage++;
        if (_fullColSet.Contains(x))
            damage++;

        if (damage <= 0)
            return;

        string spriteName = cell.FilledSprite != null ? cell.FilledSprite.name : null;
        if (BoardCell.TryGetStagedBlockInfo(spriteName, out _, out _))
        {
            if (_spriteResolver == null || !cell.TryPlayStagedDamage(damage, _spriteResolver))
                cell.ClearAllState();
            return;
        }

        cell.ClearAllState();
    }

    /// <summary>
    /// 라인이 가득 차면 클리어 대상 (stone 포함). ice/grass는 제거 대신 단계 상승.
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

