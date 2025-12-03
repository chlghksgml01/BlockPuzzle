using UnityEngine;
using UnityEngine.EventSystems;

public class BlockSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Canvas _canvas;
    public DraggableBlock _blockPrefab;

    private DraggableBlock _block;

    private void Awake()
    {
        SetNewBlock();
    }

    public void SetNewBlock()
    {
        _block = Instantiate(_blockPrefab, transform.position, transform.rotation, this.transform);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_block != null)
        {
            _block.MoveToPointer(transform as RectTransform, eventData.position);
            _block.SetBlockScale(BoardManager.Instance.BoardCellSize);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_block != null)
        {
            _block.SetBlockScale(_block._slotBlockSize);

            if (BoardManager.Instance.CanPlaceBlock)
            {
                _block.PlaceBlock();

                Destroy(_block.gameObject);
                _block = null;

                SetNewBlock();
            }

            else
            {
                (_block.transform as RectTransform).anchoredPosition = Vector2.zero;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_block != null)
        {
            _block.MoveToPointer(transform as RectTransform, eventData.position);
        }
    }
}