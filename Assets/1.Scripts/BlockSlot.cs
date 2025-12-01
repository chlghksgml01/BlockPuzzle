using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

// BlockSlot에 PreviewBlock이 있음 - PreviewBlock : DraggableBlock의 미리보기 버전
// BlockSlot를 선택하면 PreviewBlock 이랑 똑같은 모양의 DraggableBlock이 포인터의 살짝 위쪽에 생성(x좌표는 같음)
// 이 DraggableBlock을 끌어서 Board 위에 올려놓으면 BoardManager에서 해당 위치에 블록을 놓을 수 있는지 검사
public class BlockSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Canvas _canvas;
    public DraggableBlock _blockPrefab;

    private DraggableBlock _block;

    private void Awake()
    {
        // 임시 코드
        SetBlock(_blockPrefab);
    }

    public void SetBlock(DraggableBlock block)
    {
        _block = Instantiate(block, transform.position, transform.rotation, this.transform);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_block != null)
        {
            _block.MoveToPointer(transform as RectTransform, eventData.position);
            _block.SetBlockScale(_block._boardBlockSize);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_block != null)
        {
            _block.SetBlockScale(_block._slotBlockSize);

            if (!_block.CanPlaceBlock())
                (_block.transform as RectTransform).anchoredPosition = Vector2.zero;
            else
            {

            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_block != null)
        {
            _block.MoveToPointer(transform as RectTransform, eventData.position);
        }
    }
}