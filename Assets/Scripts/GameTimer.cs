using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [Header("UI")]
    public GameObject infoCanvas;
    public TextMeshProUGUI timerText;
    public TextMeshPro selectedTime;
    public TextMeshProUGUI hintText;
    public GameObject endCanvas;
    public GameObject restartButton;
    public GameObject setupCanvas;
    public GameObject levelButtons;
    public GameObject handButtons;
    public GameObject modeButtons;
    public GameObject colorButtons;


    [Header("Settings")]
    public float gameDuration = 30f;

    public float currentTime = 0f;
    private bool timerActive = false;

    [Header("Duration Limits")]
    public float minDuration = 10f;
    public float maxDuration = 120f;
    public float durationStep = 5f;

    private GameManager gameManager;

    [Header("Pop Feedback")]
    public AudioClip popSound;

    private AudioSource audioSource;
    [Header("Countdown Feedback")]
    public AudioClip countdownSound;
    private AudioSource audioCountdown;
    public AudioClip startSound;
    private AudioSource audioStart;

    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        gameDuration = PlayerPrefs.GetFloat("GameDuration", gameDuration);
    }

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

    public void StartGameFromSetup()
    {
        setupCanvas.SetActive(false);
        StartCountdown();
    }

    // =========================
    // COUNTDOWN
    // =========================

    public void StartCountdown()
    {
        setupCanvas.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(CountdownRoutine());
    }

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

    void StartTimer()
    {
        currentTime = 0f;
        timerActive = true;
    }

    public void IncreaseTime()
    {
        //if (timerActive) return; // während Spiel nicht ändern

        gameDuration = Mathf.Min(gameDuration + durationStep, maxDuration);
        UpdatePreviewUI();
        SaveDuration();
    }

    public void DecreaseTime()
    {
        //if (timerActive) return;

        gameDuration = Mathf.Max(gameDuration - durationStep, minDuration);
        UpdatePreviewUI();
        SaveDuration();
    }

    void UpdatePreviewUI()
    {
        TimeSpan time = TimeSpan.FromSeconds(gameDuration);
        timerText.text = $"Time: {time.Minutes:00}:{time.Seconds:00}";
        selectedTime.text = $"{time.Minutes:00}:{time.Seconds:00}";
    }



    void SaveDuration()
    {
        PlayerPrefs.SetFloat("GameDuration", gameDuration);
    }

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

/*public class GameTimer : MonoBehaviour
{
    [Header("UI References")]
    //public GameObject hintCanvas;   // Canvas mit Countdown-Zahlen
    public GameObject infoCanvas;   // Canvas mit Timeranzeige
    public TextMeshProUGUI _text;   // Timertext (z. B. „Zeit: 00:30“)
    public TextMeshProUGUI hintText; // Countdown-Anzeige

    public GameObject endCanvas;
    public TextMeshProUGUI _endText;
    public GameObject restartButton;
    public GameObject endGame;

    [Header("Settings")]
    public float gameDuration = 30f; // Spielzeit in Sekunden

    private bool _timerActive = false;
    private float _currentTime = 0f;

    [Header("Pop Feedback")]
    public AudioClip popSound;

    private AudioSource audioSource;

    void Start()
    {
        endCanvas.SetActive(false);
        restartButton.SetActive(false);
        StartCoroutine(StartAfterCountdown());
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    IEnumerator StartAfterCountdown()
    {
        infoCanvas.SetActive(true);
        //infoCanvas.SetActive(false);
        //hintCanvas.SetActive(true);

        int count = 3;
        while (count > 0)
        {
            ShowHint(count.ToString());
            yield return new WaitForSeconds(1f);
            count--;
        }

        ShowHint("Los!");
        yield return new WaitForSeconds(0.5f);
        //FindObjectOfType<BubbleSpawner>()?.ResumeSpawning();

        //hintCanvas.SetActive(false);
        //infoCanvas.SetActive(true);

        StartTimer();
    }

    /*public void RestartGame()
    {
        Debug.Log("🔄 Spiel wird neu gestartet...");

        // 1) Timer stoppen
        _timerActive = false;

        // 2) Alle Werte zurücksetzen
        _currentTime = 0f;
        var gm = FindFirstObjectByType<GameManager>();
        gm.score = 0;

        // 3) Alte Bubbles löschen
        var spawner = FindFirstObjectByType<BubbleSpawner>();
        if (spawner != null)
        {
            spawner.ResetBubbles();
            spawner.StartSpawning();
        }

        // 4) UI zurücksetzen
        infoCanvas.SetActive(false);
        //hintCanvas.SetActive(true);
        endCanvas.SetActive(false);

        // 5) Countdown erneut starten
        StartCoroutine(StartAfterCountdown());
        restartButton.SetActive(false);
    }
    

    void ShowHint(string msg)
    {
        //if (hintText != null)
        _text.text = msg;
    }

    void Update()
    {
        if (!_timerActive) return;

        _currentTime += Time.deltaTime;

        float timeRemaining = gameDuration - _currentTime;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            _timerActive = false;
            if (popSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(popSound);
            }
            EndGame();
            restartButton.SetActive(true);
        }

        // Zeit formatieren
        TimeSpan time = TimeSpan.FromSeconds(timeRemaining);
        _text.text = $"Time: {time.Minutes:00}:{time.Seconds:00}";
    }

    public void StartTimer()
    {
        _currentTime = 0f;
        _timerActive = true;

        // Bubble-Spawner aktivieren
        FindFirstObjectByType<BubbleSpawner>()?.StartSpawning();
    }

    public void StopTimer()
    {
        _timerActive = false;
        FindFirstObjectByType<BubbleSpawner>()?.StopSpawning();
        //FindObjectOfType<BubbleSpawner>()?.StartSpawning();
    }

    private void EndGame()
    {
        endCanvas.SetActive(true);
        restartButton.SetActive(true);
        StopTimer();
        Debug.Log("⏰ Spielzeit vorbei!");
        _endText.text = "Spiel vorbei";
        infoCanvas.SetActive(false);

        // Optional: Punktestand ausgeben
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.EndGame();
            Debug.Log("gm EndGame");
        }
    }
}*/
