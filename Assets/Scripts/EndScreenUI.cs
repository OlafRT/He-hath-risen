using TMPro;
using UnityEngine;

public class EndScreenUI : MonoBehaviour
{
    public TextMeshPro deathCountLabel;

    public string formatString = "You died {0} times!";

    public string singleDeathString = "You died 1 time!";

    void Start()
    {
        int deaths = GameStats.Instance != null ? GameStats.Instance.Deaths : 0;

        if (deathCountLabel != null)
        {
            deathCountLabel.text = deaths == 1
                ? singleDeathString
                : string.Format(formatString, deaths);
        }

        if (GameStats.Instance != null)
            GameStats.Instance.Reset();
    }
}
