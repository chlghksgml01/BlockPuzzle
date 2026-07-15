using UnityEngine;

/// <summary>
/// 가상화 풀에서 재사용되는 레벨 노드 하나의 뷰.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class LevelNodeView : MonoBehaviour
{
    public RectTransform RectTransform { get; private set; }
    public int NodeIndex { get; private set; } = -1;

    private void Awake()
    {
        RectTransform = (RectTransform)transform;
    }

    public void Bind(int nodeIndex, Vector2 anchoredPosition)
    {
        NodeIndex = nodeIndex;
        RectTransform.anchoredPosition = anchoredPosition;
        gameObject.name = $"LevelNode_{nodeIndex + 1}";
    }
}
