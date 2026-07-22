using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[Serializable]
public class BoardGridLayoutPreset
{
    [Tooltip("적용할 보드 크기 (한 변 칸 수)")]
    [SerializeField]
    [FormerlySerializedAs("boardSize")]
    private int _boardSize = 9;

    [Header("Board Img")]
    [SerializeField] private Sprite _boardImg;

    [Header("Padding")]
    [Tooltip("GridLayoutGroup Padding Left")]
    [SerializeField]
    [FormerlySerializedAs("paddingLeft")]
    private int _paddingLeft;

    [Tooltip("GridLayoutGroup Padding Right")]
    [SerializeField]
    [FormerlySerializedAs("paddingRight")]
    private int _paddingRight;

    [Tooltip("GridLayoutGroup Padding Top")]
    [SerializeField]
    [FormerlySerializedAs("paddingTop")]
    private int _paddingTop;

    [Tooltip("GridLayoutGroup Padding Bottom")]
    [SerializeField]
    [FormerlySerializedAs("paddingBottom")]
    private int _paddingBottom;

    [Header("Cell Size")]
    [Tooltip("GridLayoutGroup Cell Size (X, Y)")]
    [SerializeField]
    [FormerlySerializedAs("cellSize")]
    private Vector2 _cellSize = new Vector2(106f, 106f);

    [Header("Spacing")]
    [Tooltip("GridLayoutGroup Spacing (X, Y)")]
    [SerializeField]
    [FormerlySerializedAs("spacing")]
    private Vector2 _spacing = new Vector2(3.3f, 1.5f);

    public int BoardSize => _boardSize;
    public Sprite BoardImg => _boardImg;

    public void ApplyTo(GridLayoutGroup grid)
    {
        if (grid == null)
            return;

        grid.padding = new RectOffset(_paddingLeft, _paddingRight, _paddingTop, _paddingBottom);
        grid.cellSize = _cellSize;
        grid.spacing = _spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = _boardSize;
    }

    public void ApplyBoardImage(Image image)
    {
        if (image == null || _boardImg == null)
            return;

        image.sprite = _boardImg;
    }

    public static BoardGridLayoutPreset CreateDefault(int size)
    {
        switch (size)
        {
            case 8:
                return new BoardGridLayoutPreset
                {
                    _boardSize = 8,
                    _paddingLeft = 12,
                    _paddingRight = 0,
                    _paddingTop = 9,
                    _paddingBottom = 2,
                    _cellSize = new Vector2(119f, 119f),
                    _spacing = new Vector2(3.3f, 1.5f)
                };
            case 10:
                return new BoardGridLayoutPreset
                {
                    _boardSize = 10,
                    _paddingLeft = 7,
                    _paddingRight = 0,
                    _paddingTop = 5,
                    _paddingBottom = 2,
                    _cellSize = new Vector2(97f, 97f),
                    _spacing = new Vector2(1.8f, 0.3f)
                };
            default:
                return new BoardGridLayoutPreset
                {
                    _boardSize = 9,
                    _paddingLeft = 19,
                    _paddingRight = 0,
                    _paddingTop = 23,
                    _paddingBottom = 2,
                    _cellSize = new Vector2(106f, 106f),
                    _spacing = new Vector2(3.3f, 1.5f)
                };
        }
    }
}
