using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OilSink : MonoBehaviour
{

    public Transform body;
    public Transform rightLeg;
    public Transform leftLeg;
    public Transform rightWing;
    public Transform leftWing;
    public Transform head;

    public string nextSceneName;

    public float panicDuration      = 3.8f;
    public float exhaustionDuration = 2.6f;
    public float submergeDuration   = 2.0f;

    public float panicSinkSpeed      = 0.12f;
    public float exhaustionSinkSpeed = 0.28f;
    public float submergeSinkSpeed   = 0.65f;

    public float panicWingSpeed      = 6.0f;
    public float panicWingAngle      = 72f;
    public float panicLegSpeed       = 8.5f;
    public float panicLegAngle       = 55f;

    [Header("── The Reach (T2 moment) ───────────")]
    [Range(0f, 1f)]
    public float reachStartPhase     = 0.45f;
    public float reachWingAngle      = 105f;
    public float reachSpeed          = 2.5f;

    public CanvasGroup fadeOverlay;
    public float fadeDuration        = 1.4f;
    public float postFadeDelay       = 0.3f;

    public AudioSource audioSource;
    public AudioClip screamSFX;
    public AudioClip splashSFX;
    public AudioClip finalGulpSFX;

    bool _triggered;

    Quaternion _rightLegRest, _leftLegRest;
    Quaternion _rightWingRest, _leftWingRest;
    Quaternion _headRest;
    Quaternion _bodyRestRot;
    Vector3    _bodyRestPos, _bodyRestScale;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        ChickController hit = other.GetComponentInParent<ChickController>();
        if (hit == null) return;

        _triggered = true;
        StartCoroutine(SinkSequence(hit));
    }

    IEnumerator SinkSequence(ChickController victim)
    {
        SnapshotRestPoses();

        CharacterController cc = victim.GetComponent<CharacterController>();
        victim.enabled = false;
        if (cc) cc.enabled = false;

        PlaySound(splashSFX);
        yield return new WaitForSeconds(0.08f);
        PlaySound(screamSFX);

        float t = 0f;
        while (t < panicDuration)
        {
            t += Time.deltaTime;

            victim.transform.position += Vector3.down * panicSinkSpeed * Time.deltaTime;

            if (rightWing)
            {
                float rStroke = Mathf.Sin(t * panicWingSpeed) * panicWingAngle;
                rightWing.localRotation = Quaternion.Slerp(
                    rightWing.localRotation,
                    _rightWingRest * Quaternion.Euler(rStroke * 0.25f, 0f, -rStroke),
                    Time.deltaTime * 14f);
            }
            if (leftWing)
            {
                float lStroke = Mathf.Sin(t * panicWingSpeed + Mathf.PI) * panicWingAngle;
                leftWing.localRotation = Quaternion.Slerp(
                    leftWing.localRotation,
                    _leftWingRest * Quaternion.Euler(lStroke * 0.25f, 0f, lStroke),
                    Time.deltaTime * 14f);
            }

            if (rightLeg)
            {
                float rKick = Mathf.Sin(t * panicLegSpeed) * panicLegAngle;
                rightLeg.localRotation = Quaternion.Slerp(
                    rightLeg.localRotation,
                    _rightLegRest * Quaternion.Euler(rKick, 0f, Mathf.Sin(t * 4.3f) * 18f),
                    Time.deltaTime * 16f);
            }
            if (leftLeg)
            {
                float lKick = Mathf.Sin(t * panicLegSpeed + 1.7f) * panicLegAngle;
                leftLeg.localRotation = Quaternion.Slerp(
                    leftLeg.localRotation,
                    _leftLegRest * Quaternion.Euler(lKick, 0f, Mathf.Sin(t * 5.1f) * 18f),
                    Time.deltaTime * 16f);
            }

            if (head)
            {
                float shake = Mathf.Sin(t * 9.5f) * 14f;
                head.localRotation = Quaternion.Slerp(
                    head.localRotation,
                    _headRest * Quaternion.Euler(-40f, shake, 0f),
                    Time.deltaTime * 10f);
            }

            if (body)
            {
                float wobble = Mathf.Sin(t * 4.7f) * 7f;
                body.localRotation = Quaternion.Slerp(
                    body.localRotation,
                    _bodyRestRot * Quaternion.Euler(-8f + wobble * 0.4f, 0f, wobble),
                    Time.deltaTime * 7f);
            }

            yield return null;
        }

        t = 0f;
        while (t < exhaustionDuration)
        {
            t += Time.deltaTime;
            float phase   = t / exhaustionDuration;
            float energy  = 1f - phase;

            victim.transform.position += Vector3.down * exhaustionSinkSpeed * Time.deltaTime;

            if (rightWing)
            {
                if (phase < reachStartPhase)
                {
                    float rStroke = Mathf.Sin(t * panicWingSpeed * 0.45f) * panicWingAngle * energy;
                    rightWing.localRotation = Quaternion.Slerp(
                        rightWing.localRotation,
                        _rightWingRest * Quaternion.Euler(rStroke * 0.25f, 0f, -rStroke),
                        Time.deltaTime * 7f);
                }
                else
                {
                    Quaternion reachTarget = _rightWingRest * Quaternion.Euler(0f, -15f, -reachWingAngle);
                    rightWing.localRotation = Quaternion.Slerp(
                        rightWing.localRotation,
                        reachTarget,
                        Time.deltaTime * reachSpeed);
                }
            }

            if (leftWing)
            {
                float lStroke = (phase < 0.6f)
                    ? Mathf.Sin(t * panicWingSpeed * 0.45f + Mathf.PI) * panicWingAngle * energy
                    : 0f;
                Quaternion lTarget = (phase < 0.6f)
                    ? _leftWingRest * Quaternion.Euler(lStroke * 0.25f, 0f, lStroke)
                    : _leftWingRest * Quaternion.Euler(18f, 0f, 32f);
                leftWing.localRotation = Quaternion.Slerp(
                    leftWing.localRotation, lTarget,
                    Time.deltaTime * (phase < 0.6f ? 7f : 3.5f));
            }

            if (rightLeg)
            {
                float rKick = Mathf.Sin(t * panicLegSpeed * 0.4f) * panicLegAngle * 0.5f * energy;
                rightLeg.localRotation = Quaternion.Slerp(
                    rightLeg.localRotation,
                    _rightLegRest * Quaternion.Euler(rKick, 0f, 0f),
                    Time.deltaTime * 6f);
            }
            if (leftLeg)
            {
                float lKick = Mathf.Sin(t * panicLegSpeed * 0.4f + 1.7f) * panicLegAngle * 0.5f * energy;
                leftLeg.localRotation = Quaternion.Slerp(
                    leftLeg.localRotation,
                    _leftLegRest * Quaternion.Euler(lKick, 0f, 0f),
                    Time.deltaTime * 6f);
            }

            if (head)
            {
                float headPitch = Mathf.Lerp(-40f, 28f, Mathf.SmoothStep(0f, 1f, phase));
                head.localRotation = Quaternion.Slerp(
                    head.localRotation,
                    _headRest * Quaternion.Euler(headPitch, 0f, 0f),
                    Time.deltaTime * 3.5f);
            }

            if (body)
            {
                body.localRotation = Quaternion.Slerp(
                    body.localRotation, _bodyRestRot, Time.deltaTime * 2.5f);
            }

            yield return null;
        }

        PlaySound(finalGulpSFX);

        StartCoroutine(FadeToBlack());

        t = 0f;
        while (t < submergeDuration)
        {
            t += Time.deltaTime;

            victim.transform.position += Vector3.down * submergeSinkSpeed * Time.deltaTime;

            if (rightWing)
            {
                Quaternion droop = _rightWingRest * Quaternion.Euler(15f, 0f, 42f);
                rightWing.localRotation = Quaternion.Slerp(
                    rightWing.localRotation, droop, Time.deltaTime * 1.8f);
            }

            if (body)
            {
                body.localRotation = Quaternion.Slerp(
                    body.localRotation,
                    _bodyRestRot * Quaternion.Euler(15f, 0f, 0f),
                    Time.deltaTime * 2f);
            }

            yield return null;
        }

        yield return new WaitUntil(() => fadeOverlay == null || fadeOverlay.alpha >= 1f);
        yield return new WaitForSeconds(postFadeDelay);

        SceneManager.LoadScene(nextSceneName);
    }

    void SnapshotRestPoses()
    {
        if (body)      { _bodyRestRot = body.localRotation; _bodyRestPos = body.localPosition; _bodyRestScale = body.localScale; }
        if (rightLeg)  _rightLegRest  = rightLeg.localRotation;
        if (leftLeg)   _leftLegRest   = leftLeg.localRotation;
        if (rightWing) _rightWingRest = rightWing.localRotation;
        if (leftWing)  _leftWingRest  = leftWing.localRotation;
        if (head)      _headRest      = head.localRotation;
    }

    IEnumerator FadeToBlack()
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        fadeOverlay.alpha = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        fadeOverlay.alpha = 1f;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 0.05f, 0f, 0.5f);
        Gizmos.matrix = transform.localToWorldMatrix;

        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
            Gizmos.DrawCube(box.center, box.size);
        else
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
#endif
}
