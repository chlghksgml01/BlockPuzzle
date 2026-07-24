using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가상화 풀에서 재사용되는 Road(노드 사이 연결로) 하나의 뷰.
/// 좌회전 구간은 동일 스프라이트를 Y축 180도 회전시켜 재사용한다(기존 씬 배치와 동일한 방식).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class LevelRoadView : MonoBehaviour
{
    private static readonly Quaternion MirroredRotation = Quaternion.Euler(0f, 180f, 0f);

    [Header("Fill")]
    [Tooltip("Road 진행률을 표시하는 Filled 타입 Image. 마지막 Road가 짝수 총 레벨 수로 인해 절반만 이어질 때 0.5로 덮어쓴다")]
    [SerializeField] private Image _fillImage;

    public RectTransform RectTransform { get; private set; }
    public int RoadPairIndex { get; private set; } = -1;

    private float _defaultFillAmount;

    private void Awake()
    {
        RectTransform = (RectTransform)transform;
        _defaultFillAmount = _fillImage.fillAmount;
    }

    public void Bind(int roadPairIndex, Vector2 anchoredPosition, bool mirrored, bool forceHalfFill)
    {
        RoadPairIndex = roadPairIndex;
        RectTransform.anchoredPosition = anchoredPosition;
        RectTransform.localRotation = mirrored ? MirroredRotation : Quaternion.identity;
        gameObject.name = mirrored ? $"Road_{roadPairIndex}_L" : $"Road_{roadPairIndex}_R";
        _fillImage.fillAmount = forceHalfFill ? 0.5f : _defaultFillAmount;
    }
}
