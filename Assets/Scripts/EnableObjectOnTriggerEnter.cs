using UnityEngine;

public class EnableObjectOnTriggerEnter : MonoBehaviour
{
    [SerializeField] private GameObject targetToEnable;

    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (targetToEnable != null) targetToEnable.SetActive(true);
    }
}
