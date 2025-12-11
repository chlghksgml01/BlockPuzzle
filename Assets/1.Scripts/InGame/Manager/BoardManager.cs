using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Collections.AllocatorManager;

public class BoardManager : Singleton<BoardManager>
{
    public int _width = 9;
    public int _height = 9;

    public RectTransform _boardRoot;
    public GameObject _cellPrefab;

    public BoardCell[,] _cells;
    public Sprite _previewSprite;
    public float _previewAlpha = 0.6f;
    public float cellColSizePercent = 0.6f;

    public float BoardCellSize { get; set; }
    public bool CanPlaceBlock { get; set; }

    private List<int> _fullRow = new List<int>();
    private List<int> _fullCol = new List<int>();


    override protected void OnAwake()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        _cells = new BoardCell[_width, _height];
        BoardCellSize = _boardRoot.GetComponent<GridLayoutGroup>().cellSize.x;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                GameObject cell = Instantiate(_cellPrefab, _boardRoot);
                _cells[x, y] = cell.GetComponent<BoardCell>();
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

    public bool CanPlaceShape(BlockShape shape)
    {
        if (shape == null || shape._cellOffsets == null || shape._cellOffsets.Length == 0)
            return false;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (!_cells[x, y].IsFilled && CanPlaceAt(x, y, shape))
                    return true;
            }
        }

        return false;
    }

    // Board의 빈 곳에 shape모양대로 놓을 수 있는지 검사
    private bool CanPlaceAt(int baseX, int baseY, BlockShape shape)
    {
        foreach (var offset in shape._cellOffsets)
        {
            int tx = baseX + offset.x;
            int ty = baseY + offset.y;

            // 보드 범위 밖이거나 cell 채워져있는지 검사
            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                return false;

            if (_cells[tx, ty].IsFilled)
                return false;
        }

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
}
