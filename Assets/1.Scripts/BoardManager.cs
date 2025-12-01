using UnityEngine;

public class BoardManager : Singleton<BoardManager>
{
    public int _width = 9;
    public int _height = 9;

    public RectTransform _boardRoot;
    public GameObject _cellPrefab;

    public BoardCell[,] _cells;

    override protected void OnAwake()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        _cells = new BoardCell[_width, _height];

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

    public BoardCell GetClosestCell(Vector2 screenPosition, float maxDistance = 50f)
    {
        BoardCell closest = null;
        float closestDist = float.MaxValue;

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                RectTransform cellRect = _cells[x, y].GetComponent<RectTransform>();
                Vector3 cellScreenPos = RectTransformUtility.WorldToScreenPoint(null, cellRect.position);

                float dist = Vector2.Distance(screenPosition, cellScreenPos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = _cells[x, y];
                }
            }
        }

        if (closestDist > maxDistance)
            return null;

        return closest;
    }
}
