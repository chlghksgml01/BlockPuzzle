using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IInitializable
{
    [Header("Prefab")]
    [SerializeField] private DraggableBlock _blockPrefab;
    [Header("Spawn Effect")]
    [SerializeField] private float _popDuration = 0.3f;

    [Header("Test")]
    [SerializeField] private Sprite _testBlockSprite;
    [SerializeField] private BlockShape _testBlockShape;

    public DraggableBlock Block { get; private set; }

    public static event Action<int> OnBlockPlaced;
    public static event Action<Sprite> OnBlockSpritePlaced;
    public bool HasBlock { get; private set; }

    private IBoardHandler _boardHandler;
    private IBoardInfo _boardInfo;
    private InGameManager _inGame;

    public void Initialize(InitializeContext context)
    {
        _boardHandler = context.BoardManager;
        _boardInfo = context.BoardManager;
    }

    private void Start()
    {
        _inGame = InGameManager.Instance;
    }

    public void SpawnNewBlock(Sprite blockSprite, bool reduceLargeShapeSpawnRate = false)
    {
        HasBlock = true;
        Block = Instantiate(_blockPrefab, transform.position, transform.rotation, this.transform);
        if (_testBlockSprite != null && _testBlockShape != null)
            Block.InitializeBlock(_testBlockSprite, _testBlockShape, reduceLargeShapeSpawnRate);
        else if (_testBlockSprite != null && _testBlockShape == null)
            Block.InitializeBlock(_testBlockSprite, null, reduceLargeShapeSpawnRate);
        else if (_testBlockSprite == null && _testBlockShape != null)
            Block.InitializeBlock(blockSprite, _testBlockShape, reduceLargeShapeSpawnRate);
        else
            Block.InitializeBlock(blockSprite, null, reduceLargeShapeSpawnRate);

        Block.transform.localScale = Vector3.zero;
        Block.transform.DOScale(Vector3.one, _popDuration)
            .SetEase(Ease.OutBack)
            .SetLink(Block.gameObject, LinkBehaviour.KillOnDestroy);
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
        {
            Block.transform.DOKill();
            Destroy(Block.gameObject);
        }

        Block = null;
        HasBlock = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _inGame.StopHintCoroutine(Block, false);

        if (Block == null || !HasBlock)
            return;

        UpdateBlockPositionAndPreview(eventData);

        Block.BlockAnimate(_boardInfo.BoardCellSize);
        _inGame.StartHintCoroutine(Block);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (Block == null || !HasBlock)
        {
            Debug.Log("BlockSlot - Block is null");
            return;
        }

        if (_boardHandler.CanPlaceBlock)
        {
            _inGame.StopHintCoroutine(Block, true);
            Sprite placedSprite = Block.BlockSprite;
            if (_boardHandler.PlaceLastPreview(Block, Block.BlockSprite, out int placedCount))
            {
                RemoveBlock();
                OnBlockSpritePlaced?.Invoke(placedSprite);
                OnBlockPlaced?.Invoke(placedCount);
                SoundManager.Instance.PlaySFX(SFXType.PlaceBlock);
            }
            else
            {
                _boardHandler.ClearDragPreview();
                Block.BlockAnimate(Block.SlotBlockSize);
                (Block.transform as RectTransform).anchoredPosition = Vector2.zero;
                SoundManager.Instance.PlaySFX(SFXType.PlaceFailed);
            }
        }

        else
        {
            _boardHandler.ClearDragPreview();
            Block.BlockAnimate(Block.SlotBlockSize);
            (Block.transform as RectTransform).anchoredPosition = Vector2.zero;
            SoundManager.Instance.PlaySFX(SFXType.PlaceFailed);
        }
    }

    private void RemoveBlock()
    {
        if (Block != null)
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
            _boardHandler.UpdatePreviewFromScreen(Block, anchorScreenPos, anchorOffset, cam);
        else
            _boardHandler.UpdatePreviewFromScreen(Block, Block.GetScreenPosition(cam), Vector2Int.zero, cam);
    }
}