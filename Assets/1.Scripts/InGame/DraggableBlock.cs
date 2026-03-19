using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DraggableBlock : MonoBehaviour
{
    [Header("Block Data & Shapes")]
    [SerializeField] private BlockShape[] _blockShapes;

    public Vector2Int[] CurrentOffsets { get; private set; }
    private Sprite _blockSprite;

    [Header("Prefab & Settings")]
    [SerializeField] private float _scaleDuration = 0.2f;
    [SerializeField] private GameObject _bodyTilePrefab;

    [Header("UI Layout & Positioning")]
    [SerializeField] private float _slotBlockSize = 80f;
    [SerializeField] private float _blockYOffset = 200f;

    public float SlotBlockSize => _slotBlockSize;
    public float BlockYOffset => _blockYOffset;

    private RectTransform _rectTransform;
    private List<RectTransform> _bodyBlocks = new List<RectTransform>();
    public Sprite BlockSprite => _blockSprite;
    public RectTransform RectTransform => _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void InitializeBlock(Sprite blockSprite)
    {
        _blockSprite = blockSprite;

        if (_blockShapes != null && _blockShapes.Length > 0)
        {
            int index = Random.Range(0, _blockShapes.Length);
            CurrentOffsets = (Vector2Int[])_blockShapes[index]._cellOffsets.Clone();
        }
        else
        {
            Debug.LogError("Block Shapes is empty");
            return;
        }

        ApplyRandomRotation();

        CreateBodyTiles();
    }

    private void ApplyRandomRotation()
    {
        int randomRot = Random.Range(0, 4);
        if (randomRot == 0)
            return;

        for (int i = 0; i < CurrentOffsets.Length; i++)
        {
            CurrentOffsets[i] = Rotate(randomRot, CurrentOffsets[i]);
        }

        NormalizeOffsets(CurrentOffsets);
    }

    private void CreateBodyTiles()
    {
        if (CurrentOffsets == null || CurrentOffsets.Length == 0)
            return;

        Vector2 center = CalculateCenter(CurrentOffsets);
        Vector2 tileSize = new Vector2(_slotBlockSize, _slotBlockSize);

        foreach (Transform child in transform) { Destroy(child.gameObject); }
        _bodyBlocks.Clear();

        foreach (var offset in CurrentOffsets)
        {
            CreateTile(offset, center, tileSize);
        }
    }

    private static void NormalizeOffsets(Vector2Int[] offsets)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;

        for (int i = 0; i < offsets.Length; i++)
        {
            if (offsets[i].x < minX) minX = offsets[i].x;
            if (offsets[i].y < minY) minY = offsets[i].y;
        }

        for (int i = 0; i < offsets.Length; i++)
            offsets[i] = new Vector2Int(offsets[i].x - minX, offsets[i].y - minY);
    }

    private static Vector2Int Rotate(int randomRot, Vector2Int offset)
    {
        switch (randomRot)
        {
            case 1:
                offset = new Vector2Int(offset.y, -offset.x);
                break;
            case 2:
                offset = new Vector2Int(-offset.x, -offset.y);
                break;
            case 3:
                offset = new Vector2Int(-offset.y, offset.x);
                break;
            default:
                break;
        }

        return offset;
    }

    private Vector2 CalculateCenter(Vector2Int[] offsets)
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var offset in offsets)
        {
            if (offset.x < minX)
                minX = offset.x;
            if (offset.x > maxX)
                maxX = offset.x;
            if (offset.y < minY)
                minY = offset.y;
            if (offset.y > maxY)
                maxY = offset.y;
        }

        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        return new Vector2(centerX, centerY);
    }

    private void CreateTile(Vector2Int offset, Vector2 center, Vector2 tileSize)
    {
        GameObject tileObj = Instantiate(_bodyTilePrefab, transform, false);

        // Image
        Image tileImage = tileObj.GetComponent<Image>();
        if (tileImage != null)
        {
            tileImage.sprite = _blockSprite;
            tileImage.raycastTarget = false;
            tileImage.color = Color.white;
        }

        // RectTransform
        RectTransform tileRect = tileObj.GetComponent<RectTransform>();
        tileRect.anchorMin = new Vector2(0.5f, 0.5f);
        tileRect.anchorMax = new Vector2(0.5f, 0.5f);
        tileRect.pivot = new Vector2(0.5f, 0.5f);
        tileRect.sizeDelta = tileSize;

        float localX = (offset.x - center.x) * tileSize.x;
        float localY = (offset.y - center.y) * tileSize.y;

        tileRect.anchoredPosition = new Vector2(localX, localY);

        _bodyBlocks.Add(tileRect);
    }

    public void MoveToPointer(RectTransform slotRect, Vector2 screenMousePosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(slotRect, screenMousePosition, null, out Vector2 localPoint);
        _rectTransform.anchoredPosition = new Vector2(localPoint.x, localPoint.y + _blockYOffset);
    }

    public Vector2 GetScreenPosition(Camera uiCam = null)
    {
        return RectTransformUtility.WorldToScreenPoint(uiCam, _rectTransform.position);
    }

    public void BlockAnimate(float targetSize)
    {
        if (_bodyBlocks == null || _bodyBlocks.Count == 0)
            return;

        Vector2 center = Vector2.zero;
        foreach (RectTransform rectTransform in _bodyBlocks)
        {
            center += rectTransform.anchoredPosition;
        }
        center /= _bodyBlocks.Count;

        foreach (RectTransform rectTransform in _bodyBlocks)
        {
            float currentSize = rectTransform.sizeDelta.x;
            float scale = targetSize / currentSize;

            Vector2 offset = rectTransform.anchoredPosition - center;
            Vector2 targetPosition = center + offset * scale;
            Vector2 targetSizeDelta = new Vector2(targetSize, targetSize);

            rectTransform.DOAnchorPos(targetPosition, _scaleDuration);

            rectTransform.DOSizeDelta(targetSizeDelta, _scaleDuration);
        }
    }

    public void ClearBoardPreview()
    {
        if (BoardManager.Instance != null)
            BoardManager.Instance.ClearDragPreview();
    }
}