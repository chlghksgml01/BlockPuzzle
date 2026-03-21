using Mono.Cecil.Cil;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed class BoardGridMapper
{
    private readonly RectTransform _boardRoot;
    private readonly GridLayoutGroup _grid;
    private readonly int _width;
    private readonly int _height;

    private RectOffset _padding;
    private float _stepX;
    private float _stepY;
    private Vector2 _cellSize;

    public BoardGridMapper(RectTransform boardRoot, GridLayoutGroup grid, int width, int height)
    {
        _boardRoot = boardRoot;
        _grid = grid;
        _width = width;
        _height = height;

        InitBoard();
    }

    private void InitBoard()
    {
        _padding = _grid.padding;
        _cellSize = _grid.cellSize;

        // 마우스가 옆 칸으로 이동했는지 알려면 셀 하나 + 그 다음셀까지의 빈 공간을 합친 길이를 알아야함
        // -> stepX(stepY)마다 새로운 칸 시작
        _stepX = _cellSize.x + _grid.spacing.x;
        _stepY = _cellSize.y + _grid.spacing.y;

        Debug.Log("stepX: " + _stepX + ", stepY: " + _stepY);
    }

    // 화면 좌표가 보드의 어느 셀에 해당하는지 계산
    public bool TryGetCellIndexFromScreen(Vector2 screenPos, Camera uiCam, out int x, out int y, TMP_Text text, TMP_Text text2, RectTransform debugPointer)
    {
        x = y = -1;

        if (_boardRoot == null || _grid == null)
            return false;

        // posRelToBoard : _boardRoot 위에서 _boardRoot의 중심점(Pivot)을 (0,0)으로 잡았을 때 screenPos가 어디 있는지 나타낸 좌표
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_boardRoot, screenPos, uiCam, out Vector2 posRelToBoard))
            return false;

        // _boardRoot 피벗이 0,0이 아니어도 문제 없게끔 보드의 왼쪽 아래 모서리를 (0,0)으로 보정한 좌표로 변환
        Rect rect = _boardRoot.rect;
        float boardRelativeX = posRelToBoard.x - rect.xMin;
        float boardRelativeY = posRelToBoard.y - rect.yMin;

        // 패딩은 무시하고 실제 첫 번째 칸이 시작되는 지점부터 screenPos까지가 몇 픽셀인지 알기 위해 패딩만큼 보정
        float xFromFirstCell = boardRelativeX - _padding.left;
        float yFromFirstCell = boardRelativeY - _padding.bottom;

        if (xFromFirstCell < 0f || yFromFirstCell < 0f)
            return false;

        int colFromLeft = Mathf.FloorToInt(xFromFirstCell / _stepX);
        int rowFromBottom = Mathf.FloorToInt(yFromFirstCell / _stepY);

        // 지금까지 좌표 계산을 왼쪽 아래가 (0,0)인 기준으로 했으므로 그걸 보드의 시작 코너에 맞게 보정
        bool isStartTop = _grid.startCorner == GridLayoutGroup.Corner.UpperLeft
            || _grid.startCorner == GridLayoutGroup.Corner.UpperRight;
        bool isStartRight = _grid.startCorner == GridLayoutGroup.Corner.UpperRight
                 || _grid.startCorner == GridLayoutGroup.Corner.LowerRight;

        int col = isStartRight ? (_width - 1) - colFromLeft : colFromLeft;
        int row = isStartTop ? (_height - 1) - rowFromBottom : rowFromBottom;

        #region 테스트

        text.text = $"({(int)xFromFirstCell}, {(int)yFromFirstCell})";
        text2.text = $"({(int)colFromLeft}, {(int)rowFromBottom})";

        //// 1. 우리가 구한 '첫 번째 셀 기준 거리'에 
        //// 떼어냈던 패딩과 xMin(피벗 보정값)을 다시 더해줍니다.
        //float debugX = xFromFirstCell + _padding.left + rect.xMin;
        //float debugY = yFromFirstCell + _padding.bottom + rect.yMin;

        //// 2. 보드판(부모) 내에서의 로컬 좌표로 설정합니다.
        //debugPointer.anchoredPosition = new Vector2(debugX, debugY);
        debugPointer.anchoredPosition = posRelToBoard;

        #endregion

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

