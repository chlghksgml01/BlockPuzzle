using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _applicationIsQuitting = false;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning(typeof(T).Name + " 이미 종료 중");
                return null;
            }

            if (_instance == null)
            {
                // 다중 스레드 환경에서 안전하게 싱글톤 인스턴스 생성
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>();

                        if (_instance == null)
                        {
                            GameObject singletonObj = new GameObject(typeof(T).Name);
                            _instance = singletonObj.AddComponent<T>();
                        }
                    }
                }
            }

            return _instance;
        }
    }

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
            Debug.LogWarning(typeof(T).Name + " 중복 생성");
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
