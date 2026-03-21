using UnityEngine;

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

        for (int i = 0; i < offsets.Length; i++)
        {
            int tx = basePos.x + offsets[i].x;
            int ty = basePos.y - offsets[i].y;

            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                continue;

            _hintCells[tx, ty].ShowHint(show);
        }
    }
}

