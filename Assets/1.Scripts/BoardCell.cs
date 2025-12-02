using UnityEngine;
using UnityEngine.UI;

public class BoardCell : MonoBehaviour
{
    private Image _image;
    public int _x { get; set; }
    public int _y { get; set; }

    public bool IsFilled { get; private set; }
    private bool IsCollision { get; set; }

    private void OnEnable()
    {
        BlockSlot.OnSlotPointerUp += HandleSlotPointerUp;
    }

    private void OnDisable()
    {
        BlockSlot.OnSlotPointerUp -= HandleSlotPointerUp;
    }

    private void HandleSlotPointerUp(Sprite blockSprite)
    {
        if (IsCollision)
        {
            SetFilled(true);
            _image.sprite = blockSprite;
            _image.color = new Color(1f, 1f, 1f, 1f);
        }
    }

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
        if (IsFilled)
        {
            BoardManager.Instance.CanPlaceBlock = false;
            return;
        }
        if (collision.CompareTag("BodyTile"))
        {
            DraggableBlock draggableBlock = collision.GetComponentInParent<DraggableBlock>();

            draggableBlock.OnTileEnterCell();
            if (draggableBlock.IsAllBodyBlockPlaceable())
            {
                UpdateCellCollision(true);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (IsFilled)
        {
            BoardManager.Instance.CanPlaceBlock = false;
            return;
        }
        if (collision.CompareTag("BodyTile") && BoardManager.Instance.CanPlaceBlock)
        {
            if (collision.GetComponentInParent<DraggableBlock>().IsAllBodyBlockPlaceable())
            {
                UpdateCellCollision(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsFilled)
            return;

        if (collision.CompareTag("BodyTile"))
        {
            collision.GetComponentInParent<DraggableBlock>().OnTileExitCell();
            UpdateCellCollision(false);
        }
    }

    private void UpdateCellCollision(bool isCollision)
    {
        BoardManager.Instance.CanPlaceBlock = isCollision;
        IsCollision = isCollision;

        if (isCollision)
        {
            _image.sprite = BoardManager.Instance._previewSprite;
            _image.color = new Color(1f, 1f, 1f, BoardManager.Instance._previewAlpha);
        }
        else
        {
            _image.sprite = null;
            _image.color = new Color(1f, 1f, 1f, 0f);
        }
    }
}