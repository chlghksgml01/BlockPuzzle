using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Prefab")]
    [SerializeField] private DraggableBlock _blockPrefab;
    [Header("Spawn Effect")]
    [SerializeField] private float _popDuration = 0.3f;

    public DraggableBlock Block { get; private set; }

    public static event Action<int> OnBlockPlaced;
    public static event Action<Sprite> OnBlockSpritePlaced;
    public bool HasBlock { get; private set; }

    private BoardManager _board;
    private InGameManager _inGame;

    private void Start()
    {
        _board = BoardManager.Instance;
        _inGame = InGameManager.Instance;
    }

    public void SpawnNewBlock(Sprite blockSprite)
    {
        HasBlock = true;
        Block = Instantiate(_blockPrefab, transform.position, transform.rotation, this.transform);
        Block.InitializeBlock(blockSprite);

        Block.transform.localScale = Vector3.zero;
        Block.transform.DOScale(Vector3.one, _popDuration).SetEase(Ease.OutBack);
    }

    public void SpawnSavedBlock(Sprite blockSprite, Vector2Int[] offsets)
    {
        HasBlock = true;
        Block = Instantiate(_blockPrefab, transform.position, transform.rotation, this.transform);
        Block.InitializeBlockFromOffsets(blockSprite, offsets);
        Block.transform.localScale = Vector3.one;
    }

    public void ClearSlotBlock()
    {
        if (Block != null)
            Destroy(Block.gameObject);

        Block = null;
        HasBlock = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _inGame.StopHintCoroutine(Block, false);

        if (Block == null || !HasBlock)
            return;

        UpdateBlockPositionAndPreview(eventData);

        Block.BlockAnimate(_board.BoardCellSize);
        _inGame.StartHintCoroutine(Block);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (Block == null || !HasBlock)
        {
            Debug.Log("BlockSlot - Block is null");
            return;
        }

        if (_board.CanPlaceBlock)
        {
            _inGame.StopHintCoroutine(Block, true);
            Sprite placedSprite = Block.BlockSprite;
            if (_board.PlaceLastPreview(Block, Block.BlockSprite, out int placedCount))
            {
                RemoveBlock();
                OnBlockSpritePlaced?.Invoke(placedSprite);
                OnBlockPlaced?.Invoke(placedCount);
                SoundManager.Instance.PlaySFX(SFXType.PlaceBlock);
            }
            else
            {
                _board.ClearDragPreview();
                Block.BlockAnimate(Block.SlotBlockSize);
                (Block.transform as RectTransform).anchoredPosition = Vector2.zero;
                SoundManager.Instance.PlaySFX(SFXType.PlaceFailed);
            }
        }

        else
        {
            _board.ClearDragPreview();
            Block.BlockAnimate(Block.SlotBlockSize);
            (Block.transform as RectTransform).anchoredPosition = Vector2.zero;
            SoundManager.Instance.PlaySFX(SFXType.PlaceFailed);
        }
    }

    private void RemoveBlock()
    {
        Block.transform.DOKill();

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

        if (Block.TryGetAnchorScreenPoint(cam, out Vector2 anchorScreenPos, out Vector2Int anchorOffset))
            _board.UpdatePreviewFromScreen(Block, anchorScreenPos, anchorOffset, cam);
        else
            _board.UpdatePreviewFromScreen(Block, Block.GetScreenPosition(cam), cam);
    }
}