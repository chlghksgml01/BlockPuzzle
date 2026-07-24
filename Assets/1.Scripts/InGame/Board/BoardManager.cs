using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DefaultExecutionOrder(-90)]
public class BoardManager : MonoBehaviour, IInitializable, IBoardHandler, IBoardQuery, IBoardInfo
{
    private const int MinBoardSize = 8;
    private const int MaxBoardSize = 10;

    [Header("Board Configurations")]
    [Tooltip("보드 한 변의 칸 수 (정사각형). 예: 8 → 8x8, 9 → 9x9, 10 → 10x10")]
    [SerializeField, Range(MinBoardSize, MaxBoardSize)]
    [FormerlySerializedAs("_width")]
    private int _boardSize = 9;

    public int Width => _boardSize;
    public int Height => _boardSize;
    public int BoardSize => _boardSize;

    [Header("Grid Layout Presets")]
    [Tooltip("보드 크기별 GridLayoutGroup 설정. Board Size와 boardSize가 일치하는 프리셋이 Board에 적용됩니다.")]
    [SerializeField] private List<BoardGridLayoutPreset> _gridLayoutPresets = new List<BoardGridLayoutPreset>();

    [Header("References (UI & Prefabs)")]
    [SerializeField] private RectTransform _boardRoot;
    [SerializeField] private RectTransform _hintBoardRoot;
    [Tooltip("보드 배경 체커보드 Image (Board_Img)")]
    [SerializeField] private Image _boardBackgroundImage;
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
    private Func<string, Sprite> _missionSpriteResolver;

    public event Action<IReadOnlyList<int>, IReadOnlyList<int>> OnLinesClearedDetailed;

    /// <summary>직전 라인 클리어에 grass가 포함되었는지. ProcessFullLines 이후 유효.</summary>
    public bool LastClearContainedGrass { get; private set; }

    private ScoreSystem _scoreSystem;

    public void Initialize(InitializeContext context)
    {
        _scoreSystem = context.ScoreSystem;
    }

    /// <summary>MissionData의 크기로 보드 생성 전 _boardSize를 맞춘다. BoardManager.Awake 이전에 호출해야 한다.</summary>
    public void PrepareBoardSizeFromLayout(MissionData missionData)
    {
        if (missionData == null)
            return;

        _boardSize = Mathf.Clamp(missionData.boardSize, MinBoardSize, MaxBoardSize);
    }

    /// <summary>MissionData에 정의된 초기 채움 상태를 보드에 적용한다.</summary>
    public void ApplyBoardLayout(MissionData missionData, Func<string, Sprite> spriteResolver)
    {
        if (missionData == null)
            return;

        RestoreFilledCells(missionData.filledCells, spriteResolver);
    }

    /// <summary>점유된 미션 셀을 동시에 등장시킨다.</summary>
    public void PlayOccupiedCellsAppear(float duration)
    {
        if (_cells == null)
            return;

        for (int y = 0; y < _boardSize; y++)
        {
            for (int x = 0; x < _boardSize; x++)
            {
                BoardCell cell = _cells[x, y];
                if (!cell.IsOccupied)
                    continue;

                cell.PlayAppearTween(duration);
            }
        }
    }

    /// <summary>미션 팔레트 스프라이트 해석기. ice 단계 전환(ice01→ice02 등)에 사용.</summary>
    public void SetMissionSpriteResolver(Func<string, Sprite> spriteResolver)
    {
        _missionSpriteResolver = spriteResolver;
        if (_model != null)
            _model.SetSpriteResolver(_missionSpriteResolver);
    }

    private void Awake()
    {
        GenerateBoard();
        _boardEffect = GetComponentInChildren<BoardEffect>();
        ActivateGrayscale(false);
    }

    private void Reset()
    {
        _gridLayoutPresets = CreateDefaultPresets();
    }

    private void OnValidate()
    {
        _boardSize = Mathf.Clamp(_boardSize, MinBoardSize, MaxBoardSize);
        EnsureDefaultPresetsIfEmpty();
        ApplyGridLayoutSettings();
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
        ClearBoardChildren(_boardRoot);
        ClearBoardChildren(_hintBoardRoot);
        EnsureDefaultPresetsIfEmpty();
        ApplyGridLayoutSettings();

        _cells = new BoardCell[_boardSize, _boardSize];
        _hintCells = new HintBoardCell[_boardSize, _boardSize];

        _boardGrid = _boardRoot.GetComponent<GridLayoutGroup>();
        BoardCellSize = _boardGrid.cellSize.x;

        for (int y = 0; y < _boardSize; y++)
        {
            for (int x = 0; x < _boardSize; x++)
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
        _mapper = new BoardGridMapper(_boardRoot, _boardGrid, _boardSize, _boardSize);
        _model = new BoardModel(_boardSize, _boardSize, _cells, (cleared, rows, cols) =>
        {
            if (_scoreSystem != null)
                _scoreSystem.CalculateLineScore(cleared);
            OnLinesClearedDetailed?.Invoke(rows, cols);
        });
        _preview = new BoardPreviewController(_model, _mapper, _keepPreviewMaxDistancePx);
        _hint = new BoardHintController(_boardSize, _boardSize, _hintCells, _model);

        if (_missionSpriteResolver != null)
            _model.SetSpriteResolver(_missionSpriteResolver);
    }

    private void ApplyGridLayoutSettings()
    {
        BoardGridLayoutPreset preset = FindLayoutPreset(_boardSize);
        if (preset != null)
        {
            ApplyLayoutPreset(_boardRoot, preset);
            ApplyLayoutPreset(_hintBoardRoot, preset);
            ApplyBoardBackgroundImage(preset);
            return;
        }

        Debug.LogWarning($"BoardManager: boardSize {_boardSize}에 해당하는 Grid Layout 프리셋이 없습니다.", this);
        ApplyGridConstraintOnly(_boardRoot);
        ApplyGridConstraintOnly(_hintBoardRoot);
    }

    private BoardGridLayoutPreset FindLayoutPreset(int boardSize)
    {
        if (_gridLayoutPresets == null)
            return null;

        for (int i = 0; i < _gridLayoutPresets.Count; i++)
        {
            BoardGridLayoutPreset preset = _gridLayoutPresets[i];
            if (preset != null && preset.BoardSize == boardSize)
                return preset;
        }

        return null;
    }

    private void ApplyLayoutPreset(RectTransform root, BoardGridLayoutPreset preset)
    {
        if (root == null || preset == null)
            return;

        GridLayoutGroup grid = root.GetComponent<GridLayoutGroup>();
        if (grid == null)
            return;

        preset.ApplyTo(grid);
    }

    private void ApplyBoardBackgroundImage(BoardGridLayoutPreset preset)
    {
        if (_boardBackgroundImage == null || preset == null)
            return;

        preset.ApplyBoardImage(_boardBackgroundImage);
    }

    private void ApplyGridConstraintOnly(RectTransform root)
    {
        if (root == null)
            return;

        GridLayoutGroup grid = root.GetComponent<GridLayoutGroup>();
        if (grid == null)
            return;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = _boardSize;
    }

    private void EnsureDefaultPresetsIfEmpty()
    {
        if (_gridLayoutPresets != null && _gridLayoutPresets.Count > 0)
            return;

        _gridLayoutPresets = CreateDefaultPresets();
    }

    private static List<BoardGridLayoutPreset> CreateDefaultPresets()
    {
        return new List<BoardGridLayoutPreset>
        {
            BoardGridLayoutPreset.CreateDefault(8),
            BoardGridLayoutPreset.CreateDefault(9),
            BoardGridLayoutPreset.CreateDefault(10)
        };
    }

    private void ClearBoardChildren(RectTransform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    public bool TryGetCellWorldPosition(int x, int y, out Vector3 worldPos)
    {
        worldPos = default;

        if (_cells == null || x < 0 || x >= _boardSize || y < 0 || y >= _boardSize)
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
        LastClearContainedGrass = _model.ProcessFullLines();
    }

    public bool HasAnyGrass() => _model != null && _model.HasAnyGrass();

    public bool TrySpreadGrass(float appearDuration)
    {
        if (_model == null)
            return false;

        return _model.TrySpreadGrass(appearDuration);
    }

    private void ResetBoard()
    {
        ClearDragPreview();
        _hint.ClearAll();
        _model.ResetBoard();
        LastClearContainedGrass = false;
    }

    public void ActivateGrayscale(bool useGrayScale, float effectDuration = 0f)
    {
        _boardEffect.ActivateGrayscale(useGrayScale, effectDuration);
    }

    #region InGameManager
    public void PlayIntro(Action onComplete = null)
    {
        _boardEffect.PlayIntro(_cells, _boardSize, _boardSize, onComplete);
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

            if (data.x < 0 || data.x >= _boardSize || data.y < 0 || data.y >= _boardSize)
                continue;

            Sprite sprite = spriteResolver != null ? spriteResolver(data.spriteName) : null;
            if (BoardCell.IsStoneSpriteName(data.spriteName))
                _cells[data.x, data.y].SetBlocked(sprite);
            else
                _cells[data.x, data.y].RestoreFilledState(sprite);
        }
    }

    public List<FilledCellData> ExportFilledCells()
    {
        List<FilledCellData> result = new List<FilledCellData>();
        for (int x = 0; x < _boardSize; x++)
        {
            for (int y = 0; y < _boardSize; y++)
            {
                BoardCell cell = _cells[x, y];
                if (!cell.IsOccupied)
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
