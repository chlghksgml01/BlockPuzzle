using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IPlacementHandler
{
    public int Width { get; }
    public bool CanPlaceShape(Vector2Int[] shapeOffset);
    public void ShowHint(bool showHint, DraggableBlock block, bool isPlaced = false);
}

public class BoardManager : Singleton<BoardManager>, IPlacementHandler
{
    [Header("Board Configurations")]
    [SerializeField] private int _width = 9;
    [SerializeField] private int _height = 9;
    public int Width => _width;

    [Header("References (UI & Prefabs)")]
    [SerializeField] private RectTransform _boardRoot;
    [SerializeField] private RectTransform _hintBoardRoot;
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private GameObject _hintCellPrefab;
    [SerializeField] private Sprite _previewSprite;
    public Sprite PreviewSprite => _previewSprite;

    [Header("Visual Settings")]
    [SerializeField, Range(0f, 1f)] private float _previewAlpha = 0.6f;
    public float PreviewAlpha => _previewAlpha;

    [Header("Drag Preview Settings")]
    [SerializeField, Min(0f)] private float _keepPreviewMaxDistancePx = 180f;

    public float BoardCellSize { get; set; }
    public bool CanPlaceBlock { get; set; }
    private BoardCell[,] _cells;
    private HintBoardCell[,] _hintCells;

    private GridLayoutGroup _boardGrid;
    private Vector2Int _lastPreviewBasePos = new Vector2Int(-1, -1);
    private Vector2Int _lastPreviewAnchorOffset = Vector2Int.zero;
    private DraggableBlock _lastPreviewBlock;
    private readonly List<BoardCell> _lastPreviewCells = new List<BoardCell>();

    private List<int> _fullRow = new List<int>();
    private List<int> _fullCol = new List<int>();
    private Vector2Int _placeableCellPos;
    private DraggableBlock _prevBlock;

    public static event Action<int> OnLinesCleared;

    override protected void OnAwake()
    {
        GenerateBoard();
    }

    private void OnEnable()
    {
        InGameManager.OnBlockSettled += ProcessFullLines;
        InGameManager.OnResetGame += ResetBoard;
    }

    private void OnDisable()
    {
        InGameManager.OnBlockSettled -= ProcessFullLines;
        InGameManager.OnResetGame -= ResetBoard;
    }

    private void GenerateBoard()
    {
        _cells = new BoardCell[_width, _height];
        _hintCells = new HintBoardCell[_width, _height];
        _boardGrid = _boardRoot.GetComponent<GridLayoutGroup>();
        BoardCellSize = _boardGrid.cellSize.x;

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                GameObject cell = Instantiate(_cellPrefab, _boardRoot);
                _cells[x, y] = cell.GetComponent<BoardCell>();
                _cells[x, y].Init(x, y);

                GameObject hintCell = Instantiate(_hintCellPrefab, _hintBoardRoot);
                _hintCells[x, y] = hintCell.GetComponent<HintBoardCell>();
            }
        }
    }

    public bool IsFilled(int x, int y)
    {
        return _cells[x, y].IsFilled;
    }

    public void SetFilled(int x, int y, bool filled)
    {
        _cells[x, y].SetFilled(filled);
    }

    public void ClearAllPreview()
    {
        foreach (BoardCell cell in _cells)
            cell.UpdateCellVisual(false);
    }

    private bool TryGetCellCenterScreen(int x, int y, Camera uiCam, out Vector2 screenPos)
    {
        screenPos = default;

        if (x < 0 || x >= _width || y < 0 || y >= _height)
            return false;

        var cell = _cells[x, y];
        if (cell == null)
            return false;

        var rect = cell.transform as RectTransform;
        if (rect == null)
            return false;

        screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, rect.position);
        return true;
    }

    private bool IsTooFarFromLastPreview(Vector2 currentAnchorScreenPos, Camera uiCam)
    {
        if (_keepPreviewMaxDistancePx <= 0f)
            return false;

        if (_lastPreviewBlock == null || _lastPreviewCells.Count == 0)
            return true;

        int lastAnchorX = _lastPreviewBasePos.x + _lastPreviewAnchorOffset.x;
        int lastAnchorY = _lastPreviewBasePos.y + _lastPreviewAnchorOffset.y;

        if (!TryGetCellCenterScreen(lastAnchorX, lastAnchorY, uiCam, out var lastAnchorScreenPos))
            return true;

        float dist = Vector2.Distance(currentAnchorScreenPos, lastAnchorScreenPos);
        return dist > _keepPreviewMaxDistancePx;
    }

    private bool TryGetCellIndexFromScreen(Vector2 screenPos, Camera uiCam, out int x, out int y)
    {
        x = y = -1;

        if (_boardRoot == null || _boardGrid == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_boardRoot, screenPos, uiCam, out var local))
            return false;

        Rect rect = _boardRoot.rect;
        float localFromLeft = local.x - rect.xMin;
        float localFromBottom = local.y - rect.yMin;

        var padding = _boardGrid.padding;
        var cell = _boardGrid.cellSize;
        var spacing = _boardGrid.spacing;

        float px = localFromLeft - padding.left;
        float pyFromBottom = localFromBottom - padding.bottom;

        float stepX = cell.x + spacing.x;
        float stepY = cell.y + spacing.y;

        if (px < 0f || pyFromBottom < 0f)
            return false;

        int colFromLeft = Mathf.FloorToInt(px / stepX);
        int rowFromBottom = Mathf.FloorToInt(pyFromBottom / stepY);

        float inCellX = px - colFromLeft * stepX;
        float inCellY = pyFromBottom - rowFromBottom * stepY;
        if (inCellX < 0f || inCellX > cell.x || inCellY < 0f || inCellY > cell.y)
            return false;

        bool startTop = _boardGrid.startCorner == GridLayoutGroup.Corner.UpperLeft
                        || _boardGrid.startCorner == GridLayoutGroup.Corner.UpperRight;
        bool startRight = _boardGrid.startCorner == GridLayoutGroup.Corner.UpperRight
                          || _boardGrid.startCorner == GridLayoutGroup.Corner.LowerRight;

        int col = startRight ? (_width - 1) - colFromLeft : colFromLeft;
        int row = startTop ? (_height - 1) - rowFromBottom : rowFromBottom;

        if (col < 0 || col >= _width || row < 0 || row >= _height)
            return false;

        x = col;
        y = row;
        return true;
    }

    public bool UpdatePreviewFromScreen(DraggableBlock block, Vector2 screenPos, Camera uiCam = null)
    {
        return UpdatePreviewFromScreen(block, screenPos, Vector2Int.zero, uiCam);
    }

    public bool UpdatePreviewFromScreen(DraggableBlock block, Vector2 screenPos, Vector2Int anchorOffset, Camera uiCam = null)
    {
        // ?????? ????? offsets?? ?????? ?????? ?????? ???? ??????? ????
        if (block == null || block.CurrentOffsets == null || block.CurrentOffsets.Length == 0)
        {
            ClearDragPreview();
            return false;
        }

        // ??? ???????? ??? ???? ???? ?????? ???????? ??? ???? ???
        bool isSameBlock = _lastPreviewBlock == block;
        if (!isSameBlock)
        {
            ClearAllPreview();
            ClearLastPreviewInternal();
        }

        // ????? ????? ???? ???? ???????? ??????, "?????? ??? ?????? ????" ???
        if (!TryGetCellIndexFromScreen(screenPos, uiCam, out int anchorX, out int anchorY))
        {
            if (isSameBlock && _lastPreviewCells.Count > 0)
            {
                if (IsTooFarFromLastPreview(screenPos, uiCam))
                {
                    ClearDragPreview();
                    return false;
                }

                CanPlaceBlock = true;
                return false;
            }

            ClearDragPreview();
            return false;
        }

        int baseX = anchorX - anchorOffset.x;
        int baseY = anchorY - anchorOffset.y;

        // ??? ????????, "?????? ??? ?????? ????" ???
        if (!CanPlaceAt(baseX, baseY, block.CurrentOffsets, out var previewCells))
        {
            if (isSameBlock && _lastPreviewCells.Count > 0)
            {
                if (IsTooFarFromLastPreview(screenPos, uiCam))
                {
                    ClearDragPreview();
                    return false;
                }

                CanPlaceBlock = true;
                return false;
            }

            ClearDragPreview();
            return false;
        }

        // ???????? ??? ????: ?????? ???? ????
        ClearAllPreview();
        _lastPreviewCells.Clear();

        _lastPreviewCells.AddRange(previewCells);
        foreach (var cell in _lastPreviewCells)
            cell.UpdateCellVisual(true);

        _lastPreviewBasePos = new Vector2Int(baseX, baseY);
        _lastPreviewAnchorOffset = anchorOffset;
        _lastPreviewBlock = block;
        CanPlaceBlock = true;
        return true;
    }

    public bool PlaceLastPreview(DraggableBlock block, Sprite blockSprite)
    {
        if (!CanPlaceBlock)
            return false;

        if (block == null || block != _lastPreviewBlock)
            return false;

        if (_lastPreviewBasePos.x < 0 || _lastPreviewBasePos.y < 0)
            return false;

        if (block.CurrentOffsets == null || block.CurrentOffsets.Length == 0)
            return false;

        if (!CanPlaceAt(_lastPreviewBasePos.x, _lastPreviewBasePos.y, block.CurrentOffsets, out var cellsToPlace))
            return false;

        foreach (var cell in cellsToPlace)
            cell.PlaceBlock(blockSprite);

        ClearAllPreview();
        ClearLastPreviewInternal();
        return true;
    }

    public void ClearDragPreview()
    {
        ClearAllPreview();
        ClearLastPreviewInternal();
        CanPlaceBlock = false;
    }

    private void ClearLastPreviewInternal()
    {
        _lastPreviewBasePos = new Vector2Int(-1, -1);
        _lastPreviewAnchorOffset = Vector2Int.zero;
        _lastPreviewBlock = null;
        _lastPreviewCells.Clear();
    }

    private void ProcessFullLines(int blockShapeCount)
    {
        CheckFullLines();
        RemoveFullLines();
    }

    private void CheckFullLines()
    {
        _fullRow.Clear();
        _fullCol.Clear();

        // ???? ??
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

        // ???? ??
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

        if (_fullRow.Count + _fullCol.Count > 0)
            OnLinesCleared?.Invoke(_fullRow.Count + _fullCol.Count);
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

        for (int y = _height - 1; y >= 0; y--)
        {
            for (int x = 0; x < _width; x++)
            {
                if (!_cells[x, y].IsFilled && CanPlaceAt(x, y, shapeOffset))
                    return true;
            }
        }

        return false;
    }

    // Board?? ?? ???? shape????? ???? ?? ????? ???
    private bool CanPlaceAt(int baseX, int baseY, Vector2Int[] shapeOffset)
    {
        foreach (var offset in shapeOffset)
        {
            int tx = baseX + offset.x;
            int ty = baseY + offset.y;

            // ???? ???? ?????? cell ?????????? ???
            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                return false;

            if (_cells[tx, ty].IsFilled)
                return false;
        }

        _placeableCellPos.x = baseX;
        _placeableCellPos.y = baseY;

        return true;
    }

    private bool CanPlaceAt(int baseX, int baseY, Vector2Int[] shapeOffset, out List<BoardCell> cells)
    {
        cells = new List<BoardCell>(shapeOffset?.Length ?? 0);

        if (shapeOffset == null || shapeOffset.Length == 0)
            return false;

        foreach (var offset in shapeOffset)
        {
            int tx = baseX + offset.x;
            int ty = baseY + offset.y;

            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                return false;

            if (_cells[tx, ty].IsFilled)
                return false;

            cells.Add(_cells[tx, ty]);
        }

        return true;
    }

    private void ResetBoard()
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

    public void ShowHint(bool showHint, DraggableBlock block, bool isPlaced = false)
    {
        if (block == null)
            return;

        if (showHint)
            ApplyHint(_placeableCellPos, block.CurrentOffsets, true);

        else if (isPlaced)
            ApplyHint(_placeableCellPos, block.CurrentOffsets, false);

        else if (_prevBlock != null && block != _prevBlock)
            ApplyHint(_placeableCellPos, _prevBlock.CurrentOffsets, false);

        _prevBlock = block;
    }

    private void ApplyHint(Vector2Int basePos, Vector2Int[] offsets, bool show)
    {
        if (offsets == null || offsets.Length == 0)
            return;

        for (int i = 0; i < offsets.Length; i++)
        {
            int tx = basePos.x + offsets[i].x;
            int ty = basePos.y + offsets[i].y;

            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                continue;

            _hintCells[tx, ty].ShowHint(show);
        }
    }
}
