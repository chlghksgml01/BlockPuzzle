using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Prefab")]
    [SerializeField] private DraggableBlock _blockPrefab;

    public DraggableBlock Block { get; private set; }

    public static event Action<int> OnBlockPlaced;
    public bool HasBlock { get; private set; }

    public void SpawnNewBlock(Sprite blockSprite)
    {
        HasBlock = true;
        Block = Instantiate(_blockPrefab, transform.position, transform.rotation, this.transform);

        Block.InitializeBlock(blockSprite);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        InGameManager.Instance.StopHintCoroutine(Block, false);

        if (Block == null || !HasBlock)
            return;

        UpdateBlockPositionAndPreview(eventData);

        Block.BlockAnimate(BoardManager.Instance.BoardCellSize);
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
            BoardManager.Instance.PlaceLastPreview(Block, Block.BlockSprite);

            int blockShapeCount = Block.CurrentOffsets.Length;
            RemoveBlock();

            OnBlockPlaced?.Invoke(blockShapeCount);
        }

        else
        {
            BoardManager.Instance.ClearDragPreview();
            Block.BlockAnimate(Block.SlotBlockSize);
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
            return;

        UpdateBlockPositionAndPreview(eventData);
    }

    private void UpdateBlockPositionAndPreview(PointerEventData eventData)
    {
        Camera cam = eventData.pressEventCamera;
        RectTransform slotRect = transform as RectTransform;

        Block.MoveToPointer(slotRect, eventData.position, cam);

        if (Block.TryGetAnchorScreenPoint(cam, out Vector2 anchorScreen, out Vector2Int anchorOffset))
            BoardManager.Instance.UpdatePreviewFromScreen(Block, anchorScreen, anchorOffset, cam);
        else
            BoardManager.Instance.UpdatePreviewFromScreen(Block, Block.GetScreenPosition(cam), cam);
    }
}