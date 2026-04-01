using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-60)]
public class LineParticleSystem : MonoBehaviour, IInitializable
{
    [SerializeField] private ParticleSystem _lineClearEffectRow;
    [SerializeField] private ParticleSystem _lineClearEffectCol;
    [SerializeField] private Transform _effectRoot;
    [SerializeField] private Sprite[] _shardsSprites;
    [SerializeField, Min(0)] private int _prewarmCount = 4;

    private readonly Dictionary<string, Sprite> _shardsSpriteByKey = new Dictionary<string, Sprite>();
    private readonly List<string> _shardsSpriteKeys = new List<string>();
    private string _lastPlacedSpriteKey = string.Empty;
    private LineParticlePoolManager _poolManager;
    private IBoardQuery _boardQuery;
    private IBoardInfo _boardInfo;

    public void Initialize(InitializeContext context)
    {
        _boardQuery = context.BoardManager;
        _boardInfo = context.BoardManager;
    }

    private void Awake()
    {
        BuildShardsSpriteLookup();
        Transform parent = _effectRoot != null ? _effectRoot : transform;
        _poolManager = new LineParticlePoolManager(parent);
    }

    private void OnEnable()
    {
        BlockSlot.OnBlockSpritePlaced += HandleBlockSpritePlaced;
        _boardQuery.OnLinesClearedDetailed += HandleLinesCleared;
        EnsurePrewarm();
    }

    private void OnDisable()
    {
        BlockSlot.OnBlockSpritePlaced -= HandleBlockSpritePlaced;
        _boardQuery.OnLinesClearedDetailed -= HandleLinesCleared;
    }

    private void HandleBlockSpritePlaced(Sprite sprite)
    {
        _lastPlacedSpriteKey = NormalizeName(sprite != null ? sprite.name : string.Empty);
    }

    private void HandleLinesCleared(IReadOnlyList<int> rows, IReadOnlyList<int> cols)
    {
        if (_boardQuery == null)
            return;

        PlayRowEffects(rows);
        PlayColEffects(cols);
    }

    private void PlayRowEffects(IReadOnlyList<int> rows)
    {
        if (rows == null)
            return;

        Sprite shardsSprite = FindShardsSprite(_lastPlacedSpriteKey);
        int width = _boardInfo.Width;
        for (int i = 0; i < rows.Count; i++)
        {
            int y = rows[i];
            if (!_boardQuery.TryGetCellWorldPosition(0, y, out Vector3 left))
                continue;
            if (!_boardQuery.TryGetCellWorldPosition(width - 1, y, out Vector3 right))
                continue;

            Vector3 center = (left + right) * 0.5f;
            SpawnLineEffect(_lineClearEffectRow, center, shardsSprite);
        }
    }

    private void PlayColEffects(IReadOnlyList<int> cols)
    {
        if (cols == null)
            return;

        Sprite shardsSprite = FindShardsSprite(_lastPlacedSpriteKey);
        int topY = 0;
        int bottomY = _boardInfo.Height - 1;
        for (int i = 0; i < cols.Count; i++)
        {
            int x = cols[i];
            if (!_boardQuery.TryGetCellWorldPosition(x, topY, out Vector3 top))
                continue;
            if (!_boardQuery.TryGetCellWorldPosition(x, bottomY, out Vector3 bottom))
                continue;

            Vector3 center = (top + bottom) * 0.5f;
            SpawnLineEffect(_lineClearEffectCol, center, shardsSprite);
        }
    }

    private void SpawnLineEffect(ParticleSystem prefab, Vector3 position, Sprite shardsSprite)
    {
        if (prefab == null)
            return;

        if (_poolManager == null)
            return;

        ParticleSystem ps = _poolManager.Get(prefab, position);
        if (ps == null)
            return;

        ApplyShardsSprite(ps, shardsSprite);
        ps.Play();
    }

    private void EnsurePrewarm()
    {
        if (_poolManager == null)
            return;

        _poolManager.Prewarm(_lineClearEffectRow, _prewarmCount);
        _poolManager.Prewarm(_lineClearEffectCol, _prewarmCount);
    }

    private Sprite FindShardsSprite(string spriteKey)
    {
        if (string.IsNullOrEmpty(spriteKey))
            return null;

        string lookupKey = spriteKey + "shards";

        if (_shardsSpriteByKey.TryGetValue(lookupKey, out Sprite sprite))
        {
            return sprite;
        }

        return null;
    }

    private void BuildShardsSpriteLookup()
    {
        _shardsSpriteByKey.Clear();
        _shardsSpriteKeys.Clear();

        if (_shardsSprites == null || _shardsSprites.Length == 0)
            return;

        for (int i = 0; i < _shardsSprites.Length; i++)
        {
            Sprite sprite = _shardsSprites[i];
            if (sprite == null)
                continue;

            string key = NormalizeName(sprite.name);
            if (string.IsNullOrEmpty(key))
                continue;

            if (_shardsSpriteByKey.ContainsKey(key))
                continue;

            _shardsSpriteByKey[key] = sprite;
            _shardsSpriteKeys.Add(key);
        }
    }

    private static void ApplyShardsSprite(ParticleSystem ps, Sprite sprite)
    {
        if (ps == null || sprite == null)
            return;

        var tsa = ps.textureSheetAnimation;
        if (!tsa.enabled)
            return;

        while (tsa.spriteCount > 0)
            tsa.RemoveSprite(0);

        tsa.AddSprite(sprite);
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.ToLowerInvariant().Trim();
    }
}