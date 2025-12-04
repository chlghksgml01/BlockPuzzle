using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

public class DraggableBlock : MonoBehaviour
{
    [SerializeField]
    private BlockShape[] _blockShapes;
    private BlockShape _shape;

    [SerializeField]
    public Sprite[] _blockSprites;
    private Sprite _blockSprite;

    private RectTransform _rectTransform;
    public GameObject _bodyTilePrefab;

    public float _slotBlockSize = 80f;
    public float _blockYOffset = 200f;

    private List<RectTransform> _bodyBlocks = new List<RectTransform>();

    private HashSet<BoardCell> _overlappedCells = new HashSet<BoardCell>();
    public List<BoardCell> _previewCells = new List<BoardCell>();

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        SetRandomBlockShape();
        SetRandomBlockSprite();
    }

    private void SetRandomBlockShape()
    {
        int index = Random.Range(0, _blockShapes.Length);
        _shape = _blockShapes[index];
    }

    private void SetRandomBlockSprite()
    {
        int index = Random.Range(0, _blockSprites.Length);
        _blockSprite = _blockSprites[index];
    }

    private void Start()
    {
        CreateBodyTiles();
    }

    private void CreateBodyTiles()
    {
        if (_shape == null || _shape._cellOffsets == null)
            return;

        RotateShapeRandomly();

        Vector2 center = CalculateCenter(_shape._cellOffsets);
        Vector2 tileSize = new Vector2(_slotBlockSize, _slotBlockSize);

        foreach (var offset in _shape._cellOffsets)
        {
            CreateTile(offset, center, tileSize);
        }
    }

    private void RotateShapeRandomly()
    {
        int randomRot = Random.Range(0, 4);

        for (int i = 0; i < _shape._cellOffsets.Length; i++)
        {
            Vector2Int offset = _shape._cellOffsets[i];

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

            _shape._cellOffsets[i] = offset;
        }
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

        // Collider
        var collider = tileObj.GetComponent<BoxCollider2D>();
        if (collider != null)
            collider.size = tileSize;

        // Image
        Image tileImage = tileObj.GetComponent<Image>();
        if (tileImage != null)
        {
            tileImage.sprite = _blockSprite;
            tileImage.raycastTarget = false;
            tileImage.color = Color.white;
        }

        // RectTransform 세팅
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
        // 블럭 이동
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(slotRect, screenMousePosition, null, out localPoint);
        _rectTransform.anchoredPosition = new Vector2(localPoint.x, localPoint.y + _blockYOffset);
    }

    public void SetBlockScale(float targetSize)
    {
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
            rectTransform.anchoredPosition = center + offset * scale;
            rectTransform.sizeDelta = new Vector2(targetSize, targetSize);

            rectTransform.GetComponent<BoxCollider2D>().size = new Vector2(targetSize, targetSize);
        }
    }

    public void OnTileEnterCell(BoardCell cell)
    {
        _overlappedCells.Add(cell);
        UpdatePreview();
    }

    public void OnTileExitCell(BoardCell cell)
    {
        _overlappedCells.Remove(cell);
        UpdatePreview();
    }

    public void UpdatePreview()
    {
        BoardManager.Instance.ClearAllPreview();

        // 배치 가능 검사
        if (!IsAllBodyBlockPlaceable())
        {
            BoardManager.Instance.CanPlaceBlock = false;
            return;
        }

        // 배치 가능한 셀에만 프리뷰 켜기
        foreach (var cell in _previewCells)
        {
            cell.UpdateCellVisual(true);
        }

        BoardManager.Instance.CanPlaceBlock = true;
    }

    public bool IsAllBodyBlockPlaceable()
    {
        _previewCells.Clear();
        _previewCells.AddRange(_overlappedCells);

        if (_overlappedCells.Count != _bodyBlocks.Count)
            return false;
        return true;
    }

    public void PlaceBlock()
    {
        foreach (BoardCell previewCell in _previewCells)
        {
            previewCell.PlaceBlock(_blockSprite);
        }
    }

    public int GetBlockCount() => _shape._cellOffsets.Length;
}