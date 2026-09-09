using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Anthropic Messages API로 미션 밸런스를 검수한다.
/// </summary>
public static class ClaudeBalanceReviewer
{
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        HttpClient client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(60);
        return client;
    }

    public static async Task<string> ReviewMissionAsync(string missionJson, MissionType missionType)
    {
        string apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("ANTHROPIC_API_KEY 환경변수가 설정되어 있지 않습니다.");
            return null;
        }

        string prompt = BuildReviewPrompt(missionType, missionJson);

        object requestBody = new
        {
            model = "claude-sonnet-4-6",
            max_tokens = 700,
            messages = new[] { new { role = "user", content = prompt } }
        };

        string json = JsonConvert.SerializeObject(requestBody);
        StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = content;

        HttpResponseMessage response = await Client.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Debug.LogError($"Claude API 실패 ({(int)response.StatusCode}): {responseBody}");
            return null;
        }

        return ExtractAssistantText(responseBody);
    }

    /// <summary>
    /// 검수 대상 미션 타입의 규칙·기준만 담은 프롬프트를 만든다.
    /// 다른 타입 규칙은 넣지 않아 토큰과 오판 여지를 줄인다.
    /// </summary>
    private static string BuildReviewPrompt(MissionType missionType, string missionJson)
    {
        return
            "You are a level balance auditor for this block puzzle game. Judge solely based on the game rules below. Do not draw from general puzzle common sense or mission rules from other games.\n\n" +

            "## Common Game Rules\n" +
            "- Drag and place blocks onto an NxN board; filling a horizontal or vertical line clears that line.\n" +
            "- The mission is Game Over when there is no space on the board to place the slot blocks.\n" +
            "- isHard is an intended difficulty flag. Flag any mismatch between isHard and the actual difficulty.\n" +
            "- The mission data below contains ONLY the fields relevant to this mission type. Do not ask for other fields or suggest adding other target types.\n" +
            "- If a 'warnings' array is present, each entry is a data-integrity problem detected in the asset. Include every warning in 'issues' and raise riskLevel accordingly (usually 'high').\n\n" +

            BuildMissionTypeSection(missionType) +

            "## Audit Criteria\n" +
            "1. Is the clear condition field for this mission type present and valid? (High risk if missing or zero/empty.)\n" +
            "2. Does the actual difficulty match isHard, judging only from the fields provided?\n" +
            "3. Is the clear condition unrealistically easy or difficult? " + BuildRealismCriterion(missionType) + "\n\n" +

            "## DO NOTs\n" +
            "- Do NOT claim 'purpose is unclear' just because an Ice/Grass count is low. A low count only means an easy mission.\n" +
            "- Do NOT request fields that are absent from the mission data.\n" +
            "- Answer ONLY in JSON without introductory or explanatory prose.\n\n" +

            "Response Format:\n" +
            "{\"missionName\": string, \"riskLevel\": \"low\"|\"medium\"|\"high\", \"issues\": [string], \"suggestion\": string}\n\n" +
            "Mission Data:\n" +
            missionJson;
    }

    /// <summary>미션 타입별 클리어 규칙 섹션.</summary>
    private static string BuildMissionTypeSection(MissionType missionType)
    {
        switch (missionType)
        {
            case MissionType.Ice:
                return
                    "## Mission Type: Ice\n" +
                    "- Clear Condition: Remove all ice cells on the board by clearing lines.\n" +
                    "- Ice cells have stages (01~03) and may need the same cell's line cleared multiple times to disappear. Only the count is shown.\n" +
                    "- Relevant fields: iceCellCount, filledCellCount, boardSize, isHard.\n" +
                    "- A low iceCellCount is a valid, easy mission - not a design flaw.\n" +
                    "- If iceCellCount is 0 or missing, there is no clear condition (high risk).\n\n";

            case MissionType.Grass:
                return
                    "## Mission Type: Grass\n" +
                    "- Clear Condition: Remove all grass cells on the board by clearing lines.\n" +
                    "- If a line containing grass is not cleared for 3 consecutive turns, grass spreads by 1 cell into an adjacent empty space, increasing the remaining target.\n" +
                    "- Relevant fields: grassCellCount, filledCellCount, boardSize, isHard.\n" +
                    "- A low grassCellCount is a valid, easy mission.\n" +
                    "- A large board with many empty spaces can make the late game hard because of spreading.\n" +
                    "- If grassCellCount is 0 or missing, there is no clear condition (high risk).\n\n";

            case MissionType.Gem:
                return
                    "## Mission Type: Gem\n" +
                    "- Clear Condition: Place gem blocks spawned from slots onto the board and collect the target quantities of the specified gem types by clearing lines.\n" +
                    "- Gems spawn from slots, not from initial board placement. Do NOT compare filledCellCount directly with gem targets.\n" +
                    "- Relevant fields: gemTargets, filledCellCount (board placement pressure), boardSize, isHard.\n" +
                    "- If gemTargets is missing/empty or its total is 0, there is no clear condition (high risk).\n\n";

            case MissionType.ScoreGoal:
                return
                    "## Mission Type: ScoreGoal\n" +
                    "- Clear Condition: Reach targetScore. There is no time limit.\n" +
                    "- Scoring: Block placement + line clear. One line is roughly boardSize * 5, with multi-line/combo bonuses.\n" +
                    "- Relevant fields: targetScore, filledCellCount, boardSize, isHard.\n" +
                    "- filledCellCount of 0 is normal for an empty-board start.\n" +
                    "- Flag if targetScore is 0 or less or missing (high risk).\n\n";

            default:
                return
                    "## Mission Type: None / Unrecognized\n" +
                    "- This mission has no concrete type set. This is a configuration error - riskLevel must be 'high'.\n\n";
        }
    }

    /// <summary>Audit Criteria 3번의 타입별 현실성 판단 기준.</summary>
    private static string BuildRealismCriterion(MissionType missionType)
    {
        switch (missionType)
        {
            case MissionType.Ice:
            case MissionType.Grass:
                return "Compare the target cell count against board size and pre-filled density.";
            case MissionType.Gem:
                return "Check whether target counts are excessively high or gem types are needlessly varied.";
            case MissionType.ScoreGoal:
                return "Check whether targetScore is excessively low or high relative to board size.";
            default:
                return "Not applicable.";
        }
    }

    /// <summary>
    /// Messages API 래퍼에서 assistant text만 꺼낸다.
    /// </summary>
    private static string ExtractAssistantText(string responseBody)
    {
        if (string.IsNullOrEmpty(responseBody))
            return null;

        try
        {
            JObject root = JObject.Parse(responseBody);
            JToken textToken = root["content"]?[0]?["text"];
            if (textToken == null || textToken.Type == JTokenType.Null)
            {
                Debug.LogError($"Claude 응답에서 content[0].text를 찾지 못했습니다: {responseBody}");
                return null;
            }

            return textToken.ToString();
        }
        catch (JsonException ex)
        {
            Debug.LogError($"Claude 응답 파싱 실패: {ex.Message}\n{responseBody}");
            return null;
        }
    }
}
