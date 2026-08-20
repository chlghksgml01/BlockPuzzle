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

    public static async Task<string> ReviewMissionAsync(string missionJson)
    {
        string apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("ANTHROPIC_API_KEY 환경변수가 설정되어 있지 않습니다.");
            return null;
        }

        string prompt = BuildReviewPrompt(missionJson);

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
    /// 미션 타입별 클리어 규칙과 검수 기준을 포함한 프롬프트를 만든다.
    /// </summary>
    private static string BuildReviewPrompt(string missionJson)
    {
        return
            "You are a level balance auditor for this block puzzle game. Judge solely based on the game rules below. Do not draw from general puzzle common sense or mission rules from other games.\n\n" +

            "## Game Rules\n" +
            "- Drag and place blocks onto an NxN board; filling a horizontal or vertical line clears that line.\n" +
            "- All missions result in Game Over when there is no space on the board to place the slot blocks.\n" +
            "- A single mission uses only ONE of Ice / Grass / Gem / ScoreGoal. Do NOT mix types.\n" +
            "- isHard is an intended difficulty flag. Flag any mismatch between isHard and the actual difficulty.\n" +
            "- Do not inspect irrelevant fields, and do not suggest adding other target types.\n\n" +

            "### Ice\n" +
            "- Clear Condition: Remove all ice cells on the board by clearing lines.\n" +
            "- Ice cells have stages (01~03) and may require lines to be cleared multiple times on the same cell to disappear. Only counts are shown in the summary.\n" +
            "- Relevant fields: iceCellCount, filledCellCount, boardSize, isHard.\n" +
            "- Even if iceCellCount is low, the clear condition remains valid. A low count indicates an easy mission, not a design flaw.\n" +
            "- If iceCellCount is 0, there is no clear condition.\n" +
            "- Ignore targetScore and gemTargets.\n\n" +

            "### Grass\n" +
            "- Clear Condition: Remove all grass cells on the board by clearing lines.\n" +
            "- If a line containing grass is not cleared for 3 consecutive turns, grass spreads by 1 cell into an adjacent empty space. This increases the remaining target count.\n" +
            "- Relevant fields: grassCellCount, filledCellCount, boardSize, isHard.\n" +
            "- Even if grassCellCount is low, the clear condition remains valid. A low count indicates an easy mission.\n" +
            "- However, if the board is large with many empty spaces, spreading may make the late game difficult.\n" +
            "- If grassCellCount is 0, there is no clear condition.\n" +
            "- Ignore targetScore and gemTargets.\n\n" +

            "### Gem\n" +
            "- Clear Condition: Place gem blocks spawned from slots onto the board and collect target quantities of specified gem types by clearing lines.\n" +
            "- Gems spawn from slots, not from initial board placement. Do NOT directly compare filledCellCount with gem targets.\n" +
            "- Relevant fields: gemTargets, filledCellCount (board placement pressure), boardSize, isHard.\n" +
            "- If gemTargets is empty or the total sum is 0, there is no clear condition.\n" +
            "- Ignore targetScore, iceCellCount, and grassCellCount.\n\n" +

            "### ScoreGoal\n" +
            "- Clear Condition: Reach the target score (targetScore). There is no time limit.\n" +
            "- Scoring: Block placement + Line clear. Base score for one line is approximately boardSize * 5, with multi-line/combo bonuses applied.\n" +
            "- Relevant fields: targetScore, filledCellCount, boardSize, isHard.\n" +
            "- filledCellCount of 0 is normal for an empty board start.\n" +
            "- Flag if targetScore is 0 or less.\n" +
            "- Ignore gemTargets, iceCellCount, and grassCellCount.\n\n" +

            "## Audit Criteria\n" +
            "1. Is the clear condition field corresponding to missionType valid? (High risk if missing).\n" +
            "2. Does actual difficulty match isHard? Look ONLY at relevant fields for that mission type.\n" +
            "3. Is the clear condition unrealistically easy or difficult?\n" +
            "   - Ice/Grass: Target cell count vs. board size and pre-filled density.\n" +
            "   - Gem: Whether target counts are excessively high or gem types are unnecessarily varied.\n" +
            "   - ScoreGoal: Whether targetScore is excessively low or high relative to board size.\n\n" +

            "## DO NOTs\n" +
            "- Do NOT ask to add gemTargets to Ice/Grass/ScoreGoal.\n" +
            "- Do NOT point out that targetScore is 0 in Ice/Grass.\n" +
            "- Do NOT claim 'purpose is unclear' just because Ice/Grass count is low.\n" +
            "- Answer ONLY in JSON without introductory or explanatory prose.\n\n" +

            "Response Format:\n" +
            "{\"missionName\": string, \"riskLevel\": \"low\"|\"medium\"|\"high\", \"issues\": [string], \"suggestion\": string}\n\n" + "Mission Data:\n" +
            missionJson;
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
