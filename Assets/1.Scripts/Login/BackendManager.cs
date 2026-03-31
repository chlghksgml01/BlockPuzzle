using BackEnd;
using UnityEngine;

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
#if UNITY_EDITOR
            Debug.Log($"초기화 성공 : {bro}");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log($"초기화 실패 : {bro}");
#endif
        }
    }
}
