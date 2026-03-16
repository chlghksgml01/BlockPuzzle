using BackEnd;
using TMPro;
using UnityEngine;

public class GoogleLoginManager : MonoBehaviour
{
    public TMP_Text statusText;

    public void StartGoogleLogin()
    {
        UpdateStatus("Starting Google Login...");
        TheBackend.ToolKit.GoogleLogin.Android.GoogleLogin(true, GoogleLoginCallback);
    }

    private void GoogleLoginCallback(bool isSuccess, string errorMessage, string token)
    {
        if (isSuccess == false)
        {
            UpdateStatus("<color=red>Google Login Failed:</color>\n" + errorMessage);
            return;
        }

        UpdateStatus("Google Auth Success. Signing in to BackEnd...");

        // 뒤끝 페더레이션 로그인 시도
        var bro = Backend.BMember.AuthorizeFederation(token, FederationType.Google);

        if (bro.IsSuccess())
        {
            UpdateStatus("<color=green>BackEnd Login Success</color>");

            if (SaveManager.Instance != null)
            {
                UpdateStatus("Syncing data with server...");
                SaveManager.Instance.SyncWithServer();
            }
        }
        else
        {
            UpdateStatus("<color=red>BackEnd Login Failed</color>\n" + bro.GetMessage());
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log("[Status] " + message);
    }
}
