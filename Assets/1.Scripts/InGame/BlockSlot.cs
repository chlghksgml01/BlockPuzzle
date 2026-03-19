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
        {
            Debug.Log("BlockSlot - Block is null");
            return;
        }

        Block.MoveToPointer(transform as RectTransform, eventData.position, eventData.pressEventCamera);
        Block.BlockAnimate(BoardManager.Instance.BoardCellSize);

        var cam = eventData.pressEventCamera;
        if (Block.TryGetAnchorScreenPoint(cam, out var anchorScreen, out var anchorOffset))
            BoardManager.Instance.UpdatePreviewFromScreen(Block, anchorScreen, anchorOffset, cam);
        else
            BoardManager.Instance.UpdatePreviewFromScreen(Block, Block.GetScreenPosition(cam), cam);
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

        // ???????
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
        {
            Debug.Log("BlockSlot - Block is null");
            return;
        }

        var cam = eventData.pressEventCamera;
        Block.MoveToPointer(transform as RectTransform, eventData.position, cam);
        if (Block.TryGetAnchorScreenPoint(cam, out var anchorScreen, out var anchorOffset))
            BoardManager.Instance.UpdatePreviewFromScreen(Block, anchorScreen, anchorOffset, cam);
        else
            BoardManager.Instance.UpdatePreviewFromScreen(Block, Block.GetScreenPosition(cam), cam);
    }
}