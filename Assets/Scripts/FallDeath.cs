using System.Collections;
using UnityEngine;

public class FallDeath : MonoBehaviour
{
    public ChickController chick;
    public ChickCamera     chickCamera;
    public DeathScreen     deathScreen;

    public AudioSource audioSource;
    public AudioClip   screamSFX;

    public float deathScreenDelay = 2.2f;

    bool _triggered;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        ChickController hit = other.GetComponentInParent<ChickController>();
        if (hit == null) return;

        _triggered = true;
        StartCoroutine(FallSequence(hit));
    }

    IEnumerator FallSequence(ChickController victim)
    {
        if (chickCamera != null)
            chickCamera.Detach();

        victim.FallDie();

        if (audioSource != null && screamSFX != null)
            audioSource.PlayOneShot(screamSFX);

        yield return new WaitForSeconds(deathScreenDelay);

        if (deathScreen != null)
            deathScreen.Show();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc) Gizmos.DrawCube(transform.position + bc.center, bc.size);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.7f);
        if (bc) Gizmos.DrawWireCube(transform.position + bc.center, bc.size);
    }
#endif
}
