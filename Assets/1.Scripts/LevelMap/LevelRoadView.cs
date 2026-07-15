using UnityEngine;

/// <summary>
/// 가상화 풀에서 재사용되는 Road(노드 사이 연결로) 하나의 뷰.
/// 좌회전 구간은 동일 스프라이트를 Y축 180도 회전시켜 재사용한다(기존 씬 배치와 동일한 방식).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class LevelRoadView : MonoBehaviour
{
    private static readonly Quaternion MirroredRotation = Quaternion.Euler(0f, 180f, 0f);

    public RectTransform RectTransform { get; private set; }
    public int RoadPairIndex { get; private set; } = -1;

    private void Awake()
    {
        RectTransform = (RectTransform)transform;
    }

    public void Bind(int roadPairIndex, Vector2 anchoredPosition, bool mirrored)
    {
        RoadPairIndex = roadPairIndex;
        RectTransform.anchoredPosition = anchoredPosition;
        RectTransform.localRotation = mirrored ? MirroredRotation : Quaternion.identity;
        gameObject.name = mirrored ? $"Road_{roadPairIndex}_L" : $"Road_{roadPairIndex}_R";
    }
}
