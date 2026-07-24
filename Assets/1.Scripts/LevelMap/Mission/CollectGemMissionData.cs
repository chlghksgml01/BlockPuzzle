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
/// "보드 위 특정 블록을 목표 개수만큼 제거/수집" 미션 정의.
/// ICE 제거, GRASS 제거, 보석 수집은 데이터 형태(대상 종류 + 목표 개수)가 동일하여 하나의 클래스로 표현한다.
/// </summary>
[CreateAssetMenu(menuName = "LevelMap/Mission/Collect Gem Mission", fileName = "Mission_CollectGem")]
public sealed class CollectGemMissionData : LevelMissionData
{
    [Tooltip("수집해야 하는 보석 종류와 목표 개수")]
    [SerializeField] private List<GemTargetInfo> _gemTargets;

    public IReadOnlyList<GemTargetInfo> GemTargets => _gemTargets;
}
