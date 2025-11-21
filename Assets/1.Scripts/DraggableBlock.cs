using System.Collections.Generic;
using System.Linq;
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

    private GameObject _previewRoot;
    private List<Image> _previewTiles = new List<Image>();
    private bool _previewValid;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        GetComponent<Image>().sprite = blockSprite;

        var rootImage = GetComponent<Image>();
        if (rootImage != null && blockSprite != null)
        {
            rootImage.sprite = blockSprite;
            rootImage.raycastTarget = false;
            rootImage.color = Color.white;
        }

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

        foreach (var offset in _shape._cellOffsets)
        {
            GameObject tileObj = new GameObject("BodyTile");
            tileObj.transform.SetParent(this.transform, false);

            // Image 컴포넌트 추가 및 설정
            Image tileImage = tileObj.AddComponent<Image>();
            tileImage.sprite = blockSprite;
            tileImage.raycastTarget = false;
            tileImage.color = Color.white;

            // 위치 설정 (셀 크기 1:1 가정, 필요시 조정)
            RectTransform tileRect = tileObj.GetComponent<RectTransform>();
            tileRect.anchorMin = new Vector2(0.5f, 0.5f);
            tileRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);
            tileRect.sizeDelta = _rectTransform.sizeDelta; // 부모와 동일 크기 사용
            tileRect.anchoredPosition = new Vector2(offset.x * tileRect.sizeDelta.x, offset.y * tileRect.sizeDelta.y);

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
        transform.SetParent(_canvas.transform);
    }
}