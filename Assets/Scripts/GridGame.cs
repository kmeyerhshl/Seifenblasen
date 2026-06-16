using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GridGame : MonoBehaviour
{
    public GridGameUIManager gridGameUIManager;
    public GameObject bubblePrefab;
    public int gridSize = 4;
    public float spacing = 0.3f;
    public Material normalMaterial;
    public Material highlightMaterial;
    [Header("Colors")]
    public Material black;
    public Material blue;
    public Material green;


    private GameObject[,] bubbles;
    private int currentX = -1;
    private int currentY = -1;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;

    [Header("Game Settings")]
    //public float gameDuration = 30f;

    [Header("Duration Limits")]
    //public float minDuration = 10f;
    //public float maxDuration = 120f;
    //public float durationStep = 5f;

    int score = 0;
    public int Score => score;
    //private float timeRemaining;
    private bool gameActive = false;

    [Header("Pop Feedback")]
    public GameObject popEffectPrefab;
    public AudioClip popSound;

    private AudioSource audioSource;

    public void StartGame()
    {
        StopAllCoroutines();

        currentX = -1;
        currentY = -1;

        ClearGrid();
        score = 0;
        //ResetGrid();
        //timeRemaining = gameDuration;
        gameActive = true;

        UpdateScoreUI();
        //UpdateTimerUI();

        GenerateGrid();
        HighlightRandomBubble();
        //gridGameUIManager.GameStart();

        //StartCoroutine(GameTimer());
    }

    /*public void IncreaseTime()
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
    }

    void Awake()
    {
        gameDuration = PlayerPrefs.GetFloat("GameDuration", gameDuration);
    }

    void SaveDuration()
    {
        PlayerPrefs.SetFloat("GameDuration", gameDuration);
    }*/

    public void RestartGame()
    {
        StopAllCoroutines();

        StartGame();
    }

    /*private IEnumerator GameTimer()
    {
        while (timeRemaining > 0)
        {
            yield return null;
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
        }

        gameActive = false;
        timerText.text = "Time: 00:00";

        // Alle Bubbles deaktivieren
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                bubbles[x, y].SetActive(false);

        // UI Game Over anzeigen
        var uiManager = FindFirstObjectByType<GridGameUIManager>();
        if (uiManager != null)
        {
            Debug.Log("ShowGameOver");
            uiManager.ShowGameOver(score);
        }
    }*/

    /*void UpdateTimerUI()
    {
        TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0, timeRemaining));
        timerText.text = $"Time: {time.Minutes:00}:{time.Seconds:00}";
    }*/

    void UpdateScoreUI()
    {
        scoreText.text = $"Score: {score}";
    }

    public void ClearGrid()
    {
        if (bubbles == null) return;

        for (int x = 0; x < bubbles.GetLength(0); x++)
        {
            for (int y = 0; y < bubbles.GetLength(1); y++)
            {
                if (bubbles[x, y] != null)
                    Destroy(bubbles[x, y]);
            }
        }
    }

    public void EndGame()
    {
        gameActive = false;

        // Alle Bubbles deaktivieren
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                bubbles[x, y].SetActive(false);

        // UI Manager informieren
        var uiManager = FindFirstObjectByType<GridGameUIManager>();
        if (uiManager != null)
            uiManager.ShowGameOver(score);
    }

    void GenerateGrid()
    {
        bubbles = new GameObject[gridSize, gridSize];

        // Hintergrund-Daten holen
        var bg = GameObject.Find("Background").transform;
        float width = bg.localScale.x;
        float height = bg.localScale.y;
        Vector3 center = bg.position;

        // Abstände berechnen
        float paddingX = 0.2f;
        float paddingY = 0.2f;

        float usableWidth = width - 2 * paddingX;
        float usableHeight = height - 2 * paddingY;

        float spacingX = usableWidth / (gridSize - 1);
        float spacingY = usableHeight / (gridSize - 1);

        // Startpunkt (unten links)
        Vector3 start = center + new Vector3(-width / 2 + paddingX, -height / 2 + paddingY, 0);

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector3 pos = start + new Vector3(x * spacingX, y * spacingY, 0);
                pos.z = center.z - 0.1f; // etwas vor Hintergrund

                GameObject b = Instantiate(bubblePrefab, pos, Quaternion.identity, transform);
                //b.transform.localScale = Vector3.one * 0.12f;

                b.GetComponent<Renderer>().material = normalMaterial;

                var handler = b.AddComponent<GridTouchHandler>();
                handler.Initialize(this, x, y);

                bubbles[x, y] = b;
            }
        }
    }

    public void HighlightRandomBubble()
    {
        // Alte entfernen
        if (currentX >= 0 && currentY >= 0)
            bubbles[currentX, currentY].GetComponent<Renderer>().material = normalMaterial;

        // Liste der aktiven Bubbles sammeln
        List<(int x, int y)> active = new List<(int, int)>();

        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                if (bubbles[x, y].activeSelf)
                    active.Add((x, y));

        if (active.Count == 0)
        {
            Debug.Log("Alle Bubbles zerplatzt!");
            return;
        }


        var chosen = active[UnityEngine.Random.Range(0, active.Count)];
        currentX = chosen.x;
        currentY = chosen.y;

        bubbles[currentX, currentY].GetComponent<Renderer>().material = highlightMaterial;
    }

    public void BubbleHit(int x, int y)
    {
        if (!gameActive) return;

        // Punkte erhöhen
        score++;
        UpdateScoreUI();
        PopBubble(bubbles[x, y].transform.position);

        // Bubble entfernen
        bubbles[x, y].SetActive(false);

        // Neue grüne Bubble wählen
        HighlightRandomBubble();

        // Wieder respawnen nach 2–3 Sekunden
        StartCoroutine(RespawnBubble(x, y));
    }

    void PopBubble(Vector3 position)
    {
        if (popEffectPrefab != null)
        {
            GameObject effect = Instantiate(popEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }
    }



    IEnumerator RespawnBubble(int x, int y)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 3.5f));

        bubbles[x, y].SetActive(true);
        bubbles[x, y].GetComponent<Renderer>().material = normalMaterial;
    }

    void ResetGrid()
    {
        if (bubbles == null)
            return;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                bubbles[x, y].SetActive(true);
                bubbles[x, y].GetComponent<Renderer>().material = normalMaterial;
            }
        }
    }


    private void Awake()
    {
        if (gridGameUIManager == null)
        {
            gridGameUIManager = FindFirstObjectByType<GridGameUIManager>();
        }
    }

    public void ColorBlack()
    {
        highlightMaterial = black;
        gridGameUIManager.CloseAllSetupSubMenus();
    }
    
        public void ColorBlue()
    {
        highlightMaterial = blue;
        gridGameUIManager.CloseAllSetupSubMenus();
    }

    public void ColorGreen()
    {
        highlightMaterial = green;
        gridGameUIManager.CloseAllSetupSubMenus();
    }


    public enum HandMode
    {
        BothHands,
        LeftHandOnly,
        RightHandOnly
    }

    public HandMode handMode = HandMode.BothHands;

    public void SetHandModeBoth()
    {
        handMode = HandMode.BothHands;
        Debug.Log("Hand mode set to BOTH");
        gridGameUIManager.CloseAllSetupSubMenus();
    }

    public void SetHandModeLeft()
    {
        handMode = HandMode.LeftHandOnly;
        Debug.Log("Hand mode set to LEFT only");
        gridGameUIManager.CloseAllSetupSubMenus();
    }

    public void SetHandModeRight()
    {
        handMode = HandMode.RightHandOnly;
        Debug.Log("Hand mode set to RIGHT only");
        gridGameUIManager.CloseAllSetupSubMenus();
    }

}

