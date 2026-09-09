using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// MissionData를 AI 검수용 요약 JSON으로 변환한다.
/// 미션 타입에 실제로 필요한 필드만 담고, 타입과 무관한 필드에 값이 있으면
/// warnings로 옮겨 담아 토큰을 줄이면서 데이터 오염도 함께 알린다.
/// </summary>
public static class MissionSummaryExtractor
{
    /// <summary>null 필드는 직렬화에서 제외한다 (타입별 필드 가지치기).</summary>
    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    [Serializable]
    private class MissionSummaryDto
    {
        // 공통 필드 (항상 포함)
        public string missionName;
        public string missionType;
        public int boardSize;
        public bool isHard;
        public int filledCellCount;

        // 타입별 필드 (해당 타입일 때만 non-null → JSON에 등장)
        public int? targetScore;
        public int? iceCellCount;
        public int? grassCellCount;
        public List<GemTargetDto> gemTargets;

        // 타입과 무관한 필드에 값이 남아 있을 때만 채운다.
        public List<string> warnings;
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

        MissionType missionType = mission.MissionType;

        // 원본 값을 한 번씩만 계산한다.
        int filledCellCount = mission.filledCells != null ? mission.filledCells.Count : 0;
        int iceCellCount = mission.CountIceCells();
        int grassCellCount = mission.CountGrassCells();
        int targetScore = mission.TargetScore;
        List<GemTargetDto> gemTargetDtos = BuildGemTargetDtos(mission);

        MissionSummaryDto dto = new MissionSummaryDto
        {
            missionName = mission.name,
            missionType = missionType.ToString(),
            boardSize = mission.boardSize,
            isHard = mission.IsHard,
            filledCellCount = filledCellCount
        };

        // 해당 타입에 필요한 필드만 채운다.
        switch (missionType)
        {
            case MissionType.ScoreGoal:
                dto.targetScore = targetScore;
                break;
            case MissionType.Ice:
                dto.iceCellCount = iceCellCount;
                break;
            case MissionType.Grass:
                dto.grassCellCount = grassCellCount;
                break;
            case MissionType.Gem:
                dto.gemTargets = gemTargetDtos;
                break;
        }

        dto.warnings = BuildContaminationWarnings(
            missionType, targetScore, iceCellCount, grassCellCount, gemTargetDtos);

        return JsonConvert.SerializeObject(dto, SerializerSettings);
    }

    private static List<GemTargetDto> BuildGemTargetDtos(MissionData mission)
    {
        List<GemTargetInfo> gemTargets = mission.BuildGemTargets();
        List<GemTargetDto> dtos = new List<GemTargetDto>(gemTargets.Count);
        for (int i = 0; i < gemTargets.Count; i++)
        {
            dtos.Add(new GemTargetDto
            {
                type = gemTargets[i].gemType.ToString(),
                count = gemTargets[i].count
            });
        }

        return dtos;
    }

    /// <summary>
    /// 미션 타입과 무관한 필드에 유의미한 값이 남아 있으면 데이터 오염으로 보고한다.
    /// (필드를 JSON에서 뺐기 때문에 이 경고가 없으면 AI가 감지할 수 없다.)
    /// </summary>
    private static List<string> BuildContaminationWarnings(
        MissionType missionType,
        int targetScore,
        int iceCellCount,
        int grassCellCount,
        List<GemTargetDto> gemTargetDtos)
    {
        List<string> warnings = new List<string>();

        if (missionType != MissionType.ScoreGoal && targetScore > 0)
            warnings.Add($"targetScore({targetScore}) is set on a {missionType} mission (should be 0).");

        if (missionType != MissionType.Ice && iceCellCount > 0)
            warnings.Add($"{iceCellCount} ice cells exist on a {missionType} mission (unused).");

        if (missionType != MissionType.Grass && grassCellCount > 0)
            warnings.Add($"{grassCellCount} grass cells exist on a {missionType} mission (unused).");

        if (missionType != MissionType.Gem && HasGemTargets(gemTargetDtos))
            warnings.Add($"gemTargets are set on a {missionType} mission (unused).");

        if (missionType == MissionType.None)
            warnings.Add("missionType is None. A playable mission must set a concrete type.");

        return warnings.Count > 0 ? warnings : null;
    }

    private static bool HasGemTargets(List<GemTargetDto> gemTargetDtos)
    {
        if (gemTargetDtos == null)
            return false;

        for (int i = 0; i < gemTargetDtos.Count; i++)
        {
            if (gemTargetDtos[i].count > 0)
                return true;
        }

        return false;
    }
}
