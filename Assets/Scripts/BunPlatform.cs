using System.Collections;
using UnityEngine;

public class BunPlatform : MonoBehaviour
{
    public float bounceForce = 18f;
    public float squishDuration     = 0.10f;
    [Range(0.1f, 0.95f)]
    public float squishScaleY       = 0.48f;
    public float squishScaleXZ      = 1.32f;

    public float springDuration     = 0.55f;
    public float stretchOvershoot   = 1.18f;

    public AnimationCurve springCurve = new AnimationCurve(
        new Keyframe(0.00f, 0.00f, 0f,   8f),
        new Keyframe(0.35f, 1.10f, 4f,   0f),
        new Keyframe(0.60f, 0.94f, 0f,   0f),
        new Keyframe(0.80f, 1.02f, 0f,   0f),
        new Keyframe(1.00f, 1.00f, 0f,   0f)
    );

    public AudioSource audioSource;
    public AudioClip   bounceClip;

    Vector3 _restScale;
    bool    _isBouncing;

    void Start()
    {
        _restScale = transform.localScale;
    }

    public void TriggerBounce(ChickController chick)
    {
        if (_isBouncing) return;
        StartCoroutine(BounceRoutine(chick));
    }

    IEnumerator BounceRoutine(ChickController chick)
    {
        _isBouncing = true;

        PlaySound();

        float elapsed = 0f;
        while (elapsed < squishDuration)
        {
            elapsed    += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / squishDuration);
            float tSmooth = Mathf.SmoothStep(0f, 1f, t); 

            SetScale(
                Mathf.Lerp(1f,           squishScaleXZ, tSmooth),
                Mathf.Lerp(1f,           squishScaleY,  tSmooth)
            );
            yield return null;
        }

        SetScale(squishScaleXZ, squishScaleY);

        if (chick != null)
            chick.ApplyBounce(bounceForce);

        elapsed = 0f;
        while (elapsed < springDuration)
        {
            elapsed    += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / springDuration);
            float curveY = springCurve.Evaluate(t);

            float scaleY  = curveY < 1f
                ? Mathf.Lerp(squishScaleY, 1f,                curveY)
                : Mathf.Lerp(1f,           stretchOvershoot,  curveY - 1f);

            float scaleXZ = curveY < 1f
                ? Mathf.Lerp(squishScaleXZ, 1f,          curveY)
                : Mathf.Lerp(1f,            1f / Mathf.Max(stretchOvershoot - 0.08f, 0.9f), curveY - 1f);

            SetScale(scaleXZ, scaleY);
            yield return null;
        }

        transform.localScale = _restScale;
        _isBouncing          = false;
    }

    void SetScale(float xzMultiplier, float yMultiplier)
    {
        transform.localScale = new Vector3(
            _restScale.x * xzMultiplier,
            _restScale.y * yMultiplier,
            _restScale.z * xzMultiplier
        );
    }

    [Range(0f, 0.5f)]
    public float pitchVariation = 0.08f;

    void PlaySound()
    {
        if (audioSource && bounceClip)
        {
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.PlayOneShot(bounceClip);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.4f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
#endif
}
