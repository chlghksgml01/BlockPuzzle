using UnityEngine;

/// <summary>보드에서 제거된 미션 수집 블록 정보 (비행 연출용).</summary>
public struct MissionCollectInfo
{
    public Vector3 WorldPosition;
    public Sprite Sprite;
    public MissionType CollectType;
    public GemType GemType;
}
