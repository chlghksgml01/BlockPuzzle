using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// IBeginDragHandler, IDragHandler, IEndDragHandler 인터페이스 구현 -> EventSystem에서 드래그 이벤트 감지 가능
public class DraggableBlock : MonoBehaviour
{
    private Canvas _canvas;
    public BlockShape _shape;

    public Sprite blockSprite;
    private RectTransform _rectTransform;

    public float _xOffset = 5f;

    private List<Image> _bodyTiles = new List<Image>();

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
        // 기존 타일 제거
        foreach (var tile in _bodyTiles)
        {
            if (tile != null)
                Destroy(tile.gameObject);
        }
        _bodyTiles.Clear();

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

        Vector2 tileSize = _rectTransform.sizeDelta;

        foreach (var offset in _shape._cellOffsets)
        {
            GameObject tileObj = new GameObject("BodyTile");
            tileObj.transform.SetParent(this.transform, false);

            // Image 컴포넌트 추가 및 설정
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

            _bodyTiles.Add(tileImage);
        }
    }

    public void OnSelect(PointerEventData eventData)
    {
    }

    public void MoveToPointer(Vector2 screenPosition)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            screenPosition,
            _canvas.worldCamera,
            out localPoint);

        _rectTransform.anchoredPosition = new Vector2(localPoint.x + _xOffset, localPoint.y);
    }

    public void OnRelease(PointerEventData eventData)
    {
    }

    public void InitDraggableBlock(Canvas canvas)
    {
        _canvas = canvas;
    }
}