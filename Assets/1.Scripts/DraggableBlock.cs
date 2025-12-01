using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableBlock : MonoBehaviour
{
    public BlockShape _shape;
    BoardManager BoardManager => BoardManager.Instance;

    public Sprite blockSprite;
    private RectTransform _rectTransform;

    public float _slotBlockSize = 80f;
    public float _boardBlockSize = 106f;
    public float _blockYOffset = 200f;

    private List<RectTransform> _bodyBlocks = new List<RectTransform>();

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
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
            GameObject tileObj = new GameObject("BodyTile");
            tileObj.transform.SetParent(this.transform, false);

            Image tileImage = tileObj.AddComponent<Image>();
            tileImage.sprite = blockSprite;
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

    public bool CanPlaceBlock()
    {
        Vector3 blockScreenPos = RectTransformUtility.WorldToScreenPoint(null, transform.position);

        if (BoardManager.CanPlaceBlock(blockScreenPos, _shape))
            return true;
        return false;
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
        }
    }
}