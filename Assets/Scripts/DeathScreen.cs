using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DeathScreen : MonoBehaviour
{
    public CanvasGroup textPanel;
    public TMP_Text    deathLabel;
    public CanvasGroup blackOverlay;

    public AudioSource audioSource;
    public AudioClip   deathSFX;

    public string deathText = "You Died";

    public float panelFadeDuration  = 0.6f;
    public float holdDuration       = 1.8f;
    public float blackFadeDuration  = 0.7f;
    public float holdBlackDuration  = 0.3f;

    void Awake()
    {
        if (textPanel)    textPanel.alpha    = 0f;
        if (blackOverlay) blackOverlay.alpha = 0f;

        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (deathLabel) deathLabel.text = deathText;
        if (audioSource != null && deathSFX != null)
            audioSource.PlayOneShot(deathSFX);
        StartCoroutine(DeathSequence());
        if (GameStats.Instance != null) GameStats.Instance.AddDeath();
    }

    IEnumerator DeathSequence()
    {
        yield return Fade(textPanel, 0f, 1f, panelFadeDuration);

        yield return new WaitForSeconds(holdDuration);

        yield return Fade(blackOverlay, 0f, 1f, blackFadeDuration);

        yield return new WaitForSeconds(holdBlackDuration);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
}
