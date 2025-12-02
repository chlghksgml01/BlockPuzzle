using UnityEngine;
using UnityEngine.UI;

public class BoardManager : Singleton<BoardManager>
{
    public int _width = 9;
    public int _height = 9;

    public RectTransform _boardRoot;
    public GameObject _cellPrefab;

    public BoardCell[,] _cells;
    public Sprite _previewSprite;
    public float _previewAlpha = 0.6f;
    public float cellColSizeMargin = 0.2f;

    public float BoardCellSize;

    override protected void OnAwake()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        _cells = new BoardCell[_width, _height];
        BoardCellSize = _boardRoot.GetComponent<GridLayoutGroup>().cellSize.x;
        Vector2 cellColSize = new Vector2(BoardCellSize / 2 - cellColSizeMargin, BoardCellSize / 2 - cellColSizeMargin);

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                GameObject cell = Instantiate(_cellPrefab, _boardRoot);
                cell.GetComponent<BoxCollider2D>().size = cellColSize;
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
}
