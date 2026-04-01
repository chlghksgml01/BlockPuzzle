using System.Collections.Generic;
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

    public InitializeContext(ScoreSystem score)
    {
        if (score == null)
            Debug.LogError("ScoreSystem is null");

        ScoreSystem = score;
        BoardManager = null;
    }

    public InitializeContext(ScoreSystem score, BoardManager board)
    {
        if (score == null)
            Debug.LogError("ScoreSystem is null");
        if (board == null)
            Debug.LogError("BoardManager is null");

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
        if (_scoreSystem == null || _boardManager == null)
        {
            Debug.LogError("InGameInitializer - Missing required references: ScoreSystem, BoardManager");
            enabled = false;
            return;
        }

        _scoreSystem.ResetRuntimeState();
        InitializeContext context = new InitializeContext(_scoreSystem, _boardManager);

        IEnumerable<IInitializable> targets = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IInitializable>();

        foreach (IInitializable target in targets)
        {
            target.Initialize(context);
        }
    }
}