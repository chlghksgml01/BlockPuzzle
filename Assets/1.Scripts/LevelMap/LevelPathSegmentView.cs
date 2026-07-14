using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨 경로 막대 하나를 표시하는 뷰. LevelMapModel이 계산한 위치/크기/회전을 그대로 적용만 한다.
/// </summary>
public class LevelPathSegmentView : MonoBehaviour
{
    [Tooltip("경로 막대 이미지 (Sliced 타입 스프라이트 권장, 예: 01Cycle)")]
    [SerializeField] private Image _image;

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
    }

    public void Bind(PathSegmentLayout layout)
    {
        _rectTransform.anchoredPosition = layout.CenterPosition;
        _rectTransform.sizeDelta = layout.Size;
        _rectTransform.localRotation = Quaternion.Euler(0f, 0f, layout.RotationZ);
    }
}
