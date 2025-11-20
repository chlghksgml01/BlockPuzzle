using UnityEngine;
using UnityEngine.EventSystems;

// IBeginDragHandler, IDragHandler, IEndDragHandler 인터페이스 구현 -> EventSystem에서 드래그 이벤트 감지 가능
public class DraggableBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Canvas _canvas;
    public BoardManager _boardManager;
    public BlockShape _shape;

    private RectTransform _rectTransform;
    private Vector2 _originalAncoredPos; // 블록 원래 위치
    private Transform _originalParent;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        _originalAncoredPos = _rectTransform.anchoredPosition;
        _originalParent = _rectTransform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 드래그 한 블록 가장 맨 위로
        _rectTransform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransform canvasRect = _canvas.transform as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, _canvas.worldCamera, out localPoint))
        {
            // RectTransform : UI -> anchoredPosition이 월드 좌표보다 더 정확
            _rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        BoardCell targetCell = _boardManager.GetClosestCell(eventData.position);

        if (targetCell != null && !_boardManager.IsFilled(targetCell._x, targetCell._y))
        {
            RectTransform cellRect = targetCell.GetComponent<RectTransform>();

            _rectTransform.position = cellRect.position;
            _boardManager.SetFilled(targetCell._x, targetCell._y, true);
            enabled = false;
        }

        else
        {
            _rectTransform.anchoredPosition = _originalAncoredPos;
            _rectTransform.SetParent(_originalParent);
        }
    }
}