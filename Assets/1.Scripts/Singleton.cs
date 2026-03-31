using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    // 꺼지는 중인지 체크
    // 종료 중에 Instance에 접근해서 새로 생성되는 걸 막기 위한 용도
    private static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            // 끝나는 중이면 null 반환
            if (_applicationIsQuitting)
            {
                return null;
            }

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();

                // 없으면 새로 만들기
                if (_instance == null)
                {
                    GameObject singletonObj = new GameObject(typeof(T).Name);
                    _instance = singletonObj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    public static bool HasInstance => _instance != null && !_applicationIsQuitting;

    [SerializeField]
    private bool _dontDestroyOnLoad = true;

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;

            if (_dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        OnAwake();
    }

    protected virtual void OnAwake() { }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
}
