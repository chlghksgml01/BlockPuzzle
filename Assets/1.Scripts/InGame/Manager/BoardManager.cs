using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardManager : Singleton<BoardManager>
{
    public int _width = 9;
    public int _height = 9;

    [SerializeField]
    private RectTransform _boardRoot;
    [SerializeField]
    private RectTransform _hintBoardRoot;
    [SerializeField]
    private GameObject _cellPrefab;
    [SerializeField]
    private GameObject _hintCellPrefab;

    private BoardCell[,] _cells;
    private HintBoardCell[,] _hintCells;
    public Sprite _previewSprite;
    public float _previewAlpha = 0.6f;

    public float BoardCellSize { get; set; }
    public bool CanPlaceBlock { get; set; }

    private List<int> _fullRow = new List<int>();
    private List<int> _fullCol = new List<int>();

    private Vector2Int _placeableCellPos;

    public DraggableBlock _prevBlock;


    override protected void OnAwake()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        _cells = new BoardCell[_width, _height];
        _hintCells = new HintBoardCell[_width, _height];
        BoardCellSize = _boardRoot.GetComponent<GridLayoutGroup>().cellSize.x;

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                GameObject cell = Instantiate(_cellPrefab, _boardRoot);
                _cells[x, y] = cell.GetComponent<BoardCell>();

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

    public void ProcessFullLines()
    {
        CheckFullLines();
        RemoveFullLines();
    }

    private void CheckFullLines()
    {
        _fullRow.Clear();
        _fullCol.Clear();

        // 가로 체크
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

        // 세로 체크
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
            ScoreManager.Instance.CalculateLineScore(_fullRow.Count + _fullCol.Count);
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

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (!_cells[x, y].IsFilled && CanPlaceAt(x, y, shapeOffset))
                    return true;
            }
        }
        return false;
    }

    // Board의 빈 곳에 shape모양대로 놓을 수 있는지 검사
    private bool CanPlaceAt(int baseX, int baseY, Vector2Int[] shapeOffset)
    {
        foreach (var offset in shapeOffset)
        {
            int tx = baseX + offset.x;
            int ty = baseY + offset.y;

            // 보드 범위 밖이거나 cell 채워져있는지 검사
            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                return false;

            if (_cells[tx, ty].IsFilled)
                return false;
        }

        _placeableCellPos.x = baseX;
        _placeableCellPos.y = baseY;

        return true;
    }

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

        InGameManager.Instance.SpawnNewBlock();
        ScoreManager.Instance.ResetScore();
    }

    public void ShowHint(bool showHint, DraggableBlock block = null)
    {
        if (block == null || block.CurrentOffsets.Length == 0)
            return;

        if (showHint)
        {
            for (int i = 0; i < block.CurrentOffsets.Length; i++)
            {
                int tx = _placeableCellPos.x + block.CurrentOffsets[i].x;
                int ty = _placeableCellPos.y + block.CurrentOffsets[i].y;

                if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                    continue;

                _hintCells[tx, ty].ShowHint(true);
            }
        }

        else if (!showHint && _prevBlock != null && block != _prevBlock)
        {
            for (int i = 0; i < _prevBlock.CurrentOffsets.Length; i++)
            {
                int tx = _placeableCellPos.x + _prevBlock.CurrentOffsets[i].x;
                int ty = _placeableCellPos.y + _prevBlock.CurrentOffsets[i].y;

                if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                    continue;

                _hintCells[tx, ty].ShowHint(false);
            }
        }

        _prevBlock = block;
    }
}
