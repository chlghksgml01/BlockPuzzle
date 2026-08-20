using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Classic 스폰 가중치를 탐욕 봇으로 N판 시뮬레이션하는 에디터 창.
/// </summary>
public sealed class ClassicSimWindow : EditorWindow
{
    private const string ReportFileName = "ClassicSimReport.txt";

    [Header("Simulation")]
    [Tooltip("한 번에 돌릴 Classic 판 수")]
    [SerializeField] private int _gameCount = 100;

    [Tooltip("한 판의 최대 배치 수. 도달하면 timeout으로 센다")]
    [SerializeField] private int _maxTurns = 1000;

    [Tooltip("0이면 실행 시각 기반 시드")]
    [SerializeField] private int _seed;

    private Vector2 _scroll;
    private string _resultLog = "Classic 스폰 시뮬.\n탐욕 봇이 보드 규칙만 복제해 여러 판을 돌리고, 생존 턴/점수/모양별 스폰 비율을 출력합니다.\n";
    private bool _isRunning;

    [MenuItem("BlockPuzzle/Classic 스폰 시뮬")]
    public static void ShowWindow() => GetWindow<ClassicSimWindow>("Classic 스폰 시뮬");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Classic 스폰 시뮬", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "씬을 플레이하지 않습니다. DraggableBlock 가중치와 ScoreSystem 공식으로 헤드리스 시뮬레이션합니다.\n숫자는 탐욕 봇 기준이며 사람 점수와 같지 않습니다.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(_isRunning))
        {
            _gameCount = Mathf.Clamp(EditorGUILayout.IntField("시행 판 수", _gameCount), 1, 1000);
            _maxTurns = Mathf.Clamp(EditorGUILayout.IntField("최대 턴", _maxTurns), 10, 5000);
            _seed = EditorGUILayout.IntField("시드 (0=랜덤)", _seed);

            if (GUILayout.Button("시뮬레이션 실행"))
                RunSimulation();
        }

        if (_isRunning)
            EditorGUILayout.HelpBox("시뮬 진행 중...", MessageType.Info);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.TextArea(_resultLog, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void RunSimulation()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _resultLog = "시뮬 시작...\n";
        Repaint();

        try
        {
            ClassicSimConfig config = new ClassicSimConfig
            {
                GameCount = _gameCount,
                MaxTurns = _maxTurns,
                Seed = _seed
            };

            string loadError = ClassicSimAssetLoader.TryLoadInto(config);
            if (!string.IsNullOrEmpty(loadError))
            {
                _resultLog += loadError + "\n";
                return;
            }

            ClassicSimRunner runner = new ClassicSimRunner();
            ClassicSimRunResult result = runner.Run(config, (current, total) =>
            {
                bool canceled = EditorUtility.DisplayCancelableProgressBar(
                    "Classic 스폰 시뮬",
                    $"판 {current + 1}/{total}",
                    (current + 1) / (float)total);
                return canceled;
            });

            _resultLog = ClassicSimRunner.FormatReport(result, config);
            string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportFileName));
            File.WriteAllText(reportPath, _resultLog);
            _resultLog += $"\n리포트 저장: {reportPath}\n";
        }
        catch (Exception ex)
        {
            _resultLog += $"예외: {ex.Message}\n";
            Debug.LogException(ex);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            _isRunning = false;
            Repaint();
        }
    }
}
