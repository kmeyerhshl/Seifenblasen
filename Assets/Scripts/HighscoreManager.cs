using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HighscoreManager : MonoBehaviour
{
    public TextMeshProUGUI highscoreListText;

    public void ShowHighscores(string mode)
    {
        string prefix = mode.ToString(); // Reaction oder Grid
        string result = "";

        for (int i = 0; i < 5; i++)
        {
            int score = PlayerPrefs.GetInt($"{prefix}_Score_{i}", -1);
            string date = PlayerPrefs.GetString($"{prefix}_Date_{i}", "");

            if (score >= 0)
                result += $"{i + 1}.  Score: {score}   ({date})\n";
        }

        if (result == "")
            result = "Noch keine Highscores.";

        highscoreListText.text = result;
    }

}
