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

    public bool CanPlaceBlock(Vector2 screenPosition, BlockShape blockShape, float maxDistance = 50f)
    {
        float closestDist;
        BoardCell baseClosest = GetClosestBoardCell(screenPosition, out closestDist);

        if (baseClosest == null || closestDist > maxDistance)
            return false;

        // 블럭 배치 가능 여부 검사
        foreach (Vector2Int offset in blockShape._cellOffsets)
        {
            int tx = baseClosest._x + offset.x;
            int ty = baseClosest._y + offset.y;

            // 보드 범위 벗어나면 불가
            if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
                return false;

            // 그 칸이 채워져 있으면 불가
            if (_cells[tx, ty].IsFilled)
                return false;
        }

        PlaceBlock(screenPosition, blockShape, maxDistance);
        return true;
    }

    private void PlaceBlock(Vector2 screenPosition, BlockShape blockShape, float maxDistance)
    {
    }

    private BoardCell GetClosestBoardCell(Vector2 screenPosition, out float closestDist)
    {
        BoardCell baseClosest = null;
        closestDist = float.MaxValue;

        // pivot 기준으로 가장 가까운 Board Cell 찾기
        for (int y = _height - 1; y >= 0; y--)   // 아래쪽부터 검사
        {
            for (int x = 0; x < _width; x++)
            {
                RectTransform cellRect = _cells[x, y].GetComponent<RectTransform>();
                Vector3 cellScreenPos = RectTransformUtility.WorldToScreenPoint(null, cellRect.position);

                float dist = Vector2.Distance(screenPosition, cellScreenPos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    baseClosest = _cells[x, y];
                }
            }
        }

        return baseClosest;
    }

}
