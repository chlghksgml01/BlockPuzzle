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
        GetSHA1();
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

    public void GetSHA1()
    {
        try
        {
            using (var androidClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var context = androidClass.GetStatic<AndroidJavaObject>("currentActivity");
                var packageManager = context.Call<AndroidJavaObject>("getPackageManager");
                var packageName = context.Call<string>("getPackageName");

                // GET_SIGNATURES = 64
                var packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, 64);

                // 배열을 직접 가져오는 대신 인덱스로 접근하거나 JavaObject로 받습니다.
                var signatures = packageInfo.Get<AndroidJavaObject>("signatures");
                var signature = signatures.Call<AndroidJavaObject>("get", 0); // 배열의 첫번째 요소

                if (signature != null)
                {
                    // byte[] 대신 sbyte[]를 사용하여 경고를 방지합니다.
                    sbyte[] cert = signature.Call<sbyte[]>("toByteArray");

                    // MessageDigest 인스턴스 생성
                    using (var mdClass = new AndroidJavaClass("java.security.MessageDigest"))
                    {
                        using (var mdInstance = mdClass.CallStatic<AndroidJavaObject>("getInstance", "SHA-1"))
                        {
                            // digest는 인스턴스 메서드입니다!
                            byte[] sha1Byte = (byte[])(object)mdInstance.Call<sbyte[]>("digest", cert);

                            string sha1String = BitConverter.ToString(sha1Byte).Replace("-", ":");

                            UpdateStatus("SHA-1: " + sha1String);
                            Debug.Log("<color=yellow>[SHA-1 Fingerprint]</color> " + sha1String);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("SHA-1 추출 실패: " + e.Message);
            // 추출 실패 시 수동으로 입력한 SHA-1과 대조해보라는 안내가 필요합니다.
        }
    }
}
