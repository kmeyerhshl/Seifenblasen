using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// Lädt eine Szene anhand ihres Namens.
    /// Vor dem Laden wird geprüft, ob die Szene
    /// in den Build Settings verfügbar ist.
    /// </summary>
    /// <param name="sceneName">
    /// Name der zu ladenden Szene.
    /// </param>
    public void LoadSceneByName(string sceneName)
    {
        // Prüfen, ob die Szene geladen werden kann
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            // Szene wechseln
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            // Warnung ausgeben, falls die Szene nicht gefunden wurde
            Debug.LogWarning("Szene nicht gefunden: " + sceneName);
        }
    }

    /// <summary>
    /// Lädt die aktuell aktive Szene erneut
    /// und setzt dadurch den Szenenzustand zurück.
    /// </summary>
    public void RestartScene()
    {
        // Name der aktuellen Szene ermitteln
        string currentScene =
            SceneManager.GetActiveScene().name;

        // Aktuelle Szene neu laden
        SceneManager.LoadScene(currentScene);
    }
}
