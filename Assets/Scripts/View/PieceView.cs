using System.Collections;
using UnityEngine;

public class PieceView : MonoBehaviour
{
    SpriteRenderer _renderer;
    bool _isBlack;
    Coroutine _flipCoroutine;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
            _renderer = gameObject.AddComponent<SpriteRenderer>();
    }

    public void SetColor(bool isBlack, bool immediate = false)
    {
        if (immediate)
        {
            _isBlack = isBlack;
            ApplyColor();
            return;
        }

        if (_flipCoroutine != null) StopCoroutine(_flipCoroutine);
        _flipCoroutine = StartCoroutine(FlipCoroutine(isBlack));
    }

    void ApplyColor()
    {
        _renderer.color = _isBlack ? Color.black : Color.white;
    }

    IEnumerator FlipCoroutine(bool toBlack)
    {
        float duration = 0.12f;

        // Phase 1: scale X 1 → 0
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0f, t / duration);
            transform.localScale = new Vector3(scale, 1f, 1f);
            yield return null;
        }

        // Swap color while invisible
        _isBlack = toBlack;
        ApplyColor();

        // Phase 2: scale X 0 → 1
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1f, t / duration);
            transform.localScale = new Vector3(scale, 1f, 1f);
            yield return null;
        }

        transform.localScale = Vector3.one;
        _flipCoroutine = null;
    }
}
