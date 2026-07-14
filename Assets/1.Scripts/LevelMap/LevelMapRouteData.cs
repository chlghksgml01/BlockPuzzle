using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨 노드에 표시할 특수 아이콘 종류. 참고 아트(Hierarchical Challenge)의 해골/보상 상자 노드를 표현하기 위함.
/// </summary>
public enum LevelNodeIconType
{
    Normal,
    Skull,
    Chest
}

[Serializable]
public class LevelMapNodeEntry
{
    [Tooltip("레벨 맵 콘텐츠(Content RectTransform) 기준 로컬 좌표. 상단이 0이고 아래로 갈수록 음수가 되도록 배치")]
    [SerializeField] private Vector2 _localPosition;
    public Vector2 LocalPosition => _localPosition;

    [Tooltip("이전 노드와 좌표가 대각선으로 어긋날 때, 연결 경로를 가로 방향부터 꺾을지 세로 방향부터 꺾을지 결정")]
    [SerializeField] private bool _bendHorizontalFirst = true;
    public bool BendHorizontalFirst => _bendHorizontalFirst;

    [Tooltip("노드에 표시할 특수 아이콘 (기본 Normal이면 숫자만 표시)")]
    [SerializeField] private LevelNodeIconType _iconType = LevelNodeIconType.Normal;
    public LevelNodeIconType IconType => _iconType;
}

[CreateAssetMenu(fileName = "LevelMapRouteData", menuName = "Game/Level Map Route")]
public class LevelMapRouteData : ScriptableObject
{
    [Header("Level Nodes")]
    [Tooltip("레벨 순서대로 배치한 노드 목록 (0번 인덱스 = 1번 레벨). 경로(선)는 인접한 두 노드 좌표로부터 자동 계산됨")]
    [SerializeField] private List<LevelMapNodeEntry> _nodes = new List<LevelMapNodeEntry>();
    public IReadOnlyList<LevelMapNodeEntry> Nodes => _nodes;

    public int LevelCount => _nodes.Count;
}
