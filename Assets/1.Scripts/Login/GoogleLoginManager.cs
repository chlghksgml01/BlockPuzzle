using UnityEngine;
using BackEnd;
using TheBackend.ToolKit.GoogleLogin;

public class GoogleLoginManager : MonoBehaviour
{
    public void OnClick_GoogleLogin()
    {
        Debug.Log("Google 로그인 시작");
        Android.GoogleLogin(false, OnGoogleLoginResult);
    }

    private void OnGoogleLoginResult(bool isSuccess, string errorMsg, string token)
    {
        if (!isSuccess)
        {
            Debug.LogError($"구글 로그인 실패: {errorMsg}");
            return;
        }

        Debug.Log($"구글 로그인 성공! Token: {token}");

        var bro = Backend.BMember.AuthorizeFederation(token, FederationType.Google);

        if (bro.IsSuccess())
        {
            Debug.Log("뒤끝 로그인 완료!");
        }
        else
        {
            Debug.LogError($"뒤끝 로그인 실패: {bro}");
        }
    }
}
