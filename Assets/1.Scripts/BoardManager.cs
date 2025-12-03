using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum LineType
{
    Row,
    Column
}

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
}
