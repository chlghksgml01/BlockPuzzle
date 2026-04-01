using System.Linq;
using UnityEngine;

public interface IInitializable
{
    void OnInitialize(InitializeContext context);
}

public class InitializeContext
{
    public readonly ScoreSystem ScoreSystem;

    public InitializeContext(ScoreSystem score)
    {
        ScoreSystem = score;
    }
}

[DefaultExecutionOrder(-200)]
public class InGameInitializer : MonoBehaviour
{
    [SerializeField] private ScoreSystem _scoreSystem;

    private void Awake()
    {
        InitializeContext context = new InitializeContext(_scoreSystem);

        var targets = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IInitializable>();

        foreach (IInitializable target in targets)
        {
            target.OnInitialize(context);
        }
    }
}