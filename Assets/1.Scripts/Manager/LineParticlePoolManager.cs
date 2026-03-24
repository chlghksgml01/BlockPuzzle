using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public sealed class LineParticlePoolManager
{
    private readonly Transform _parent;
    private readonly Dictionary<ParticleSystem, ObjectPool<ParticleSystem>> _poolByPrefab = new Dictionary<ParticleSystem, ObjectPool<ParticleSystem>>();
    private readonly Dictionary<ParticleSystem, ParticleSystem> _prefabByInstance = new Dictionary<ParticleSystem, ParticleSystem>();

    public LineParticlePoolManager(Transform parent)
    {
        _parent = parent;
    }

    public void Prewarm(ParticleSystem prefab, int count)
    {
        if (prefab == null || count <= 0)
            return;

        ObjectPool<ParticleSystem> pool = GetOrCreatePool(prefab);
        List<ParticleSystem> temp = new List<ParticleSystem>(count);
        for (int i = 0; i < count; i++)
        {
            ParticleSystem ps = pool.Get();
            temp.Add(ps);
        }

        for (int i = 0; i < temp.Count; i++)
            pool.Release(temp[i]);
    }

    public ParticleSystem Get(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null)
            return null;

        ObjectPool<ParticleSystem> pool = GetOrCreatePool(prefab);
        ParticleSystem ps = pool.Get();
        ps.transform.position = new Vector3(position.x, position.y, 0f);
        return ps;
    }

    internal void Release(ParticleSystem instance)
    {
        if (instance == null)
            return;

        if (!_prefabByInstance.TryGetValue(instance, out ParticleSystem prefab))
            return;

        if (!_poolByPrefab.TryGetValue(prefab, out ObjectPool<ParticleSystem> pool))
            return;

        pool.Release(instance);
    }

    private ObjectPool<ParticleSystem> GetOrCreatePool(ParticleSystem prefab)
    {
        if (_poolByPrefab.TryGetValue(prefab, out ObjectPool<ParticleSystem> cached))
            return cached;

        ObjectPool<ParticleSystem> pool = new ObjectPool<ParticleSystem>(
            createFunc: () => CreateInstance(prefab),
            actionOnGet: OnGetInstance,
            actionOnRelease: OnReleaseInstance,
            actionOnDestroy: OnDestroyInstance,
            collectionCheck: false,
            defaultCapacity: 4,
            maxSize: 64);

        _poolByPrefab[prefab] = pool;
        return pool;
    }

    private ParticleSystem CreateInstance(ParticleSystem prefab)
    {
        ParticleSystem ps = Object.Instantiate(prefab, _parent);
        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.Callback;

        LineParticlePoolItem item = ps.GetComponent<LineParticlePoolItem>();
        if (item == null)
            item = ps.gameObject.AddComponent<LineParticlePoolItem>();

        item.Initialize(this);
        _prefabByInstance[ps] = prefab;
        return ps;
    }

    private static void OnGetInstance(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.gameObject.SetActive(true);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Clear(true);
    }

    private static void OnReleaseInstance(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Clear(true);
        ps.gameObject.SetActive(false);
    }

    private void OnDestroyInstance(ParticleSystem ps)
    {
        if (ps != null)
            _prefabByInstance.Remove(ps);

        if (ps != null)
            Object.Destroy(ps.gameObject);
    }
}

public sealed class LineParticlePoolItem : MonoBehaviour
{
    private LineParticlePoolManager _owner;
    private ParticleSystem _particleSystem;

    public void Initialize(LineParticlePoolManager owner)
    {
        _owner = owner;
        if (_particleSystem == null)
            _particleSystem = GetComponent<ParticleSystem>();
    }

    private void OnParticleSystemStopped()
    {
        if (_owner != null && _particleSystem != null)
            _owner.Release(_particleSystem);
    }
}

