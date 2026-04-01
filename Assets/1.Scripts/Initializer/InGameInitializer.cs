using System.Linq;
using UnityEngine;

public interface IInitializable
{
    void Initialize(InitializeContext context);
}

public class InitializeContext
{
    public readonly ScoreSystem ScoreSystem;
    public readonly BoardManager BoardManager;

    public InitializeContext(ScoreSystem score, BoardManager board = null)
    {
        ScoreSystem = score;
        BoardManager = board;
    }
}

[DefaultExecutionOrder(-200)]
public class InGameInitializer : MonoBehaviour
{
    [SerializeField] private ScoreSystem _scoreSystem;
    [SerializeField] private BoardManager _boardManager;

    private void Awake()
    {
        InitializeContext context = new InitializeContext(_scoreSystem, _boardManager);

        var targets = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IInitializable>();

        foreach (IInitializable target in targets)
        {
            target.Initialize(context);
        }
    }
}