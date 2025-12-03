using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DraggableBlock : MonoBehaviour
{
    [SerializeField]
    private BlockShape[] _blockShapes;
    private BlockShape _shape;

    public Sprite _blockSprite;
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

        _shape = GetRandomBlockShape();
    }

    private BlockShape GetRandomBlockShape()
    {
        int index = Random.Range(0, _blockShapes.Length);
        return _blockShapes[index];
    }

    private void Start()
    {
        CreateBodyTiles();
    }

    private void CreateBodyTiles()
    {
        if (_shape == null || _shape._cellOffsets == null)
            return;

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (var offset in _shape._cellOffsets)
        {
            if (offset.x < minX) minX = offset.x;
            if (offset.x > maxX) maxX = offset.x;
            if (offset.y < minY) minY = offset.y;
            if (offset.y > maxY) maxY = offset.y;
        }

        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        Vector2 tileSize = new Vector2(_slotBlockSize, _slotBlockSize);

        foreach (var offset in _shape._cellOffsets)
        {
            GameObject tileObj = Instantiate(_bodyTilePrefab, transform, false);
            tileObj.GetComponent<BoxCollider2D>().size = tileSize;

            Image tileImage = tileObj.GetComponent<Image>();
            tileImage.sprite = _blockSprite; // 나중에 랜덤으로 바꾸기
            tileImage.raycastTarget = false;
            tileImage.color = Color.white;

            RectTransform tileRect = tileObj.GetComponent<RectTransform>();
            tileRect.anchorMin = new Vector2(0.5f, 0.5f);
            tileRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);
            tileRect.sizeDelta = tileSize;

            float localX = (offset.x - centerX) * tileSize.x;
            float localY = (offset.y - centerY) * tileSize.y;
            tileRect.anchoredPosition = new Vector2(localX, localY);

            _bodyBlocks.Add(tileRect);
        }
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
            center += rectTransform.anchoredPosition;
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
            cell.UpdateCellCollision(true);
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
}