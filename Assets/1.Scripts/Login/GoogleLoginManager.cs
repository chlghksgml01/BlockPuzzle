using BackEnd;
using System;
using TMPro;
using UnityEngine;

public class GoogleLoginManager : Singleton<GoogleLoginManager>
{
    public TMP_Text statusText;

    public static event Action<bool> OnLoginSucceed;

    private bool _isLoggingIn;

    /// <summary>뒤끝 페더레이션 로그인 완료 여부.</summary>
    public bool IsLoggedIn => Backend.IsLogin;

    public void StartGoogleLogin()
    {
        if (Backend.IsLogin)
        {
            UpdateStatus("Already logged in. Skip login.");
            OnLoginSucceed?.Invoke(true);
            return;
        }

        if (_isLoggingIn)
        {
            UpdateStatus("Login already in progress.");
            return;
        }

        _isLoggingIn = true;
        UpdateStatus("Starting Google Login...");
        TheBackend.ToolKit.GoogleLogin.Android.GoogleLogin(true, GoogleLoginCallback);
    }

    private void GoogleLoginCallback(bool isSuccess, string errorMessage, string token)
    {
        _isLoggingIn = false;

        if (isSuccess == false)
        {
            UpdateStatus("<color=red>Google Login Failed:</color>\n" + errorMessage);
            OnLoginSucceed?.Invoke(false);
            return;
        }

        UpdateStatus("Google Auth Success. Signing in to BackEnd...");

        // 뒤끝 페더레이션 로그인 시도
        var bro = Backend.BMember.AuthorizeFederation(token, FederationType.Google);

        if (bro.IsSuccess())
        {
            UpdateStatus("<color=green>BackEnd Login Success</color>");

            if (LeaderboardManager.HasInstance)
            {
                UpdateStatus("Login Succeed");
                OnLoginSucceed?.Invoke(true);
            }
        }
        else
        {
            UpdateStatus("<color=red>BackEnd Login Failed</color>\n" + bro.GetMessage());
            OnLoginSucceed?.Invoke(false);
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
