using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms.Impl;
using Unity.VisualScripting;

public class GridGameUIManager : MonoBehaviour
{
    // Referenz auf die Spiellogik des Grid-Spiels
    public GridGame gridGame;

    // UI-Elemente für Countdown, Timer und Punktestand
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;
    public TextMeshPro selectedTime;
    public TextMeshProUGUI scoreText;

    // UI-Elemente für Spielende
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public GameObject restartButton;
    public GameObject background;

    [Header("Setup UI")]
    // Einstellungsmenü vor Spielbeginn
    public GameObject setupCanvas;

    [Header("Duration Settings")]
    // Spielzeit-Einstellungen
    public float gameDuration = 30f;
    public float minDuration = 10f;
    public float maxDuration = 120f;
    public float step = 5f;

    [Header("Buttons")]
    // Untermenüs für Einstellungen
    public GameObject levelButtons;
    public GameObject handButtons;
    public GameObject colorButtons;

    // Laufender Spieltimer
    private float timer = 0f;

    // Gibt an, ob aktuell gespielt wird
    private bool gameActive = false;

    // Highscore-Anzeige
    public TextMeshProUGUI highscoreText;

    // Verhindert mehrfaches Speichern eines Highscores
    private bool highscoreSaved = false;

    [Header("Pop Feedback")]
    // Soundeffekt beim Spielende
    public AudioClip popSound;

    private AudioSource audioSource;

    public float CurrentTime
    {
        get
        {
            return gameDuration - timer;
        }
    }

    private void Start()
    {
        // Startzustand der Benutzeroberfläche

        setupCanvas.SetActive(true);

        timerText.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);

        gameOverPanel.SetActive(false);
        restartButton.SetActive(false);

        background.SetActive(false);

        levelButtons.SetActive(false);
        handButtons.SetActive(false);
        colorButtons.SetActive(false);

        // Vorschau der eingestellten Spielzeit anzeigen
        UpdateTimePreview();
    }

    /// <summary>
    /// Erhöht die Spieldauer um den definierten Schrittwert.
    /// </summary>
    public void IncreaseTime()
    {
        gameDuration = Mathf.Min(gameDuration + step, maxDuration);

        UpdateTimePreview();
        SaveDuration();
    }

    /// <summary>
    /// Verringert die Spieldauer um den definierten Schrittwert.
    /// </summary>
    public void DecreaseTime()
    {
        gameDuration = Mathf.Max(gameDuration - step, minDuration);

        UpdateTimePreview();
        SaveDuration();
    }

    /// <summary>
    /// Aktualisiert die Vorschau der gewählten Spielzeit im Setup-Menü.
    /// </summary>
    void UpdateTimePreview()
    {
        TimeSpan t = TimeSpan.FromSeconds(gameDuration);

        selectedTime.text = $"{t.Minutes:00}:{t.Seconds:00}";
    }

    /// <summary>
    /// Speichert die aktuell gewählte Spielzeit dauerhaft.
    /// </summary>
    void SaveDuration()
    {
        PlayerPrefs.SetFloat("GridGameDuration", gameDuration);
    }

    private void Awake()
    {
        // Gespeicherte Spielzeit laden
        gameDuration = PlayerPrefs.GetFloat("GridGameDuration", gameDuration);
    }

    /// <summary>
    /// Wird vom Start-Button im Setup-Menü aufgerufen.
    /// </summary>
    public void StartFromSetup()
    {
        setupCanvas.SetActive(false);
        background.SetActive(true);

        StartCoroutine(StartCountdown());

        // Audioquelle erzeugen
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    /// <summary>
    /// Zeigt vor Spielbeginn einen Countdown an.
    /// </summary>
    IEnumerator StartCountdown()
    {
        timerText.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);

        countdownText.gameObject.SetActive(true);

        // Gewähltes Level anzeigen
        countdownText.text =
            $"Level {gridGame.gridSize} x {gridGame.gridSize}";

        yield return new WaitForSeconds(1.2f);

        // Countdown 3 → 1
        int count = 3;

        while (count > 0)
        {
            countdownText.text = count.ToString();

            yield return new WaitForSeconds(1f);

            count--;
        }

        countdownText.text = "Los!";

        yield return new WaitForSeconds(0.5f);

        countdownText.gameObject.SetActive(false);

        StartGame();
    }

    /// <summary>
    /// Startet das eigentliche Spiel.
    /// </summary>
    void StartGame()
    {
        gridGame.StartGame();

        timer = gameDuration;
        gameActive = true;

        timerText.gameObject.SetActive(true);
        scoreText.gameObject.SetActive(true);

        UpdateScore(0);
    }

    private void Update()
    {
        // Nur aktualisieren, wenn das Spiel läuft
        if (!gameActive)
            return;

        // Countdown der verbleibenden Spielzeit
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            timer = 0;
            EndGame();
        }

        // Zeit formatiert anzeigen
        TimeSpan timeSpan = TimeSpan.FromSeconds(timer);

        timerText.text =
            $"Time: {timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    }

    /// <summary>
    /// Aktualisiert die Punktestand-Anzeige.
    /// </summary>
    public void UpdateScore(int score)
    {
        scoreText.text = $"Score: {score}";
    }

    /// <summary>
    /// Ändert die Größe des Spielfeldes (z.B. 3x3, 4x4, 5x5).
    /// </summary>
    public void SetGridSize(int size)
    {
        gridGame.gridSize = size;

        // Altes Spielfeld entfernen
        gridGame.ClearGrid();

        gameOverPanel.SetActive(false);
        restartButton.SetActive(false);
        levelButtons.SetActive(false);
    }

    /// <summary>
    /// Schließt alle geöffneten Untermenüs.
    /// </summary>
    public void CloseAllSetupSubMenus()
    {
        levelButtons.SetActive(false);
        handButtons.SetActive(false);
        colorButtons.SetActive(false);
    }

    /// <summary>
    /// Öffnet die Levelauswahl.
    /// </summary>
    public void SelectLevel()
    {
        CloseAllSetupSubMenus();

        levelButtons.SetActive(true);
    }

    /// <summary>
    /// Öffnet die Handauswahl.
    /// </summary>
    public void SelectHand()
    {
        CloseAllSetupSubMenus();

        handButtons.SetActive(true);
    }

    /// <summary>
    /// Öffnet die Farbauswahl.
    /// </summary>
    public void SelectColor()
    {
        CloseAllSetupSubMenus();

        colorButtons.SetActive(true);
    }

    /// <summary>
    /// Beendet das Spiel nach Ablauf der Zeit.
    /// </summary>
    void EndGame()
    {
        gameActive = false;

        gridGame.EndGame();

        timerText.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);

        gameOverPanel.SetActive(true);

        finalScoreText.text =
            $"Endscore: {gridGame.Score}";

        // Soundeffekt abspielen
        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }

        // Highscore speichern
        if (!highscoreSaved)
        {
            highscoreSaved = true;

            SaveHighscore(gridGame.Score, "Grid");
        }

        // Zurück ins Setup-Menü
        setupCanvas.SetActive(true);
    }

    /// <summary>
    /// Speichert die besten fünf Ergebnisse dauerhaft.
    /// </summary>
    void SaveHighscore(int newScore, string mode)
    {
        string prefix = mode.ToString();

        List<int> scores = new List<int>();
        List<string> dates = new List<string>();

        int maxEntries = 5;

        // Vorhandene Highscores laden
        for (int i = 0; i < maxEntries; i++)
        {
            int s =
                PlayerPrefs.GetInt($"{prefix}_Score_{i}", -1);

            string d =
                PlayerPrefs.GetString($"{prefix}_Date_{i}", "");

            if (s >= 0)
            {
                scores.Add(s);
                dates.Add(d);
            }
        }

        // Neuen Eintrag hinzufügen
        scores.Add(newScore);
        dates.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        // Kombinieren und sortieren
        var combined = new List<(int score, string date)>();

        for (int i = 0; i < scores.Count; i++)
        {
            combined.Add((scores[i], dates[i]));
        }

        combined.Sort((a, b) =>
            b.score.CompareTo(a.score));

        // Nur Top 5 behalten
        combined =
            combined.GetRange(0,
            Mathf.Min(maxEntries, combined.Count));

        // Speichern
        for (int i = 0; i < combined.Count; i++)
        {
            PlayerPrefs.SetInt(
                $"{prefix}_Score_{i}",
                combined[i].score);

            PlayerPrefs.SetString(
                $"{prefix}_Date_{i}",
                combined[i].date);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Setzt das Spiel zurück und kehrt ins Setup-Menü zurück.
    /// </summary>
    public void Restart()
    {
        StopAllCoroutines();

        gameActive = false;
        timer = 0f;

        gridGame.ClearGrid();

        gameOverPanel.SetActive(false);

        setupCanvas.SetActive(true);
    }

    /// <summary>
    /// Zeigt den Game-Over-Bildschirm mit Endpunktzahl an.
    /// </summary>
    public void ShowGameOver(int finalScore)
    {
        gameOverPanel.SetActive(true);

        finalScoreText.text =
            $"Endscore: {finalScore}";
    }
}

