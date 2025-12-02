using UnityEditorInternal;
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("BodyTile"))
        {
            _image.sprite = BoardManager.Instance._previewSprite;
            _image.color = new Color(1f, 1f, 1f, BoardManager.Instance._previewAlpha);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("BodyTile"))
        {
            _image.sprite = BoardManager.Instance._previewSprite;
            _image.color = new Color(1f, 1f, 1f, BoardManager.Instance._previewAlpha);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("BodyTile"))
        {
            _image.sprite = null;
            _image.color = new Color(1f, 1f, 1f, 0f);
        }
    }
}