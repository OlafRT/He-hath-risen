using System.Collections;
using UnityEngine;
 
public class SpikeKill : MonoBehaviour
{
    public ChickController chick;
    public DeathScreen deathScreen;
 
    public float bodyTipOffset       = 0.35f;
    public float slideDistance       = 0.9f;
    public float slideRandomRange    = 0.35f;
    public float slideDuration       = 0.45f;
    public float rotationRandomRange = 25f;
    public float deathScreenDelay    = 1.1f;
 
    public ParticleSystem bloodBurst;
 
    public AudioSource audioSource;
    public AudioClip   impaleSFX;
    public AudioClip   sadSFX;
 

    static bool s_dead;

    void Awake()
    {
        s_dead = false;
    }
 
    void OnTriggerEnter(Collider other)
    {
        if (s_dead) return;
 
        ChickController hit = other.GetComponentInParent<ChickController>();
        if (hit == null) return;
 
        s_dead = true;
        StartCoroutine(ImpalementSequence(hit));
    }
 
    IEnumerator ImpalementSequence(ChickController victim)
    {
        victim.Die();

        Transform spikeRoot = transform.parent != null ? transform.parent : transform;

        Vector3 initialSlideDir = (spikeRoot.position - transform.position).normalized;
        if (initialSlideDir.sqrMagnitude < 0.01f) initialSlideDir = Vector3.down;
        Vector3 slideDirLocal = Quaternion.Inverse(spikeRoot.rotation) * initialSlideDir;

        float randomYRot = Random.Range(-rotationRandomRange, rotationRandomRange);
        Vector3 fwd = victim.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude > 0.01f)
            victim.transform.rotation = Quaternion.LookRotation(fwd.normalized)
                * Quaternion.Euler(0f, randomYRot, 0f);

        Vector3 bodyUpOffset = victim.transform.up * bodyTipOffset;

        victim.transform.position = transform.position - bodyUpOffset;

        Quaternion rotOffsetFromSpike = Quaternion.Inverse(spikeRoot.rotation)
                                        * victim.transform.rotation;

        if (bloodBurst != null)
        {
            bloodBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            bloodBurst.Play();
        }

        PlaySound(impaleSFX);

        float thisSlideDist = slideDistance + Random.Range(0f, slideRandomRange);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);

            Vector3    tipNow = transform.position;
            Vector3    dirNow = (spikeRoot.rotation * slideDirLocal).normalized;
            Quaternion rotNow = spikeRoot.rotation * rotOffsetFromSpike;

            Vector3 currentBodyOffset = rotNow * Vector3.up * bodyTipOffset;

            victim.transform.position = tipNow - currentBodyOffset + dirNow * (thisSlideDist * t);
            victim.transform.rotation = rotNow;

            yield return null;
        }

        {
            Vector3    tipFinal = transform.position;
            Vector3    dirFinal = (spikeRoot.rotation * slideDirLocal).normalized;
            Quaternion rotFinal = spikeRoot.rotation * rotOffsetFromSpike;
            Vector3    finalBodyOffset = rotFinal * Vector3.up * bodyTipOffset;

            victim.transform.position = tipFinal - finalBodyOffset + dirFinal * thisSlideDist;
            victim.transform.rotation = rotFinal;
        }

        foreach (Renderer r in victim.GetComponentsInChildren<Renderer>())
            r.enabled = false;

        if (CorpseManager.Instance != null)
            CorpseManager.Instance.RegisterCorpse(SnapshotCorpse(victim), spikeRoot);

        yield return new WaitForSeconds(0.15f);
        PlaySound(sadSFX);
 
        yield return new WaitForSeconds(deathScreenDelay);
 
        if (deathScreen != null)
            deathScreen.Show();
    }
 
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    CorpseData SnapshotCorpse(ChickController victim)
    {
        Transform root = victim.transform;
        Transform body = root.Find("Body");

        CorpseData d = new CorpseData();
        d.position = new SerializedVector3(root.position);
        d.rotation = new SerializedQuaternion(root.rotation);

        if (body != null)
        {
            d.bodyRot   = new SerializedQuaternion(body.localRotation);
            d.bodyScale = new SerializedVector3(body.localScale);

            d.rightLegRot  = ChildRot(body, "RightLeg");
            d.leftLegRot   = ChildRot(body, "LeftLeg");
            d.rightWingRot = ChildRot(body, "RightWing");
            d.leftWingRot  = ChildRot(body, "LeftWing");
            d.headRot      = ChildRot(body, "Head");
        }

        return d;
    }

    SerializedQuaternion ChildRot(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        return new SerializedQuaternion(t != null ? t.localRotation : Quaternion.identity);
    }
 
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, 0.18f);

        if (transform.parent != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawLine(transform.position, transform.parent.position);
        }
    }
#endif
}
