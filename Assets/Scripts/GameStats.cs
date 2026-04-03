using UnityEngine;

public class GameStats : MonoBehaviour
{
    public static GameStats Instance { get; private set; }

    const string DEATHS_KEY = "RunDeaths";

    public int Deaths => PlayerPrefs.GetInt(DEATHS_KEY, 0);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddDeath()
    {
        PlayerPrefs.SetInt(DEATHS_KEY, Deaths + 1);
        PlayerPrefs.Save();
    }

    public void Reset()
    {
        PlayerPrefs.DeleteKey(DEATHS_KEY);
        PlayerPrefs.Save();
    }
}
