using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// MissionData를 AI 검수용 요약 JSON으로 변환한다.
/// </summary>
public static class MissionSummaryExtractor
{
    [Serializable]
    private class MissionSummaryDto
    {
        public string missionName;
        public int boardSize;
        public string missionType;
        public bool isHard;
        public int filledCellCount;
        public int iceCellCount;
        public int grassCellCount;
        public int targetScore;
        public List<GemTargetDto> gemTargets;
    }

    [Serializable]
    private class GemTargetDto
    {
        public string type;
        public int count;
    }

    public static string ToSummaryJson(MissionData mission)
    {
        if (mission == null)
            throw new ArgumentNullException(nameof(mission));

        List<GemTargetInfo> gemTargets = mission.BuildGemTargets();
        List<GemTargetDto> gemTargetDtos = new List<GemTargetDto>(gemTargets.Count);
        for (int i = 0; i < gemTargets.Count; i++)
        {
            gemTargetDtos.Add(new GemTargetDto
            {
                type = gemTargets[i].gemType.ToString(),
                count = gemTargets[i].count
            });
        }

        int filledCellCount = mission.filledCells != null ? mission.filledCells.Count : 0;
        MissionSummaryDto dto = new MissionSummaryDto
        {
            missionName = mission.name,
            boardSize = mission.boardSize,
            missionType = mission.MissionType.ToString(),
            isHard = mission.IsHard,
            filledCellCount = filledCellCount,
            iceCellCount = mission.CountIceCells(),
            grassCellCount = mission.CountGrassCells(),
            targetScore = mission.TargetScore,
            gemTargets = gemTargetDtos
        };

        return JsonConvert.SerializeObject(dto);
    }
}
