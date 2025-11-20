using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// TouchArea: 블록 주변에 배치되는 투명 버튼/영역.
// PointerDown/Up 이벤트를 받아 연결된 IBlockSelectable에 전달함.
[RequireComponent(typeof(Image))]
public class TouchArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public MonoBehaviour _target; // 할당: DraggableBlock (IBlockSelectable 구현체)

    private IBlockSelectable _selectable;

    private void Awake()
    {
        if (_target != null) _selectable = _target as IBlockSelectable;
        var img = GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
            img.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _selectable?.OnSelect(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _selectable?.OnRelease(eventData);
    }

    public void SetTarget(MonoBehaviour target)
    {
        _target = target;
        _selectable = _target as IBlockSelectable;
    }
}