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
    public GridGame gridGame;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;
    public TextMeshPro selectedTime;
    public TextMeshProUGUI scoreText;

    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public GameObject restartButton;
    public GameObject background;
    [Header("Setup UI")]
    public GameObject setupCanvas;
    [Header("Duration Settings")]
    public float gameDuration = 30f;
    public float minDuration = 10f;
    public float maxDuration = 120f;
    public float step = 5f;
    [Header("Buttons")]
    public GameObject levelButtons;
    public GameObject handButtons;
    public GameObject colorButtons;

    //private float gameDuration = 30f;
    private float timer = 0f;
    private bool gameActive = false;
    public TextMeshProUGUI highscoreText;
    private bool highscoreSaved = false;

    [Header("Pop Feedback")]
    public AudioClip popSound;

    private AudioSource audioSource;

    private void Start()
    {
        setupCanvas.SetActive(true);
        timerText.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);
        gameOverPanel.SetActive(false);
        restartButton.SetActive(false);
        background.SetActive(false);
        levelButtons.SetActive(false);
        handButtons.SetActive(false);
        colorButtons.SetActive(false);

        UpdateTimePreview();
        //GameStart();
    }

    public void IncreaseTime()
    {
        gameDuration = Mathf.Min(gameDuration + step, maxDuration);
        UpdateTimePreview();
        SaveDuration();
    }

    public void DecreaseTime()
    {
        gameDuration = Mathf.Max(gameDuration - step, minDuration);
        UpdateTimePreview();
        SaveDuration();
    }

    void UpdateTimePreview()
    {
        TimeSpan t = TimeSpan.FromSeconds(gameDuration);
        selectedTime.text = $"{t.Minutes:00}:{t.Seconds:00}";
    }

    void SaveDuration()
    {
        PlayerPrefs.SetFloat("GridGameDuration", gameDuration);
    }

    void Awake()
    {
        gameDuration = PlayerPrefs.GetFloat("GridGameDuration", gameDuration);
    }

    public void StartFromSetup()
    {
        setupCanvas.SetActive(false);
        background.SetActive(true);
        StartCoroutine(StartCountdown());
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }


    /*public void GameStart()
    {
        gameOverPanel.SetActive(false);
        StartCoroutine(StartCountdown());
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }*/

    IEnumerator StartCountdown()
    {
        timerText.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);
        countdownText.gameObject.SetActive(true);
        // 1️⃣ Levelanzeige
        countdownText.text = $"Level {gridGame.gridSize} x {gridGame.gridSize}";
        yield return new WaitForSeconds(1.2f);

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

    void StartGame()
    {
        gridGame.StartGame();
        timer = gameDuration;
        gameActive = true;
        timerText.gameObject.SetActive(true);
        scoreText.gameObject.SetActive(true);
        //countdownText.gameObject.SetActive(false);
        //gameOverPanel.SetActive(false);
        //restartButton.gameObject.SetActive(false);
        UpdateScore(0);
    }

    private void Update()
    {
        if (!gameActive) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            timer = 0;
            EndGame();
        }

        TimeSpan timeSpan = TimeSpan.FromSeconds(timer);
        timerText.text = $"Time: {timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    }

    public void UpdateScore(int score)
    {
        scoreText.text = $"Score: {score}";
    }

    public void SetGridSize(int size)
    {
        //StopAllCoroutines();

        gridGame.gridSize = size;
        gridGame.ClearGrid();

        gameOverPanel.SetActive(false);
        restartButton.SetActive(false);
        levelButtons.SetActive(false);

        //StartCoroutine(StartCountdown());
    }

    public void CloseAllSetupSubMenus()
    {
        levelButtons.SetActive(false);
        handButtons.SetActive(false);
        colorButtons.SetActive(false);
    }


    public void SelectLevel()
    {
        CloseAllSetupSubMenus();
        levelButtons.SetActive(true);
    }

    public void SelectHand()
    {
        CloseAllSetupSubMenus();
        handButtons.SetActive(true);
    }

    public void SelectColor()
    {
        CloseAllSetupSubMenus();
        colorButtons.SetActive(true);
    }


    void EndGame()
    {
        gameActive = false;
        gridGame.EndGame();
        timerText.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);
        gameOverPanel.SetActive(true);
        finalScoreText.text = $"Endscore: {gridGame.Score}";
        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }
        if (!highscoreSaved)
        {
            highscoreSaved = true;
            SaveHighscore(gridGame.Score, "Grid");
        }
        setupCanvas.SetActive(true);
    }

    void SaveHighscore(int newScore, string mode)
    {
        string prefix = mode.ToString();  // "Reaction" oder "Grid"
        List<int> scores = new List<int>();
        List<string> dates = new List<string>();
        int maxEntries = 5;

        // 1️⃣ Bestehende Highscores laden
        for (int i = 0; i < maxEntries; i++)
        {
            int s = PlayerPrefs.GetInt($"{prefix}_Score_{i}", -1);
            string d = PlayerPrefs.GetString($"{prefix}_Date_{i}", "");

            if (s >= 0)
            {
                scores.Add(s);
                dates.Add(d);
            }
        }

        // 2️⃣ Neuen Score hinzufügen
        scores.Add(newScore);
        dates.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        // 3️⃣ Kombinieren und sortieren
        var combined = new List<(int score, string date)>();
        for (int i = 0; i < scores.Count; i++)
            combined.Add((scores[i], dates[i]));

        combined.Sort((a, b) => b.score.CompareTo(a.score));

        // 4️⃣ Nur Top 5
        combined = combined.GetRange(0, Mathf.Min(maxEntries, combined.Count));

        // 5️⃣ Speichern
        for (int i = 0; i < combined.Count; i++)
        {
            PlayerPrefs.SetInt($"{prefix}_Score_{i}", combined[i].score);
            PlayerPrefs.SetString($"{prefix}_Date_{i}", combined[i].date);
        }

        PlayerPrefs.Save();
    }


    public void Restart()
    {
        StopAllCoroutines();

        gameActive = false;
        timer = 0f;

        gridGame.ClearGrid();

        gameOverPanel.SetActive(false);
        setupCanvas.SetActive(true);
        //restartButton.SetActive(false);
        //finalScoreText.gameObject.SetActive(false);

        //GameStart();
        //StartCoroutine(StartCountdown());
    }

    public void ShowGameOver(int finalScore)
    {
        gameOverPanel.SetActive(true);
        finalScoreText.text = $"Endscore: {finalScore}";
        //restartButton.gameObject.SetActive(true);
        //restartButton.SetActive(true);
    }
}

