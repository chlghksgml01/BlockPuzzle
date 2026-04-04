using UnityEngine;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private LoadingUI _loadingUI;

    [Header("Scene Settings")]
    [SerializeField] private SceneName _nextScene;

    private void Awake()
    {
        SystemSetup();
        Application.targetFrameRate = 60;
    }

    private void SystemSetup()
    {
        Application.runInBackground = false;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        _loadingUI.Play(OnAfterLoading);
    }

    private void OnAfterLoading()
    {
        SceneLoadManager.LoadScene(_nextScene);
    }
}
