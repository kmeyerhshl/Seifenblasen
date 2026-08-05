using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HighscoreManager : MonoBehaviour
{
    // UI-Textfeld zur Anzeige der Highscore-Liste
    public TextMeshProUGUI highscoreListText;

    /// <summary>
    /// Lädt und zeigt die gespeicherten Highscores
    /// für den gewählten Spielmodus an.
    /// </summary>
    public void ShowHighscores(string mode)
    {
        // Präfix für die entsprechenden PlayerPrefs-Einträge
        string prefix = mode.ToString();

        // String zur Ausgabe der Highscore-Liste
        string result = "";

        // Die fünf besten gespeicherten Ergebnisse laden
        for (int i = 0; i < 5; i++)
        {
            int score =
                PlayerPrefs.GetInt($"{prefix}_Score_{i}", -1);

            string date =
                PlayerPrefs.GetString($"{prefix}_Date_{i}", "");

            // Nur gültige Einträge anzeigen
            if (score >= 0)
            {
                result +=
                    $"{i + 1}.  Score: {score}   ({date})\n";
            }
        }

        // Falls noch keine Ergebnisse gespeichert wurden
        if (result == "")
        {
            result = "Noch keine Highscores.";
        }

        // Highscore-Liste im UI anzeigen
        highscoreListText.text = result;
    }
}
