using System;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

public class BlockSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Canvas _canvas;
    public DraggableBlock _blockPrefab;

    public DraggableBlock Block { get; private set; }

    public static event Action<int> OnBlockPlaced;

    public bool HasBlock { get; private set; }

    private void Awake()
    {
        SetNewBlock();
    }

    public void SetNewBlock()
    {
        HasBlock = true;
        Block = Instantiate(_blockPrefab, transform.position, transform.rotation, this.transform);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Block == null || !HasBlock)
        {
            Debug.Log("BlockSlot - Block is null");
            return;
        }

        Block.MoveToPointer(transform as RectTransform, eventData.position);
        Block.SetBlockScale(BoardManager.Instance.BoardCellSize);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (Block == null || !HasBlock)
        {
            Debug.Log("BlockSlot - Block is null");
            return;
        }

        if (BoardManager.Instance.CanPlaceBlock)
        {
            Block.PlaceBlock();

            int blockShapeCount = Block.Shape._cellOffsets.Length;
            RemoveBlock();

            OnBlockPlaced?.Invoke(blockShapeCount);
        }

        // 제자리로
        else
        {
            Block.SetBlockScale(Block._slotBlockSize);
            (Block.transform as RectTransform).anchoredPosition = Vector2.zero;
        }
    }

    private void RemoveBlock()
    {
        HasBlock = false;
        Destroy(Block.gameObject);
        Block = null;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Block == null || !HasBlock)
        {
            Debug.Log("BlockSlot - Block is null");
            return;
        }

        Block.MoveToPointer(transform as RectTransform, eventData.position);
    }
}