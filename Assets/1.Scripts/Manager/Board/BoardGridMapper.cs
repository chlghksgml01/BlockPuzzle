using UnityEngine;
using UnityEngine.UI;

public sealed class BoardGridMapper
{
    private readonly RectTransform _boardRoot;
    private readonly GridLayoutGroup _grid;
    private readonly int _width;
    private readonly int _height;

    private readonly float _stepX;
    private readonly float _stepY;
    private readonly Vector2 _cellSize;
    private readonly RectOffset _padding;

    public BoardGridMapper(RectTransform boardRoot, GridLayoutGroup grid, int width, int height)
    {
        _boardRoot = boardRoot;
        _grid = grid;
        _width = width;
        _height = height;

        _padding = _grid.padding;
        _cellSize = _grid.cellSize;
        _stepX = _cellSize.x + _grid.spacing.x;
        _stepY = _cellSize.y + _grid.spacing.y;
    }

    public bool TryGetCellIndexFromScreen(Vector2 screenPos, Camera uiCam, out int x, out int y)
    {
        x = y = -1;

        if (_boardRoot == null || _grid == null)
            return false;

        // posRelToBoard : _boardRoot 위에서 _boardRoot의 중심점(Pivot)을 (0,0)으로 잡았을 때 screenPos가 어디 있는지 나타낸 좌표
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_boardRoot, screenPos, uiCam, out Vector2 posRelToBoard))
            return false;

        // 왼쪽 위 0,0 기준으로 보정
        Rect rect = _boardRoot.rect;
        float boardRelativeX = posRelToBoard.x - rect.xMin;
        float boardRelativeY = rect.yMax - posRelToBoard.y;

        // 패딩은 무시하고 실제 첫 번째 칸이 시작되는 지점부터 screenPos까지가 몇 픽셀인지 알기 위해 패딩만큼 보정
        float xFromFirstCell = boardRelativeX - _padding.left;
        float yFromFirstCell = boardRelativeY - _padding.top;

        int colIdx = Mathf.FloorToInt(xFromFirstCell / _stepX);
        int rowIdx = Mathf.FloorToInt(yFromFirstCell / _stepY);

        if (colIdx < 0 || colIdx >= _width || rowIdx < 0 || rowIdx >= _height)
            return false;

        x = colIdx;
        y = rowIdx;

        return true;
    }

    public static bool TryGetRectScreenPos(RectTransform rect, Camera uiCam, out Vector2 screenPos)
    {
        screenPos = default;
        if (rect == null)
            return false;

        screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, rect.position);
        return true;
    }
}
