using UnityEngine;

/// <summary>
/// 프리팹에 붙는 세그먼트. ParticleSystem은 프리팹 Inspector에서 조절하고,
/// 런타임에는 위치/박스 크기만 맞춘다.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public sealed class LineClearSparkleSegment : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    private ParticleSystemRenderer _particleRenderer;
    private Canvas _canvas;
    private float _padding = 0.08f;

    private void Awake()
    {
        CacheParticle();
    }

    public void Bind(Transform worldEffectRoot, Canvas canvas, float padding)
    {
        _canvas = canvas;
        _padding = Mathf.Max(0f, padding);
        CacheParticle();

        if (worldEffectRoot != null && transform.parent != worldEffectRoot)
            transform.SetParent(worldEffectRoot, false);

        if (worldEffectRoot != null)
            gameObject.layer = worldEffectRoot.gameObject.layer;

        if (_particleSystem != null)
        {
            ParticleSystem.MainModule main = _particleSystem.main;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
        }

        Hide();
    }

    public void Show(Vector3 worldMin, Vector3 worldMax)
    {
        CacheParticle();
        if (_particleSystem == null)
            return;

        Vector3 paddedMin = worldMin - new Vector3(_padding, _padding, 0f);
        Vector3 paddedMax = worldMax + new Vector3(_padding, _padding, 0f);
        paddedMin.z = worldMin.z;
        paddedMax.z = worldMax.z;

        Vector3 center = (paddedMin + paddedMax) * 0.5f;
        Vector3 size = paddedMax - paddedMin;
        size.x = Mathf.Max(0.01f, Mathf.Abs(size.x));
        size.y = Mathf.Max(0.01f, Mathf.Abs(size.y));
        size.z = 0.1f;

        gameObject.SetActive(true);
        transform.position = center;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        ParticleSystem.ShapeModule shape = _particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.BoxEdge;
        shape.scale = size;

        ApplyCanvasSorting();

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _particleSystem.Clear(true);
        _particleSystem.Play(true);
    }

    public void Hide()
    {
        CacheParticle();
        if (_particleSystem != null)
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particleSystem.Clear(true);
        }

        gameObject.SetActive(false);
    }

    private void CacheParticle()
    {
        if (_particleSystem == null)
            _particleSystem = GetComponent<ParticleSystem>();

        if (_particleRenderer == null && _particleSystem != null)
            _particleRenderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
    }

    private void ApplyCanvasSorting()
    {
        if (_particleRenderer == null)
            return;

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        if (_canvas != null)
        {
            _particleRenderer.sortingLayerID = _canvas.sortingLayerID;
            _particleRenderer.sortingOrder = _canvas.sortingOrder + 50;
        }
        else
        {
            _particleRenderer.sortingOrder = 50;
        }
    }
}
