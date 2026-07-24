using System.Collections.Generic;
using UnityEngine;

public enum GemType
{
    Pentagon,
    Square,
    Star
}

[System.Serializable]
public struct GemTargetInfo
{
    public GemType gemType;
    public int count;
}

/// <summary>
/// 보드 위 보석을 목표 개수만큼 수집하는 미션.
/// </summary>
[CreateAssetMenu(menuName = "LevelMap/Mission/Collect Gem Mission", fileName = "Mission_CollectGem")]
public sealed class CollectGemMissionData : LevelMissionData
{
    [Tooltip("수집해야 하는 보석 종류와 목표 개수")]
    [SerializeField] private List<GemTargetInfo> _gemTargets;

    public override MissionType MissionType => MissionType.Gem;

    public IReadOnlyList<GemTargetInfo> GemTargets => _gemTargets;
}
