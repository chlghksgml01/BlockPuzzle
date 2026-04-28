using BackEnd;
using UnityEngine;

[DefaultExecutionOrder(-999)]
public class BackendManager : Singleton<BackendManager>
{
    protected override void OnAwake()
    {
        BackendSetup();
    }

    private void BackendSetup()
    {
        BackendReturnObject bro = Backend.Initialize();

        if (bro.IsSuccess())
        {
            Debug.Log($"초기화 성공 : {bro}");
        }
        else
        {
            Debug.Log($"초기화 실패 : {bro}");
        }
    }
}
