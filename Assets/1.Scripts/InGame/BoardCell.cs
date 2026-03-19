using UnityEngine;
using UnityEngine.UI;

public class BoardCell : MonoBehaviour
{
    private Image _image;
    public int _x { get; set; }
    public int _y { get; set; }

    public bool IsFilled { get; private set; }

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void Init(int x, int y)
    {
        _x = x;
        _y = y;
        SetFilled(false);
    }

    public void SetFilled(bool isfilled)
    {
        IsFilled = isfilled;
    }

    public void UpdateCellVisual(bool isPreviewFilled)
    {
        if (IsFilled)
            return;

        if (isPreviewFilled)
        {
            _image.sprite = BoardManager.Instance.PreviewSprite;
            _image.color = new Color(1f, 1f, 1f, BoardManager.Instance.PreviewAlpha);
        }
        else if (!isPreviewFilled)
        {
            _image.sprite = null;
            _image.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void PlaceBlock(Sprite blockSprite)
    {
        SetFilled(true);
        _image.sprite = blockSprite;
        _image.color = new Color(1f, 1f, 1f, 1f);
    }
}