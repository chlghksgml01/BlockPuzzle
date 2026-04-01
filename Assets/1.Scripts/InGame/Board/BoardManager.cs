using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-90)]
public class BoardManager : MonoBehaviour, IInitializable, IBoardHandler, IBoardQuery, IBoardInfo
{
    [Header("Board Configurations")]
    [SerializeField] private int _width = 9;
    [SerializeField] private int _height = 9;
    public int Width => _width;
    public int Height => _height;

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

    private BoardEffect _boardEffect;
    private BoardGridMapper _mapper;
    private BoardModel _model;
    private BoardPreviewController _preview;
    private BoardHintController _hint;

    public event Action<IReadOnlyList<int>, IReadOnlyList<int>> OnLinesClearedDetailed;

    private ScoreSystem _scoreSystem;

    public void Initialize(InitializeContext context)
    {
        _scoreSystem = context.ScoreSystem;
    }

    private void Awake()
    {
        GenerateBoard();
        _boardEffect = GetComponentInChildren<BoardEffect>();
        ActivateGrayscale(false);
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
                _cells[x, y].Init(x, y, this);

                GameObject hintCell = Instantiate(_hintCellPrefab, _hintBoardRoot);
                _hintCells[x, y] = hintCell.GetComponent<HintBoardCell>();
            }
        }

        InitializeBoardCore();
    }

    private void InitializeBoardCore()
    {
        _mapper = new BoardGridMapper(_boardRoot, _boardGrid, _width, _height);
        _model = new BoardModel(_width, _height, _cells, (cleared, rows, cols) =>
        {
            if (_scoreSystem != null)
                _scoreSystem.CalculateLineScore(cleared);
            OnLinesClearedDetailed?.Invoke(rows, cols);
        });
        _preview = new BoardPreviewController(_model, _mapper, _keepPreviewMaxDistancePx);
        _hint = new BoardHintController(_width, _height, _hintCells, _model);
    }

    public bool TryGetCellWorldPosition(int x, int y, out Vector3 worldPos)
    {
        worldPos = default;

        if (_cells == null || x < 0 || x >= _width || y < 0 || y >= _height)
            return false;

        BoardCell cell = _cells[x, y];
        if (cell == null)
            return false;

        worldPos = cell.transform.position;
        return true;
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

        bool canPlaceBlock = _preview.PlaceLastPreview(block, blockSprite, out placedCount);
        if (canPlaceBlock)
            CanPlaceBlock = false;

        return canPlaceBlock;
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

    private void ResetBoard()
    {
        ClearDragPreview();
        _model.ResetBoard();
    }

    public void ActivateGrayscale(bool useGrayScale, float effectDuration = 0f)
    {
        _boardEffect.ActivateGrayscale(useGrayScale, effectDuration);
    }

    #region InGameManager
    public void PlayIntro()
    {
        _boardEffect.PlayIntro(_cells, _width, _height);
    }

    public void ShowHint(bool showHint, DraggableBlock block, bool isPlaced = false)
    {
        _hint.ShowHint(showHint, block, isPlaced);
    }

    public void RestoreFilledCells(List<FilledCellData> filledCells, Func<string, Sprite> spriteResolver)
    {
        _model.ResetBoard();
        ClearDragPreview();

        if (filledCells == null)
            return;

        for (int i = 0; i < filledCells.Count; i++)
        {
            FilledCellData data = filledCells[i];
            if (data == null)
                continue;

            if (data.x < 0 || data.x >= _width || data.y < 0 || data.y >= _height)
                continue;

            Sprite sprite = spriteResolver != null ? spriteResolver(data.spriteName) : null;
            _cells[data.x, data.y].RestoreFilledState(sprite);
        }
    }

    public List<FilledCellData> ExportFilledCells()
    {
        List<FilledCellData> result = new List<FilledCellData>();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                BoardCell cell = _cells[x, y];
                if (!cell.IsFilled)
                    continue;

                result.Add(new FilledCellData
                {
                    x = x,
                    y = y,
                    spriteName = cell.FilledSprite != null ? cell.FilledSprite.name : string.Empty
                });
            }
        }

        return result;
    }

    public bool CanPlaceShape(Vector2Int[] shapeOffset) => _model.CanPlaceShape(shapeOffset);
    #endregion

    [ContextMenu("Turn On GrayScale 1s")]
    private void Test_TurnOn()
    {
        ActivateGrayscale(true, 1f);
    }

    [ContextMenu("Turn Off GrayScale")]
    private void Test_TurnOff()
    {
        ActivateGrayscale(false);
    }
}
