using System.Collections;
using UnityEngine;

public class BoardEffect : MonoBehaviour
{
    [Header("Intro Domino Effect")]
    [SerializeField] private bool _playIntroDominoEffect = true;
    [SerializeField] private Sprite _introEffectSprite;
    [SerializeField, Min(0f)] private float _introLineInterval = 0.04f;
    [SerializeField, Min(0f)] private float _introLineHold = 0.08f;

    [Header("GrayScale Effect")]
    [SerializeField] private Material _cellMaterial;

    public void ActivateGrayscale(bool useGrayScale, float effectDuration = 0f)
    {
#if UNITY_EDITOR
        Debug.Log("Use GrayScale " + useGrayScale);
#endif

        if (useGrayScale)
        {
            _cellMaterial.SetFloat("_UseGrayscale", 1f);
            _cellMaterial.SetFloat("_EffectStartTime", Time.time);
            _cellMaterial.SetFloat("_EffectDuration", effectDuration);
        }
        else
        {
            _cellMaterial.SetFloat("_UseGrayscale", 0f);
            _cellMaterial.SetFloat("_EffectStartTime", 0f);
            _cellMaterial.SetFloat("_EffectDuration", 0f);
        }
    }

    public void PlayIntro(BoardCell[,] cells, int width, int height)
    {
        if (_playIntroDominoEffect)
            StartCoroutine(PlayIntroDominoEffect(cells, width, height));
    }

    private IEnumerator PlayIntroDominoEffect(BoardCell[,] cells, int width, int height)
    {
        SoundManager.Instance.PlaySFX(SFXType.Intro);
        if (_introEffectSprite == null || cells == null)
            yield break;

        int maxSum = (width - 1) + (height - 1);

        for (int sum = 0; sum <= maxSum; sum++)
        {
            for (int x = 0; x < width; x++)
            {
                int y = sum - x;
                if (y < 0 || y >= height)
                    continue;

                cells[x, y].SetLinePreview(true, _introEffectSprite);
            }

            if (_introLineInterval > 0f)
                yield return new WaitForSeconds(_introLineInterval);
        }

        if (_introLineHold > 0f)
            yield return new WaitForSeconds(_introLineHold);

        for (int sum = 0; sum <= maxSum; sum++)
        {
            for (int x = 0; x < width; x++)
            {
                int y = sum - x;
                if (y < 0 || y >= height)
                    continue;

                cells[x, y].SetLinePreview(false, null, true);
            }

            if (_introLineInterval > 0f)
                yield return new WaitForSeconds(_introLineInterval);
        }

        InGameManager.Instance.EnableInteraction(true);
    }
}
