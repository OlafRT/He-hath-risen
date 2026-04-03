using UnityEngine;
using UnityEngine.UI;

public class QuitButton : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(Quit);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnEnable()
    {
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(Quit);
    }
}
