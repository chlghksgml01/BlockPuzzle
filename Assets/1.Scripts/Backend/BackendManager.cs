using UnityEngine;
using BackEnd;

public class BackendManager : Singleton<BackendManager>
{
    protected override void OnAwake()
    {
        BackendSetup();
    }

    private void Update()
    {

    }

    private void BackendSetup()
    {
        // 뒤끝 초기화
        BackendReturnObject bro = Backend.Initialize();

        // 뒤끝 초기화에 대한 응답값
        if (bro.IsSuccess())
        {
            // 초기화 성공시 statusCode 204 Succees
            Debug.Log($"초기화 성공 : {bro}");
        }
        else
        {
            // 초기화 실패시 statusCode 400대 에러 발생
            Debug.Log($"초기화 실패 : {bro}");
        }
    }
}
