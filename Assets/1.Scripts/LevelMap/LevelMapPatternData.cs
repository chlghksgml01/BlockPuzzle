using UnityEngine;

/// <summary>
/// 레벨맵의 지그재그 노드/도로 배치 간격을 정의하는 데이터.
/// 기존 Level 씬의 LoadPreview에서 손으로 배치했던 값(노드 간격 180.5 / 190.5, 진폭 412.5 등)을 그대로 수치화한 것.
/// </summary>
[CreateAssetMenu(menuName = "LevelMap/Level Map Pattern", fileName = "LevelMapPattern")]
public class LevelMapPatternData : ScriptableObject
{
    [Header("Node Layout")]
    [Tooltip("짝수 인덱스 노드에서 Road를 거쳐 다음(홀수) 노드로 이어질 때의 세로 간격")]
    [SerializeField] private float _connectedNodeGap = 180.5f;

    [Tooltip("홀수 인덱스 노드에서 Road 없이 바로 다음(짝수) 노드로 이어질 때의 세로 간격")]
    [SerializeField] private float _directNodeGap = 190.5f;

    [Tooltip("노드가 좌우로 벌어지는 가로 진폭. 인덱스 4개 주기로 0, +진폭, 0, -진폭 순서로 반복")]
    [SerializeField] private float _horizontalAmplitude = 412.5f;

    [Header("Road Layout")]
    [Tooltip("Road가 좌/우 코너에 위치할 때의 가로 오프셋")]
    [SerializeField] private float _roadHorizontalOffset = 217f;

    [Tooltip("Road가 연결하는 두 노드 중 아래쪽(짝수 인덱스) 노드로부터 얼마나 위에 위치하는지")]
    [SerializeField] private float _roadVerticalOffsetFromLowerNode = 178.5f;

    [Header("Origin")]
    [Tooltip("0번 노드(맵 시작)가 Content 하단 기준으로부터 얼마나 떨어져 시작하는지")]
    [SerializeField] private float _originYOffset = 178.5f;

    public float ConnectedNodeGap => _connectedNodeGap;
    public float DirectNodeGap => _directNodeGap;
    public float HorizontalAmplitude => _horizontalAmplitude;
    public float RoadHorizontalOffset => _roadHorizontalOffset;
    public float RoadVerticalOffsetFromLowerNode => _roadVerticalOffsetFromLowerNode;
    public float OriginYOffset => _originYOffset;

    /// <summary>노드 2개 + Road 1개가 차지하는 세로 주기 길이</summary>
    public float PairCycleHeight => _connectedNodeGap + _directNodeGap;
}
