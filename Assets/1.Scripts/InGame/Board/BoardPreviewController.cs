using System.Collections.Generic;
using UnityEngine;

public sealed class BoardPreviewController
{
    private readonly BoardModel _model;
    private readonly BoardGridMapper _mapper;
    private readonly float _keepPreviewMaxDistancePx;

    private Vector2Int _lastPreviewBasePos = new Vector2Int(-1, -1);
    private Vector2Int _lastPreviewAnchorOffset = Vector2Int.zero;
    private Vector2 _lastPreviewAnchorScreenPos = default;
    private DraggableBlock _lastPreviewBlock;
    private readonly List<BoardCell> _lastPreviewCells = new List<BoardCell>();

    public BoardPreviewController(BoardModel model, BoardGridMapper mapper, float keepPreviewMaxDistancePx)
    {
        _model = model;
        _mapper = mapper;
        _keepPreviewMaxDistancePx = keepPreviewMaxDistancePx;
    }

    public bool HasLastPreview => _lastPreviewBlock != null && _lastPreviewCells.Count > 0;

    public bool UpdatePreview(DraggableBlock block, Vector2 anchorScreenPos, Vector2Int anchorOffset, Camera uiCam, out bool canPlace, out List<BoardCell> lastPreviewCells)
    {
        canPlace = false;
        _lastPreviewCells.Clear();
        lastPreviewCells = _lastPreviewCells;

        if (block == null || block.CurrentOffsets == null || block.CurrentOffsets.Length == 0)
        {
            bool wasShowing = HasLastPreview;
            Clear();
            return wasShowing;
        }

        bool isSameBlock = _lastPreviewBlock == block;
        if (!isSameBlock)
            ClearAllPreviewOnly();

        if (!_mapper.TryGetCellIndexFromScreen(anchorScreenPos, uiCam, out int anchorX, out int anchorY))
        {
            if (isSameBlock && HasLastPreview)
            {
                if (IsTooFarFromLastPreview(anchorScreenPos, uiCam))
                {
                    Clear();
                    return true;
                }

                canPlace = true;
                return false;
            }

            bool wasShowing = HasLastPreview;
            Clear();
            return wasShowing;
        }

        int baseX = anchorX - anchorOffset.x;
        int baseY = anchorY + anchorOffset.y;

        if (!_model.CanPlaceAt(baseX, baseY, block.CurrentOffsets, _lastPreviewCells))
        {
            if (isSameBlock && HasLastPreview)
            {
                if (IsTooFarFromLastPreview(anchorScreenPos, uiCam))
                {
                    Clear();
                    return true;
                }

                canPlace = true;
                return false;
            }

            Clear();
            return false;
        }

        bool isPosChanged = (baseX != _lastPreviewBasePos.x || baseY != _lastPreviewBasePos.y || !isSameBlock);

        // 프리뷰 갱신
        ClearAllPreviewOnly();

        foreach (BoardCell cell in _lastPreviewCells)
        {
            cell.UpdateCellVisual(true, block.BlockSprite);
            cell.SetPreviewFilled(true);
        }

        _lastPreviewBasePos = new Vector2Int(baseX, baseY);
        _lastPreviewAnchorOffset = anchorOffset;
        _lastPreviewAnchorScreenPos = anchorScreenPos;
        _lastPreviewBlock = block;
        canPlace = true;
        return isPosChanged;
    }

    public bool PlaceLastPreview(DraggableBlock block, Sprite blockSprite, out int placedCount)
    {
        placedCount = 0;

        if (!HasLastPreview)
            return false;

        if (block == null || block != _lastPreviewBlock)
            return false;

        if (_lastPreviewBasePos.x < 0 || _lastPreviewBasePos.y < 0)
            return false;

        if (block.CurrentOffsets == null || block.CurrentOffsets.Length == 0)
            return false;

        if (!_model.CanPlaceAt(_lastPreviewBasePos.x, _lastPreviewBasePos.y, block.CurrentOffsets, _lastPreviewCells))
            return false;

        foreach (BoardCell cell in _lastPreviewCells)
            cell.PlaceBlock(blockSprite);

        placedCount = _lastPreviewCells.Count;
        ClearAllPreviewOnly();
        ClearStateOnly();
        return true;
    }

    public void Clear()
    {
        ClearAllPreviewOnly();
        ClearStateOnly();
    }

    private void ClearAllPreviewOnly()
    {
        BoardCell[,] cells = _model.Cells;
        foreach (BoardCell cell in cells)
        {
            cell.UpdateCellVisual(false);
            cell.SetPreviewFilled(false);
        }
    }

    private void ClearStateOnly()
    {
        _lastPreviewBasePos = new Vector2Int(-1, -1);
        _lastPreviewAnchorOffset = Vector2Int.zero;
        _lastPreviewAnchorScreenPos = default;
        _lastPreviewBlock = null;
        _lastPreviewCells.Clear();
    }

    // 마지막 프리뷰 위치에서 멀리 떨어졌는지 확인
    private bool IsTooFarFromLastPreview(Vector2 currentAnchorScreenPos, Camera uiCam)
    {
        if (_keepPreviewMaxDistancePx <= 0f)
            return false;

        if (!HasLastPreview)
            return true;

        float dist = Vector2.Distance(currentAnchorScreenPos, _lastPreviewAnchorScreenPos);
        return dist > _keepPreviewMaxDistancePx;
    }
}

