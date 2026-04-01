using UnityEngine;
using System.Collections.Generic;

public sealed class BoardHintController
{
    private readonly int _width;
    private readonly int _height;
    private readonly HintBoardCell[,] _hintCells;
    private readonly BoardModel _model;

    private DraggableBlock _prevBlock;

    public BoardHintController(int width, int height, HintBoardCell[,] hintCells, BoardModel model)
    {
        _width = width;
        _height = height;
        _hintCells = hintCells;
        _model = model;
    }

    public void ShowHint(bool showHint, DraggableBlock block, bool isPlaced = false)
    {
        if (block == null)
            return;

        if (showHint)
            ApplyHint(_model.LastPlaceableBasePos, block.CurrentOffsets, true);

        else if (isPlaced)
            ApplyHint(_model.LastPlaceableBasePos, block.CurrentOffsets, false);

        else if (_prevBlock != null && block != _prevBlock)
            ApplyHint(_model.LastPlaceableBasePos, _prevBlock.CurrentOffsets, false);

        _prevBlock = block;
    }

    private void ApplyHint(Vector2Int basePos, Vector2Int[] offsets, bool show)
    {
        if (offsets == null || offsets.Length == 0)
            return;

        List<Vector2Int> ordered = new List<Vector2Int>(offsets);
        ordered.Sort((a, b) =>
        {
            int yCmp = b.y.CompareTo(a.y);
            if (yCmp != 0) return yCmp;
            return a.x.CompareTo(b.x);
        });

        for (int i = 0; i < ordered.Count; i++)
        {
            int tx = basePos.x + ordered[i].x;
            int ty = basePos.y - ordered[i].y;

            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                continue;

            _hintCells[tx, ty].ShowHint(show);
        }
    }
}

