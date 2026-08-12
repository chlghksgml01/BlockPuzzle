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

        string prompt = $@"
다음은 블록 퍼즐 게임의 미션(레벨) 데이터입니다.
아래 기준으로 검수하고, JSON 형식으로만 답하세요 (설명 문장 없이):
1. isHard 플래그와 실제 난이도(보드 크기, 채워진 칸 수, 목표 점수)가 일치하는가
2. gemTargets 목표 개수가 filledCellCount 대비 비현실적으로 높지는 않은가 (클리어 불가능 위험)
3. targetScore가 boardSize 대비 너무 낮거나 높지 않은가

응답 형식:
{{""missionName"": string, ""riskLevel"": ""low""|""medium""|""high"", ""issues"": [string], ""suggestion"": string}}

미션 데이터:
{missionJson}";

        object requestBody = new
        {
            model = "claude-sonnet-4-6",
            max_tokens = 500,
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
