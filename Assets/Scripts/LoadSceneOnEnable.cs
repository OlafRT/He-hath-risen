using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class LoadSceneOnEnable : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Bicycle";
    [SerializeField] private bool deactivateSelfAfterKickoff = true;
    [SerializeField] private float optionalDelay = 0f;

    private static bool s_isLoading = false;
    private bool _triggered = false;

    private void OnEnable()
    {
        s_isLoading = false;
        _triggered = false;

        StartCoroutine(LoadNext());
    }

    private System.Collections.IEnumerator LoadNext()
    {
        s_isLoading = true;

        if (optionalDelay > 0f)
            yield return new WaitForSeconds(optionalDelay);

        if (deactivateSelfAfterKickoff)
            gameObject.SetActive(false);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
        yield return op;

        s_isLoading = false;
    }
}
