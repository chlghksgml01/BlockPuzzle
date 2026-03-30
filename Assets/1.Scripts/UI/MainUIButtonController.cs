using UnityEngine;
using UnityEngine.UI;

public class MainUIButtonController : MonoBehaviour
{
    [SerializeField] private Button _leaderBoardButton;
    [SerializeField] private Button _startButton;
    [SerializeField] private LeaderboardUI _leaderBoardUI;

    private void Start()
    {
        _leaderBoardButton.onClick.AddListener(() => _leaderBoardUI.Open());
        _startButton.onClick.AddListener(() => SceneLoadManager.Instance.LoadScene(SceneName.InGame));
    }
}
