using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class LobbyInitializer : MonoBehaviour
{
    [SerializeField] private ScoreSystem _scoreSystem;

    private void Awake()
    {
        InitializeContext context = new InitializeContext(_scoreSystem);

        var targets = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IInitializable>();

        foreach (IInitializable target in targets)
        {
            target.Initialize(context);
        }
    }
}