using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TombIntro : MonoBehaviour
{

    public ChickController chick;
    public Transform rockPivot;

    public Vector3 rollTranslation  = new Vector3(7f, 0f, 0f);
    public float   rollAngle        = -180f;
    public float   rollDuration     = 2.2f;
    public AnimationCurve rollCurve = new AnimationCurve(
        new Keyframe(0.00f, 0f, 0f,  0f),
        new Keyframe(0.35f, 0.08f),
        new Keyframe(0.75f, 0.82f),
        new Keyframe(1.00f, 1f, 1.5f, 0f)
    );

    public ParticleSystem rockSeamDust;
    [Range(0f, 1f)]
    public float  seamParticleStartT = 0.05f;
    public ParticleSystem openingDust;
    public ParticleSystem openingDebris;

    public AudioSource audioSource;
    public AudioClip   rockRumbleSFX;
    public AudioClip   rockThudSFX;
    public AudioClip   revelationSFX;

    public float prePause           = 1.0f;
    public float postRockPause      = 0.6f;

    public CanvasGroup introPanel;
    public TMP_Text    introText;
    [TextArea]
    public string      introMessage    = "He is risen.";
    public float       textFadeIn      = 0.8f;
    public float       textHold        = 2.5f;
    public float       textFadeOut     = 0.8f;

    Vector3    _rockStartPos;
    Quaternion _rockStartRot;

    void Start()
    {
        if (rockPivot)
        {
            _rockStartPos = rockPivot.localPosition;
            _rockStartRot = rockPivot.localRotation;
        }

        if (introPanel) introPanel.alpha = 0f;
        if (introText)  introText.text   = introMessage;

        StopParticles(rockSeamDust);
        StopParticles(openingDust);
        StopParticles(openingDebris);

        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(prePause);

        PlaySound(rockRumbleSFX);

        yield return RollRock();

        PlaySound(rockThudSFX);
        StartParticles(openingDust);
        StartParticles(openingDebris);

        PlaySound(revelationSFX);
        yield return Fade(introPanel, 0f, 1f, textFadeIn);
        yield return new WaitForSeconds(textHold);
        yield return Fade(introPanel, 1f, 0f, textFadeOut);

    }

    IEnumerator RollRock()
    {
        if (!rockPivot) yield break;

        Vector3    endLocalPos = _rockStartPos + rollTranslation;
        Quaternion endLocalRot = _rockStartRot * Quaternion.Euler(0f, 0f, rollAngle);

        bool seamStarted = false;
        float elapsed    = 0f;

        while (elapsed < rollDuration)
        {
            elapsed    += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / rollDuration);
            float tCurve = rollCurve.Evaluate(t);

            rockPivot.localPosition = Vector3.Lerp(_rockStartPos, endLocalPos, tCurve);
            rockPivot.localRotation = Quaternion.Slerp(_rockStartRot, endLocalRot, tCurve);

            if (!seamStarted && t >= seamParticleStartT)
            {
                seamStarted = true;
                StartParticles(rockSeamDust);
            }

            yield return null;
        }

        rockPivot.localPosition = endLocalPos;
        rockPivot.localRotation = endLocalRot;

        StopParticles(rockSeamDust);
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    void StartParticles(ParticleSystem ps)
    {
        if (ps != null) ps.Play();
    }

    void StopParticles(ParticleSystem ps)
    {
        if (ps != null) ps.Stop();
    }
}
