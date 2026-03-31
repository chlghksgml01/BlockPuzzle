using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    private readonly Dictionary<Vector2Int, RectTransform> _tileByOffset = new Dictionary<Vector2Int, RectTransform>();

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void InitializeBlock(Sprite blockSprite, BlockShape blockshape = null)
    {
        _blockSprite = blockSprite;

        if (blockshape != null)
        {
            CurrentOffsets = (Vector2Int[])blockshape.CellOffsets.Clone();
        }

        else if (_blockShapes != null && _blockShapes.Length > 0)
        {
            int index = PickShapeIndexWeighted(_blockShapes);
            CurrentOffsets = (Vector2Int[])_blockShapes[index].CellOffsets.Clone();
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("Block Shapes is empty");
#endif
            return;
        }

        ApplyRandomRotation();

        CreateBodyTiles();
    }

    public void InitializeBlockFromOffsets(Sprite blockSprite, Vector2Int[] offsets)
    {
        _blockSprite = blockSprite;
        CurrentOffsets = (Vector2Int[])offsets.Clone();
        CreateBodyTiles();
    }

    private int PickShapeIndexWeighted(BlockShape[] shapes)
    {
        if (shapes == null || shapes.Length == 0)
            return 0;

        float total = 0f;
        for (int i = 0; i < shapes.Length; i++)
        {
            if (shapes[i] == null)
                continue;

            float weight = shapes[i].Weights;
            if (weight > 0f)
                total += weight;
        }

        if (total <= 0f)
            return Random.Range(0, shapes.Length);

        float roll = Random.value * total;
        float acc = 0f;

        for (int i = 0; i < shapes.Length; i++)
        {
            if (shapes[i] == null)
                continue;

            float w = shapes[i].Weights;
            if (w <= 0f || float.IsNaN(w) || float.IsInfinity(w))
                continue;

            acc += w;
            if (roll <= acc)
                return i;
        }

        for (int i = shapes.Length - 1; i >= 0; i--)
        {
            if (shapes[i] != null && shapes[i].Weights > 0f)
                return i;
        }

        return Random.Range(0, shapes.Length);
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
        _tileByOffset.Clear();

        foreach (Vector2Int offset in CurrentOffsets)
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

        foreach (Vector2Int offset in offsets)
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
        _tileByOffset[offset] = tileRect;
    }

    public void MoveToPointer(RectTransform slotRect, Vector2 screenMousePosition, Camera uiCam = null)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(slotRect, screenMousePosition, uiCam, out Vector2 localPoint);
        _rectTransform.anchoredPosition = new Vector2(localPoint.x, localPoint.y + _blockYOffset);
    }

    public Vector2 GetScreenPosition(Camera uiCam = null)
    {
        return RectTransformUtility.WorldToScreenPoint(uiCam, _rectTransform.position);
    }

    // 기준 블럭 찾고 화면상 좌표 계산
    public bool TryGetAnchorScreenPoint(Camera uiCam, out Vector2 anchorScreenPosition, out Vector2Int anchorOffset)
    {
        anchorScreenPosition = default;
        anchorOffset = default;

        if (CurrentOffsets == null || CurrentOffsets.Length == 0)
            return false;

        // 가장 왼쪽 위에 있는 블럭 찾기
        anchorOffset = CurrentOffsets[0];
        for (int i = 1; i < CurrentOffsets.Length; i++)
        {
            Vector2Int offset = CurrentOffsets[i];

            if (offset.x < anchorOffset.x || (offset.x == anchorOffset.x && offset.y > anchorOffset.y))
                anchorOffset = offset;
        }

        if (!_tileByOffset.TryGetValue(anchorOffset, out RectTransform anchorRect) || anchorRect == null)
            return false;

        anchorScreenPosition = RectTransformUtility.WorldToScreenPoint(uiCam, anchorRect.position);
        return true;
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
        if (BoardManager.HasInstance)
            BoardManager.Instance.ClearDragPreview();
    }
}