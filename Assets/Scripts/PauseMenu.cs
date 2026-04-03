using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [SerializeField] private MonoBehaviour[] disableWhilePaused;

    private bool isPaused;

    private readonly List<AudioSource> audioSources = new();
    private readonly Dictionary<AudioSource, bool> audioWasPlaying = new();

    private readonly List<ParticleSystem> particleSystems = new();
    private readonly Dictionary<ParticleSystem, bool> particleWasPlaying = new();

    private readonly List<Animator> animators = new();
    private readonly Dictionary<Animator, float> animatorSpeed = new();

    void Awake()
    {
        if (pausePanel) pausePanel.SetActive(false);

        CachePausables();
        SetPaused(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    void LateUpdate()
    {
        Cursor.visible   = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void CachePausables()
    {
        audioSources.Clear();
        particleSystems.Clear();
        animators.Clear();

        audioSources.AddRange(FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        particleSystems.AddRange(FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        animators.AddRange(FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    public void Pause()
    {
        SetPaused(true);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;

        if (disableWhilePaused != null)
        {
            foreach (var b in disableWhilePaused)
            {
                if (!b) continue;
                b.enabled = !paused;
            }
        }

        if (pausePanel) pausePanel.SetActive(paused);

        Time.timeScale = paused ? 0f : 1f;

        if (paused)
        {
            audioWasPlaying.Clear();
            foreach (var a in audioSources)
            {
                if (!a) continue;
                audioWasPlaying[a] = a.isPlaying;
                a.Pause();
            }

            particleWasPlaying.Clear();
            foreach (var p in particleSystems)
            {
                if (!p) continue;
                particleWasPlaying[p] = p.isPlaying;
                p.Pause(true);
            }

            animatorSpeed.Clear();
            foreach (var an in animators)
            {
                if (!an) continue;
                animatorSpeed[an] = an.speed;
                an.speed = 0f;
            }
        }
        else
        {
            foreach (var kv in audioWasPlaying)
            {
                if (!kv.Key) continue;
                if (kv.Value) kv.Key.UnPause();
            }

            foreach (var kv in particleWasPlaying)
            {
                if (!kv.Key) continue;
                if (kv.Value) kv.Key.Play(true);
            }

            foreach (var kv in animatorSpeed)
            {
                if (!kv.Key) continue;
                kv.Key.speed = kv.Value;
            }
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDisable()
    {
        if (isPaused) Time.timeScale = 1f;
    }
}
