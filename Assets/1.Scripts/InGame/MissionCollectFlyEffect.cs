using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 미션 블록이 HUD 아이콘으로 날아가는 연출.
/// 연출이 끝난 뒤에만 onComplete가 호출된다.
/// </summary>
public class MissionCollectFlyEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip("비행 아이콘이 붙을 Canvas (보통 LevelInGame Canvas)")]
    [SerializeField] private RectTransform _flyRoot;

    [Tooltip("비행에 사용할 Image 프리팹 (IconImage 등)")]
    [SerializeField] private GameObject _flyIconPrefab;

    [Header("Fly Speed")]
    [Tooltip("출발 → HUD 아이콘까지 비행 시간(초). 작을수록 빠름.")]
    [SerializeField, Range(0.05f, 1.5f)]
    [FormerlySerializedAs("_duration")]
    private float _flyDuration = 0.45f;

    [Header("Arc")]
    [Tooltip("포물선 꼭지점 높이 (월드 Y 오프셋)")]
    [SerializeField, Range(0f, 4f)]
    private float _arcHeight = 1.2f;

    [Header("Spin")]
    [Tooltip("비행 중 Z축 회전 횟수. 0이면 회전 없음.")]
    [SerializeField, Range(0f, 3f)]
    private float _spinTurns = 0.75f;

    [Header("Scale")]
    [Tooltip("도착 시 스케일")]
    [SerializeField, Min(0.01f)]
    private float _endScale = 0.35f;

    private readonly List<GameObject> _activeFlies = new List<GameObject>();

    /// <summary>from → to 로 스프라이트를 날린 뒤 onComplete 호출.</summary>
    public void Play(Sprite sprite, Vector3 fromWorld, Vector3 toWorld, Action onComplete)
    {
        if (_flyRoot == null || _flyIconPrefab == null || sprite == null)
        {
            onComplete?.Invoke();
            return;
        }

        GameObject fly = Instantiate(_flyIconPrefab, _flyRoot);
        _activeFlies.Add(fly);

        Image image = fly.GetComponent<Image>();
        if (image == null)
            image = fly.GetComponentInChildren<Image>();

        if (image != null)
        {
            image.sprite = sprite;
            image.SetNativeSize();
            image.raycastTarget = false;
        }

        Transform flyTransform = fly.transform;
        flyTransform.position = fromWorld;
        flyTransform.localScale = Vector3.one;
        flyTransform.rotation = Quaternion.identity;

        Vector3 mid = (fromWorld + toWorld) * 0.5f;
        mid.y += _arcHeight;

        Sequence sequence = DOTween.Sequence().SetLink(fly, LinkBehaviour.KillOnDestroy);
        sequence.Append(flyTransform.DOPath(
            new[] { fromWorld, mid, toWorld },
            _flyDuration,
            PathType.CatmullRom).SetEase(Ease.InOutQuad));
        sequence.Join(flyTransform.DOScale(_endScale, _flyDuration).SetEase(Ease.InQuad));

        if (_spinTurns > 0f)
        {
            float spinDegrees = _spinTurns * 360f;
            sequence.Join(flyTransform.DORotate(
                new Vector3(0f, 0f, spinDegrees),
                _flyDuration,
                RotateMode.FastBeyond360).SetEase(Ease.Linear));
        }

        sequence.OnComplete(() =>
        {
            _activeFlies.Remove(fly);
            if (fly != null)
                Destroy(fly);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 진행 중인 비행 연출을 즉시 중단한다.
    /// onComplete는 호출하지 않아 Retry 시 이전 수집이 반영되지 않는다.
    /// </summary>
    public void CancelAll()
    {
        for (int i = 0; i < _activeFlies.Count; i++)
        {
            if (_activeFlies[i] != null)
            {
                _activeFlies[i].transform.DOKill();
                Destroy(_activeFlies[i]);
            }
        }

        _activeFlies.Clear();
    }

    private void OnDisable()
    {
        CancelAll();
    }
}
