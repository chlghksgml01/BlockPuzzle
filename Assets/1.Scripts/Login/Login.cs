using BackEnd;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Login : LoginBase
{
    [SerializeField]
    private Image _imageID;
    [SerializeField]
    private TMP_InputField _inputFieldID;

    [SerializeField]
    private Image _imagePW;
    [SerializeField]
    private TMP_InputField _inputFieldPW;

    [SerializeField]
    private Button _loginBtn;

    public void ResetUI()
    {
        ResetUI(_imageID, _imagePW);
    }

    public void OnClickLogin()
    {
        ResetUI(_imageID, _imagePW);

        if (IsFieldDataEmpty(_imageID, _inputFieldID.text, "ID"))
            return;
        if (IsFieldDataEmpty(_imagePW, _inputFieldPW.text, "ID"))
            return;

        // 로그인 버튼 연타 못하게
        _loginBtn.interactable = false;
        StartCoroutine(nameof(LoginProcess));

        ResponseToLogin(_inputFieldID.text, _inputFieldPW.text);
    }

    private void ResponseToLogin(string ID, string PW)
    {
        // 뒤끝 서버에 로그인 요청 후 요청 끝나면 뒤끝이 callback 보내줌
        Backend.BMember.CustomLogin(ID, PW, callback =>
        {
            StopCoroutine(nameof(LoginProcess));

            if (callback.IsSuccess())
                SetMessage($"Welcome, {_inputFieldID.text}!");

            else
            {
                _loginBtn.interactable = true;

                string message = string.Empty;
                switch (int.Parse(callback.GetStatusCode()))
                {
                    case 401:
                        message = callback.GetMessage().Contains("customId") ? "No account found" : "Incorrect password";
                        break;
                    case 410:
                        message = "This account is being deleted";
                        break;
                    default:
                        message = callback.GetMessage(); // 뒤끝 서버에서 제공한 기본 메세지
                        break;
                }

                if (message.Contains("password"))
                    GuideForIncorrectyEnteredData(_imagePW, message);
                else
                    GuideForIncorrectyEnteredData(_imageID, message);
            }
        });
    }

    private IEnumerator LoginProcess()
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime;
            SetMessage($"Logging in...{time:F1}");
            yield return null;
        }
    }
}
