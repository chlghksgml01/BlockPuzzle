using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 모든 MissionData를 Claude로 밸런스 검수하는 에디터 창.
/// </summary>
public class BalanceReviewWindow : EditorWindow
{
    [MenuItem("BlockPuzzle/AI 밸런스 검수")]
    public static void ShowWindow() => GetWindow<BalanceReviewWindow>("AI 밸런스 검수");

    private Vector2 _scroll;
    private string _resultLog = "";
    private bool _isRunning;

    private void OnGUI()
    {
        using (new EditorGUI.DisabledScope(_isRunning))
        {
            if (GUILayout.Button("모든 미션 검수 실행"))
            {
                RunReviewAll();
            }
        }

        if (_isRunning)
            EditorGUILayout.HelpBox("검수 진행 중...", MessageType.Info);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.TextArea(_resultLog, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private async void RunReviewAll()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _resultLog = "검수 시작...\n";
        Repaint();

        try
        {
            string[] guids = AssetDatabase.FindAssets("t:MissionData");
            if (guids == null || guids.Length == 0)
            {
                _resultLog += "MissionData 에셋을 찾지 못했습니다.\n";
                return;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                MissionData mission = AssetDatabase.LoadAssetAtPath<MissionData>(path);
                if (mission == null)
                {
                    _resultLog += $"[skip] 로드 실패: {path}\n\n";
                    Repaint();
                    continue;
                }

                string summary = MissionSummaryExtractor.ToSummaryJson(mission);
                string result = await ClaudeBalanceReviewer.ReviewMissionAsync(summary, mission.MissionType);
                if (string.IsNullOrEmpty(result))
                    result = "(검수 실패 — Console 로그 확인)";

                _resultLog += $"[{mission.name}]\n{result}\n\n";
                Repaint();
            }

            string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BalanceReviewReport.txt"));
            File.WriteAllText(reportPath, _resultLog);
            _resultLog += $"리포트 저장: {reportPath}\n";
        }
        catch (Exception ex)
        {
            _resultLog += $"예외: {ex.Message}\n";
            Debug.LogException(ex);
        }
        finally
        {
            _isRunning = false;
            Repaint();
        }
    }
}
