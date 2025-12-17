using UnityEngine;

public class Loading : MonoBehaviour
{
    [SerializeField]
    private LoadingUI _loadingUI;

    private void Awake()
    {
        SystemSetup();
    }

    private void SystemSetup()
    {
        // 다른 화면으로 가도 계속되게, 꺼지지 않게
        Application.runInBackground = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        _loadingUI.Play(OnAfterLoading);
    }

    private void OnAfterLoading()
    {

    }
}
