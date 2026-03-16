using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Prefab")]
    [SerializeField] private DraggableBlock _blockPrefab;

    public DraggableBlock Block { get; private set; }

    [Header("Test")]
    [SerializeField] private BlockShape _blockShapes;

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

        if (_blockShapes != null)
        {
            Block.SetBlockShape(_blockShapes);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        InGameManager.Instance.StopHintCoroutine(Block, false);

        if (Block == null || !HasBlock)
        {
            Debug.Log("BlockSlot - Block is null");
            return;
        }

        Block.MoveToPointer(transform as RectTransform, eventData.position);
        Block.SetBlockScale(BoardManager.Instance.BoardCellSize);

        InGameManager.Instance.StartHintCoroutine(Block);
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
            InGameManager.Instance.StopHintCoroutine(Block, true);
            Block.PlaceBlock();

            int blockShapeCount = Block.CurrentOffsets.Length;
            RemoveBlock();

            OnBlockPlaced?.Invoke(blockShapeCount);
        }

        // 제자리로
        else
        {
            Block.SetBlockScale(Block.SlotBlockSize);
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

    public void SpawnNewBlock()
    {
        if (HasBlock)
        {
            RemoveBlock();
        }
        SetNewBlock();
    }
}