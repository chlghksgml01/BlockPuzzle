using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프리팹에 붙는 세그먼트. ParticleSystem은 프리팹 Inspector에서 조절하고,
/// 런타임에는 위치/박스 크기만 맞춘다.
/// 스파클 스프라이트는 Texture Sheet Animation으로 파티클마다 랜덤 선택한다.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public sealed class LineClearSparkleSegment : MonoBehaviour
{
    [Header("Spark Sprites")]
    [Tooltip("파티클마다 랜덤으로 골라 쓸 스파클 스프라이트 (서로 다른 텍스처여도 자동 아틀라스)")]
    [SerializeField] private Sprite[] _sparkSprites;

    private ParticleSystem _particleSystem;
    private ParticleSystemRenderer _particleRenderer;
    private Canvas _canvas;
    private float _padding = 0.08f;
    private bool _sparkSheetConfigured;
    private Texture2D _runtimeAtlas;
    private Material _runtimeMaterial;
    private readonly List<Sprite> _runtimeSprites = new List<Sprite>();

    private void Awake()
    {
        CacheParticle();
        ConfigureSparkSheet();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _runtimeSprites.Count; i++)
        {
            if (_runtimeSprites[i] != null)
                Destroy(_runtimeSprites[i]);
        }

        _runtimeSprites.Clear();

        if (_runtimeAtlas != null)
            Destroy(_runtimeAtlas);

        if (_runtimeMaterial != null)
            Destroy(_runtimeMaterial);
    }

    public void Bind(Transform worldEffectRoot, Canvas canvas, float padding)
    {
        _canvas = canvas;
        _padding = Mathf.Max(0f, padding);
        CacheParticle();
        ConfigureSparkSheet();

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
        ConfigureSparkSheet();
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

    private void ConfigureSparkSheet()
    {
        if (_sparkSheetConfigured || _particleSystem == null)
            return;

        if (_sparkSprites == null || _sparkSprites.Length == 0)
            return;

        List<Sprite> valid = new List<Sprite>(_sparkSprites.Length);
        for (int i = 0; i < _sparkSprites.Length; i++)
        {
            if (_sparkSprites[i] != null)
                valid.Add(_sparkSprites[i]);
        }

        if (valid.Count == 0)
            return;

        Sprite[] sheetSprites = BuildSharedTextureSprites(valid);
        if (sheetSprites == null || sheetSprites.Length == 0)
            return;

        ParticleSystem.TextureSheetAnimationModule sheet = _particleSystem.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Sprites;
        while (sheet.spriteCount > 0)
            sheet.RemoveSprite(0);

        for (int i = 0; i < sheetSprites.Length; i++)
            sheet.AddSprite(sheetSprites[i]);

        // 파티클마다 랜덤 스프라이트 1장, 수명 동안 고정
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
        sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, Mathf.Max(0f, sheetSprites.Length - 0.001f));
        sheet.cycleCount = 1;

        ApplyAtlasToRenderer();
        _sparkSheetConfigured = true;
    }

    private Sprite[] BuildSharedTextureSprites(List<Sprite> sources)
    {
        Texture2D sharedTexture = null;
        bool allSameTexture = true;

        for (int i = 0; i < sources.Count; i++)
        {
            Texture2D texture = sources[i].texture;
            if (texture == null)
                continue;

            if (sharedTexture == null)
                sharedTexture = texture;
            else if (sharedTexture != texture)
            {
                allSameTexture = false;
                break;
            }
        }

        if (allSameTexture)
            return sources.ToArray();

        return PackIntoAtlas(sources);
    }

    private Sprite[] PackIntoAtlas(List<Sprite> sources)
    {
        int count = sources.Count;
        int cellW = 1;
        int cellH = 1;

        for (int i = 0; i < count; i++)
        {
            Rect textureRect = sources[i].textureRect;
            cellW = Mathf.Max(cellW, Mathf.CeilToInt(textureRect.width));
            cellH = Mathf.Max(cellH, Mathf.CeilToInt(textureRect.height));
        }

        int atlasW = Mathf.Max(1, cellW * count);
        int atlasH = Mathf.Max(1, cellH);

        if (_runtimeAtlas != null)
            Destroy(_runtimeAtlas);

        _runtimeAtlas = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, false);
        _runtimeAtlas.name = "LineClearSparkleRuntimeAtlas";
        _runtimeAtlas.filterMode = FilterMode.Bilinear;
        _runtimeAtlas.wrapMode = TextureWrapMode.Clamp;

        Color32[] clear = new Color32[atlasW * atlasH];
        _runtimeAtlas.SetPixels32(clear);
        _runtimeAtlas.Apply(false, false);

        Sprite[] result = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            BlitSpriteIntoAtlas(sources[i], i * cellW, 0, cellW, cellH);

            float pixelsPerUnit = sources[i].pixelsPerUnit > 0f ? sources[i].pixelsPerUnit : 100f;
            Sprite packed = Sprite.Create(
                _runtimeAtlas,
                new Rect(i * cellW, 0f, cellW, cellH),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            packed.name = sources[i].name;
            _runtimeSprites.Add(packed);
            result[i] = packed;
        }

        return result;
    }

    private void BlitSpriteIntoAtlas(Sprite sprite, int destX, int destY, int cellW, int cellH)
    {
        Texture2D sourceTexture = sprite.texture;
        if (sourceTexture == null || _runtimeAtlas == null)
            return;

        Rect textureRect = sprite.textureRect;
        float scaleX = textureRect.width / sourceTexture.width;
        float scaleY = textureRect.height / sourceTexture.height;
        float offsetX = textureRect.x / sourceTexture.width;
        float offsetY = textureRect.y / sourceTexture.height;

        RenderTexture temporary = RenderTexture.GetTemporary(
            cellW,
            cellH,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;

        Graphics.SetRenderTarget(temporary);
        GL.Clear(true, true, Color.clear);
        Graphics.Blit(sourceTexture, temporary, new Vector2(scaleX, scaleY), new Vector2(offsetX, offsetY));

        RenderTexture.active = temporary;
        _runtimeAtlas.ReadPixels(new Rect(0f, 0f, cellW, cellH), destX, destY, false);
        _runtimeAtlas.Apply(false, false);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);
    }

    private void ApplyAtlasToRenderer()
    {
        if (_particleRenderer == null)
            return;

        Texture atlasTexture = _runtimeAtlas;
        if (atlasTexture == null && _sparkSprites != null && _sparkSprites.Length > 0 && _sparkSprites[0] != null)
            atlasTexture = _sparkSprites[0].texture;

        if (atlasTexture == null)
            return;

        Material sourceMaterial = _particleRenderer.sharedMaterial;
        if (sourceMaterial == null)
            return;

        if (_runtimeMaterial != null)
            Destroy(_runtimeMaterial);

        _runtimeMaterial = new Material(sourceMaterial);
        _runtimeMaterial.name = sourceMaterial.name + " (SparkRuntime)";
        _runtimeMaterial.mainTexture = atlasTexture;

        if (_runtimeMaterial.HasProperty("_BaseMap"))
            _runtimeMaterial.SetTexture("_BaseMap", atlasTexture);

        if (_runtimeMaterial.HasProperty("_MainTex"))
            _runtimeMaterial.SetTexture("_MainTex", atlasTexture);

        _particleRenderer.material = _runtimeMaterial;
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
