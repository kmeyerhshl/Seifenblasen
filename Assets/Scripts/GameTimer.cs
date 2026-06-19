using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Verwaltet den zeitlichen Ablauf einer Spielrunde.
/// Die Klasse steuert den Spiel-Countdown, die verbleibende Spielzeit,
/// die Benutzeroberfläche sowie den Start und das Ende einer Runde.
/// </summary>
public class GameTimer : MonoBehaviour
{
    // ------------------- UI REFERENZEN -------------------

    [Header("UI")]

    // Canvas mit Spielinformationen und Timer
    public GameObject infoCanvas;

    // Anzeige der verbleibenden Spielzeit
    public TextMeshProUGUI timerText;

    // Anzeige der aktuell gewählten Spieldauer
    public TextMeshPro selectedTime;

    // Anzeige für Hinweise und Countdown
    public TextMeshProUGUI hintText;

    // Endbildschirm
    public GameObject endCanvas;

    // Neustart-Button
    public GameObject restartButton;

    // Einstellungsmenü vor Spielbeginn
    public GameObject setupCanvas;

    // Untermenüs für Spielkonfiguration
    public GameObject levelButtons;
    public GameObject handButtons;
    public GameObject modeButtons;
    public GameObject colorButtons;


    // ------------------- SPIELEINSTELLUNGEN -------------------

    [Header("Settings")]

    // Dauer einer Spielrunde in Sekunden
    public float gameDuration = 30f;

    // Aktuell vergangene Spielzeit
    public float currentTime = 0f;

    // Gibt an, ob der Timer läuft
    private bool timerActive = false;

    // ------------------- DAUERBESCHRÄNKUNGEN -------------------

    [Header("Duration Limits")]

    // Kürzeste erlaubte Spielzeit
    public float minDuration = 10f;

    // Längste erlaubte Spielzeit
    public float maxDuration = 120f;

    // Schrittweite beim Erhöhen oder Verringern
    public float durationStep = 5f;

    // Referenz auf den zentralen Spielmanager
    private GameManager gameManager;

    // ------------------- AUDIO -------------------

    [Header("Pop Feedback")]

    // Sound beim Spielende
    public AudioClip popSound;

    // Audioquelle für Endsound
    private AudioSource audioSource;

    [Header("Countdown Feedback")]

    // Sound für den Countdown
    public AudioClip countdownSound;

    // Audioquelle für Countdown-Sounds
    private AudioSource audioCountdown;

    // Sound beim Spielstart
    public AudioClip startSound;

    // Audioquelle für Startsignal
    private AudioSource audioStart;

    /// <summary>
    /// Initialisiert Referenzen und lädt die zuletzt gespeicherte
    /// Spieldauer aus den Benutzereinstellungen.
    /// </summary>
    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        gameDuration = PlayerPrefs.GetFloat("GameDuration", gameDuration);
    }

    /// <summary>
    /// Initialisiert Audioquellen und setzt die Benutzeroberfläche
    /// in ihren Ausgangszustand.
    /// </summary>
    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        audioCountdown = gameObject.AddComponent<AudioSource>();
        audioCountdown.playOnAwake = false;
        audioCountdown.spatialBlend = 1f;

        audioStart = gameObject.AddComponent<AudioSource>();
        audioStart.playOnAwake = false;
        audioStart.spatialBlend = 1f;

        setupCanvas.SetActive(true);
        gameManager.instructionText.text = "Fangen Sie so viele Seifenblasen wie möglich!";
        infoCanvas.SetActive(false);
        endCanvas.SetActive(false);
        restartButton.SetActive(false);
        levelButtons.SetActive(false);
        handButtons.SetActive(false);
        modeButtons.SetActive(false);
        colorButtons.SetActive(false);
        UpdatePreviewUI();
        //StartCountdown();
    }

    /// <summary>
    /// Wird vom Setup-Menü aufgerufen.
    /// Schließt das Konfigurationsmenü und startet den Countdown.
    /// </summary>
    public void StartGameFromSetup()
    {
        setupCanvas.SetActive(false);
        StartCountdown();
    }

    // =========================
    // COUNTDOWN
    // =========================

    /// <summary>
    /// Startet den Spiel-Countdown.
    /// Bereits laufende Coroutines werden vorher beendet.
    /// </summary>
    public void StartCountdown()
    {
        setupCanvas.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(CountdownRoutine());
    }

    /// <summary>
    /// Führt den visuellen und akustischen Countdown vor Spielbeginn aus.
    /// Nach Ablauf wird das eigentliche Spiel gestartet.
    /// </summary>
    IEnumerator CountdownRoutine()
    {
        timerActive = false;
        infoCanvas.SetActive(true);
        endCanvas.SetActive(false);
        restartButton.SetActive(false);

        hintText.text = $"Level {gameManager.currentLevel}";
        yield return new WaitForSeconds(1.2f);

        for (int i = 3; i > 0; i--)
        {
            hintText.text = i.ToString();
            if (countdownSound != null && audioCountdown != null)
            {
                audioCountdown.PlayOneShot(countdownSound);
            }
            yield return new WaitForSeconds(1f);

        }

        hintText.text = "Los!";
        if (startSound != null && audioStart != null)
        {
            audioStart.PlayOneShot(startSound);
        }
        yield return new WaitForSeconds(0.5f);

        //infoCanvas.SetActive(false);

        gameManager.StartGame();
        StartTimer();
    }

    /// <summary>
    /// Setzt die Benutzeroberfläche nach einer Spielrunde zurück.
    /// Aktuell wird dabei kein automatischer Neustart ausgelöst.
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("🔄 Restart Button gedrückt");

        StopAllCoroutines();
        timerActive = false;

        endCanvas.SetActive(false);
        restartButton.SetActive(false);
        infoCanvas.SetActive(false);

        //setupCanvas.SetActive(true);
        //gameManager.RestartGame();

        //StartCountdown();
    }


    // =========================
    // TIMER
    // =========================

    /// <summary>
    /// Startet den eigentlichen Spieltimer.
    /// Die vergangene Spielzeit wird auf null zurückgesetzt.
    /// </summary>
    void StartTimer()
    {
        currentTime = 0f;
        timerActive = true;
    }

    /// <summary>
    /// Erhöht die Spieldauer innerhalb der definierten Grenzen
    /// und aktualisiert die Anzeige.
    /// </summary>
    public void IncreaseTime()
    {
        //if (timerActive) return; // während Spiel nicht ändern

        gameDuration = Mathf.Min(gameDuration + durationStep, maxDuration);
        UpdatePreviewUI();
        SaveDuration();
    }

    /// <summary>
    /// Verringert die Spieldauer innerhalb der definierten Grenzen
    /// und aktualisiert die Anzeige.
    /// </summary>
    public void DecreaseTime()
    {
        //if (timerActive) return;

        gameDuration = Mathf.Max(gameDuration - durationStep, minDuration);
        UpdatePreviewUI();
        SaveDuration();
    }

    /// <summary>
    /// Aktualisiert die Anzeige der aktuell gewählten Spieldauer
    /// im Setup-Menü.
    /// </summary>
    void UpdatePreviewUI()
    {
        TimeSpan time = TimeSpan.FromSeconds(gameDuration);
        timerText.text = $"Time: {time.Minutes:00}:{time.Seconds:00}";
        selectedTime.text = $"{time.Minutes:00}:{time.Seconds:00}";
    }


    /// <summary>
    /// Speichert die aktuell gewählte Spieldauer dauerhaft
    /// in den Unity PlayerPrefs.
    /// </summary>
    void SaveDuration()
    {
        PlayerPrefs.SetFloat("GameDuration", gameDuration);
    }

    /// <summary>
    /// Wird in jedem Frame aufgerufen.
    /// Aktualisiert die verbleibende Spielzeit und beendet
    /// das Spiel automatisch nach Ablauf des Timers.
    /// </summary>
    void Update()
    {
        if (!timerActive) return;

        currentTime += Time.deltaTime;
        float remaining = gameDuration - currentTime;

        if (remaining <= 0)
        {
            remaining = 0;
            timerActive = false;
            EndGame();
        }

        TimeSpan time = TimeSpan.FromSeconds(remaining);
        timerText.text = $"Time: {time.Minutes:00}:{time.Seconds:00}";
    }

    /// <summary>
    /// Beendet die aktuelle Spielrunde.
    /// Spieltimer und Spielmanager werden gestoppt,
    /// die Benutzeroberfläche wird zurückgesetzt und
    /// ein akustisches Signal ausgegeben.
    /// </summary>
    void EndGame()
    {
        Debug.Log("⏰ Zeit abgelaufen");

        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }

        timerActive = false;
        gameManager.EndGame();

        //endCanvas.SetActive(true);
        //restartButton.SetActive(true);
        setupCanvas.SetActive(true);
        infoCanvas.SetActive(false);
    }
}
