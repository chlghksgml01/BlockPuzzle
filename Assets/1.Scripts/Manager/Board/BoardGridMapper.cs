using TMPro;
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

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_boardRoot, screenPos, uiCam, out Vector2 posRelToBoard))
            return false;

        Rect rect = _boardRoot.rect;

        float boardRelativeX = posRelToBoard.x - rect.xMin;
        float boardRelativeYFromTop = rect.yMax - posRelToBoard.y;

        float xFromFirstCell = boardRelativeX - _padding.left;
        float yFromFirstCell = boardRelativeYFromTop - _padding.top;

        int colIdx = Mathf.FloorToInt(xFromFirstCell / _stepX);
        int rowIdx = Mathf.FloorToInt(yFromFirstCell / _stepY);

        if (colIdx < 0 || colIdx >= _width || rowIdx < 0 || rowIdx >= _height)
            return false;

        x = colIdx;
        y = _height - 1 - rowIdx;

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
