using UnityEngine.EventSystems;

// DraggableBlock에 선택/해제 계약을 명시하는 인터페이스
public interface IBlockSelectable
{
    void OnSelect(PointerEventData eventData);   // 선택(포인터 다운)
    void OnRelease(PointerEventData eventData);  // 해제(포인터 업)
}