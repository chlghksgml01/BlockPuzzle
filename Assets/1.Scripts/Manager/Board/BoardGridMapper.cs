using UnityEngine;
using UnityEngine.UI;

public sealed class BoardGridMapper
{
    private readonly RectTransform _boardRoot;
    private readonly GridLayoutGroup _grid;
    private readonly int _width;
    private readonly int _height;

    public BoardGridMapper(RectTransform boardRoot, GridLayoutGroup grid, int width, int height)
    {
        _boardRoot = boardRoot;
        _grid = grid;
        _width = width;
        _height = height;
    }

    // 화면 좌표가 보드의 어느 셀에 해당하는지 계산
    public bool TryGetCellIndexFromScreen(Vector2 screenPos, Camera uiCam, out int x, out int y)
    {
        x = y = -1;

        if (_boardRoot == null || _grid == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_boardRoot, screenPos, uiCam, out Vector2 local))
            return false;

        Rect rect = _boardRoot.rect;
        float localFromLeft = local.x - rect.xMin;
        float localFromBottom = local.y - rect.yMin;

        RectOffset padding = _grid.padding;
        Vector2 cell = _grid.cellSize;
        Vector2 spacing = _grid.spacing;

        float px = localFromLeft - padding.left;
        float pyFromBottom = localFromBottom - padding.bottom;

        float stepX = cell.x + spacing.x;
        float stepY = cell.y + spacing.y;

        if (px < 0f || pyFromBottom < 0f)
            return false;

        int colFromLeft = Mathf.FloorToInt(px / stepX);
        int rowFromBottom = Mathf.FloorToInt(pyFromBottom / stepY);

        // 셀 영역 내부(간격 영역 제외)인지 체크
        float inCellX = px - colFromLeft * stepX;
        float inCellY = pyFromBottom - rowFromBottom * stepY;
        if (inCellX < 0f || inCellX > cell.x || inCellY < 0f || inCellY > cell.y)
            return false;

        bool startTop = _grid.startCorner == GridLayoutGroup.Corner.UpperLeft
                        || _grid.startCorner == GridLayoutGroup.Corner.UpperRight;
        bool startRight = _grid.startCorner == GridLayoutGroup.Corner.UpperRight
                          || _grid.startCorner == GridLayoutGroup.Corner.LowerRight;

        int col = startRight ? (_width - 1) - colFromLeft : colFromLeft;
        int row = startTop ? (_height - 1) - rowFromBottom : rowFromBottom;

        if (col < 0 || col >= _width || row < 0 || row >= _height)
            return false;

        x = col;
        y = row;
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

