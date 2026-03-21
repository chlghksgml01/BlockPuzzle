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

    [Header("Visual Settings")]
    [SerializeField] private Color _previewColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color PreviewColor => _previewColor;

    [Header("Drag Preview Settings")]
    [SerializeField, Min(0f)] private float _keepPreviewMaxDistancePx = 180f;

    public float BoardCellSize { get; private set; }
    public bool CanPlaceBlock { get; private set; }

    private BoardCell[,] _cells;
    private HintBoardCell[,] _hintCells;
    private GridLayoutGroup _boardGrid;
    private List<BoardCell> _lastPreviewCells;

    private BoardGridMapper _mapper;
    private BoardModel _model;
    private BoardPreviewController _preview;
    private BoardHintController _hint;

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

        InitializeBoardCore();
    }

    private void InitializeBoardCore()
    {
        _mapper = new BoardGridMapper(_boardRoot, _boardGrid, _width, _height);
        _model = new BoardModel(_width, _height, _cells, cleared => OnLinesCleared?.Invoke(cleared));
        _preview = new BoardPreviewController(_model, _mapper, _keepPreviewMaxDistancePx);
        _hint = new BoardHintController(_width, _height, _hintCells, _model);
    }

    public void UpdatePreviewFromScreen(DraggableBlock block, Vector2 anchorScreenPos, Camera uiCam = null)
    {
        UpdatePreviewFromScreen(block, anchorScreenPos, Vector2Int.zero, uiCam);
    }

    public void UpdatePreviewFromScreen(DraggableBlock block, Vector2 anchorScreenPos, Vector2Int anchorOffset, Camera uiCam = null)
    {
        bool canPlace;
        bool changed = _preview.UpdatePreview(block, anchorScreenPos, anchorOffset, uiCam, out canPlace, out _lastPreviewCells);
        if (changed)
            _model.PreviewLineClears(_lastPreviewCells, block.BlockSprite);
        CanPlaceBlock = canPlace;
    }

    public bool PlaceLastPreview(DraggableBlock block, Sprite blockSprite, out int placedCount)
    {
        placedCount = 0;

        if (!CanPlaceBlock)
            return false;

        bool ok = _preview.PlaceLastPreview(block, blockSprite, out placedCount);
        if (ok)
            CanPlaceBlock = false;

        return ok;
    }

    public void ClearDragPreview()
    {
        _preview.Clear();
        CanPlaceBlock = false;
    }

    private void ProcessFullLines(int blockShapeCount)
    {
        _model.ProcessFullLines();
    }

    public bool CanPlaceShape(Vector2Int[] shapeOffset) => _model.CanPlaceShape(shapeOffset);

    private void ResetBoard()
    {
        ClearDragPreview();
        _model.ResetBoard();
    }

    public void ShowHint(bool showHint, DraggableBlock block, bool isPlaced = false)
    {
        _hint.ShowHint(showHint, block, isPlaced);
    }
}
