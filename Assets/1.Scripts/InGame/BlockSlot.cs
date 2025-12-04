using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Canvas _canvas;
    public DraggableBlock _blockPrefab;

    public DraggableBlock Block { get; private set; }

    public static event Action<DraggableBlock> OnBlockPlaced;

    private void Awake()
    {
        SetNewBlock();
    }

    public void SetNewBlock()
    {
        Block = Instantiate(_blockPrefab, transform.position, transform.rotation, this.transform);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Block == null)
        {
            Debug.Log("BlockSlot - Block is null");
            return;
        }

        Block.MoveToPointer(transform as RectTransform, eventData.position);
        Block.SetBlockScale(BoardManager.Instance.BoardCellSize);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (Block == null)
        {
            Debug.Log("BlockSlot - Block is null");
            return;
        }

        if (BoardManager.Instance.CanPlaceBlock)
        {
            Block.PlaceBlock();
            OnBlockPlaced?.Invoke(Block);

            Destroy(Block.gameObject);
            Block = null;

            SetNewBlock();
        }

        // 제자리로
        else
        {
            Block.SetBlockScale(Block._slotBlockSize);
            (Block.transform as RectTransform).anchoredPosition = Vector2.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Block == null)
        {
            Debug.Log("BlockSlot - Block is null");
            return;
        }

        Block.MoveToPointer(transform as RectTransform, eventData.position);
    }
}