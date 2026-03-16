using BackEnd;
using System;
using System.Security.Cryptography;
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
            Debug.LogError(errorMessage);
            return;
        }

        UpdateStatus("Google Auth Success. Signing in to BackEnd...");
        Debug.Log("Google Token : " + token);

        var bro = Backend.BMember.AuthorizeFederation(token, FederationType.Google);

        if (bro.IsSuccess())
        {
            UpdateStatus("<color=green>BackEnd Login Success</color>");
        }
        else
        {
            UpdateStatus("<color=red>BackEnd Login Failed</color>\n" + bro.GetMessage());
        }

        Debug.Log("Federation Login Result : " + bro);
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
