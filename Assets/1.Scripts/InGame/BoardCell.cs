using UnityEngine;
using UnityEngine.UI;

public class BoardCell : MonoBehaviour
{
    private Image _image;
    public int _x { get; set; }
    public int _y { get; set; }

    public bool IsFilled { get; private set; }
    public bool IsPreviewFilled { get; private set; }
    public Sprite FilledSprite => _defaultSprite;

    private Sprite _defaultSprite;

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
        IsPreviewFilled = isfilled;
    }

    public void SetPreviewFilled(bool isPreviewFilled)
    {
        IsPreviewFilled = isPreviewFilled;
    }

    public void UpdateCellVisual(bool isPreviewFilled, Sprite blockSprite = null)
    {
        if (IsFilled)
            return;

        if (isPreviewFilled)
        {
            _image.sprite = blockSprite;
            _image.color = BoardManager.Instance.PreviewColor;
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
        _defaultSprite = blockSprite;
        _image.sprite = _defaultSprite;
        _image.color = new Color(1f, 1f, 1f, 1f);
    }

    public void RestoreFilledState(Sprite blockSprite)
    {
        SetFilled(true);
        _defaultSprite = blockSprite;
        _image.sprite = _defaultSprite;
        _image.color = new Color(1f, 1f, 1f, 1f);
    }

    public void SetLinePreview(bool active, Sprite lineSprite = null)
    {
        if (IsPreviewFilled)
            return;

        if (active)
        {
            _image.sprite = lineSprite;
            _image.color = new Color(1f, 1f, 1f, 1f);
        }
        else
        {
            _image.sprite = _defaultSprite;
        }
    }
}