using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

/// <summary>
/// Implementiert ein Rasterspiel mit Seifenblasen.
/// Innerhalb eines festen Gitters wird jeweils eine Zielblase markiert,
/// die vom Nutzer berührt werden muss. Nach erfolgreicher Auswahl wird
/// die Blase entfernt, die Punktzahl erhöht und eine neue Zielblase gewählt.
/// </summary>
public class GridGame : MonoBehaviour
{
    // ------------------- REFERENZEN -------------------

    // UI-Manager für Menüs und Spielende
    public GridGameUIManager gridGameUIManager;

    // Prefab einer einzelnen Blase
    public GameObject bubblePrefab;
    // ------------------- GITTEREINSTELLUNGEN -------------------

    // Anzahl der Zeilen und Spalten des Gitters
    public int gridSize = 4;

    // Abstand zwischen den Blasen
    public float spacing = 0.3f;

    // Standardmaterial für normale Blasen
    public Material normalMaterial;

    // Material der aktuell markierten Zielblase
    public Material highlightMaterial;

    // Material der aktuell markierten Zielblase
    // ------------------- FARBOPTIONEN -------------------

    [Header("Colors")]

    // Verfügbare Zielfarben
    public Material black;
    public Material blue;
    public Material green;


    // Zweidimensionales Array aller Blasenobjekte
    private GameObject[,] bubbles;

    // Position der aktuell markierten Zielblase
    private int currentX = -1;
    private int currentY = -1;

    // ------------------- UI -------------------

    [Header("UI References")]

    // Anzeige der verbleibenden Zeit
    public TextMeshProUGUI timerText;

    // Anzeige der aktuellen Punktzahl
    public TextMeshProUGUI scoreText;

    [Header("Game Settings")]
    //public float gameDuration = 30f;

    [Header("Duration Limits")]
    //public float minDuration = 10f;
    //public float maxDuration = 120f;
    //public float durationStep = 5f;

    // Aktueller Punktestand
    int score = 0;

    // Öffentlicher Zugriff auf den Punktestand
    public int Score => score;

    // Gibt an, ob aktuell eine Spielrunde läuft
    private bool gameActive = false;

    [Header("Pop Feedback")]
    public GameObject popEffectPrefab;
    public AudioClip popSound;

    private AudioSource audioSource;

    private Dictionary<int, TargetReactionData> targetLogs = new Dictionary<int, TargetReactionData>();
    public EyeTrackingGridGame eyeTracking;
    //public GridGameUIManager uiManager;
    private GameObject currentTarget;
    private int currentTargetId = -1;
    private int nextTargetId = 1;
    [System.Serializable]
    public class BubblePosition
    {
        public int row;
        public int column;
        public Vector3 position;
    }

    private List<BubblePosition> bubblePositions = new List<BubblePosition>();

    /// <summary>
    /// Startet eine neue Spielrunde.
    /// Das Spielfeld wird zurückgesetzt, die Punktzahl gelöscht
    /// und ein neues Raster erzeugt.
    /// </summary>
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

        eyeTracking.ClearData();
        eyeTracking.isTracking = true;
        targetLogs.Clear();
        nextTargetId = 1;
        currentTargetId = -1;
        currentTarget = null;

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

    /// <summary>
    /// Startet das Spiel vollständig neu.
    /// </summary>
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

    /// <summary>
    /// Aktualisiert die Anzeige der aktuellen Punktzahl.
    /// </summary>
    void UpdateScoreUI()
    {
        scoreText.text = $"Score: {score}";
    }

    /// <summary>
    /// Entfernt alle aktuell erzeugten Blasenobjekte
    /// aus dem Raster.
    /// </summary>
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

    /// <summary>
    /// Beendet die Spielrunde.
    /// Alle Blasen werden deaktiviert und der UI-Manager
    /// über das Spielende informiert.
    /// </summary>
    public void EndGame()
    {
        gameActive = false;
        var data = eyeTracking.GetData();
        Debug.Log("Gaze samples: " + data.Count);
        SaveGazeData(data);
        SaveTargetData();
        eyeTracking.isTracking = false;

        // Alle Bubbles deaktivieren
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                bubbles[x, y].SetActive(false);

        // UI Manager informieren
        var uiManager = FindFirstObjectByType<GridGameUIManager>();
        if (uiManager != null)
            uiManager.ShowGameOver(score);
    }

    public void SaveGazeData(List<EyeTrackingGridGame.GridGazeSample> data)
    {
        string fileName = "gaze_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

        string path = Path.Combine(
            Application.persistentDataPath,
            fileName);

        using (StreamWriter writer = new StreamWriter(path))
        {
            foreach (var s in data)
            {
                writer.WriteLine(
                    $"{s.time};" +
                    $"{s.origin.x};{s.origin.y};{s.origin.z};" +
                    $"{s.direction.x};{s.direction.y};{s.direction.z};" +
                    $"{s.headForward.x};{s.headForward.y};{s.headForward.z};" +
                    $"{s.eyeHeadAngle};" +
                    $"{s.hitPoint.x};{s.hitPoint.y};{s.hitPoint.z};" +
                    $"{s.hitSomething};" +
                    $"{(s.hitObject != null ? s.hitObject.name : "None")}"
                );
            }
        }
        Debug.Log(path);
    }

    public void SaveTargetData()
    {
        string fileName = "bubble_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

        string path = Path.Combine(
            Application.persistentDataPath,
            fileName);

        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("targetId;row;column;x;y;z;appearTime;wasSeen;lookTime;wasTouched;touchTime");

            foreach (var t in targetLogs.Values)
            {
                writer.WriteLine(
                    $"{t.targetId};" +
                    $"{t.row};" +
                    $"{t.column};" +
                    $"{t.position.x};" +
                    $"{t.position.y};" +
                    $"{t.position.z};" +
                    $"{t.appearTime};" +
                    $"{t.wasSeen};" +
                    $"{t.lookTime};" +
                    $"{t.wasTouched};" +
                    $"{t.touchTime}"
                );
            }
        }

        Debug.Log("Targetdaten gespeichert: " + path);
    }

    /// <summary>
    /// Erzeugt ein neues Raster aus Blasenobjekten.
    /// Die Positionen werden anhand der Größe des
    /// Hintergrundobjekts berechnet.
    /// </summary>
    void GenerateGrid()
    {
        bubbles = new GameObject[gridSize, gridSize];

        // Hintergrund-Daten holen
        var bg = GameObject.Find("Background").transform;
        float width = bg.localScale.x;
        float height = bg.localScale.y;
        Vector3 center = bg.position;

        // Vom Hintergrund wird zunächst auf jeder Seite ein Rand (padding) abgezogen
        float paddingX = 0.2f;
        float paddingY = 0.2f;

        float usableWidth = width - 2 * paddingX;
        float usableHeight = height - 2 * paddingY;

        // Abstand zwischen den Blasen berechnen
        float spacingX = usableWidth / (gridSize - 1);
        float spacingY = usableHeight / (gridSize - 1);

        // Startpunkt (unten links)
        Vector3 start = center + new Vector3(-width / 2 + paddingX, -height / 2 + paddingY, 0);

        bubblePositions.Clear();
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
                bubblePositions.Add(new BubblePosition
                {
                    row = x,
                    column = y,
                    position = pos
                });
            }
        }
        SaveGridLayout();
    }

    public void SaveGridLayout()
    {
        string file =
            "grid_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") +
            ".txt";

        using (StreamWriter writer =
            new StreamWriter(Path.Combine(
                Application.persistentDataPath,
                file)))
        {
            foreach (var b in bubblePositions)
            {
                writer.WriteLine(
                    $"{b.row};" +
                    $"{b.column};" +
                    $"{b.position.x};" +
                    $"{b.position.y};" +
                    $"{b.position.z}"
                );
            }
        }
    }

    /// <summary>
    /// Wählt zufällig eine aktive Blase aus
    /// und markiert sie als aktuelles Ziel.
    /// </summary>
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
        currentTarget = bubbles[currentX, currentY];
        var handler = currentTarget.GetComponent<GridTouchHandler>();

        if (handler != null)
        {
            handler.hasBeenLookedAt = false;
            handler.firstLookTime = -1f;
        }
        currentTargetId = nextTargetId++;

        targetLogs[currentTargetId] = new TargetReactionData
        {
            targetId = currentTargetId,

            row = currentY,
            column = currentX,

            position = bubbles[currentX, currentY].transform.position,

            appearTime = gridGameUIManager.CurrentTime
            //appearTime = gridGameUIManager.gameDuration - gridGameUIManager.CurrentTime
        };
    }

    /// <summary>
    /// Wird aufgerufen, wenn die Zielblase erfolgreich
    /// berührt wurde.
    /// </summary>
    public void BubbleHit(int x, int y)
    {
        if (!gameActive) return;

        // Punkte erhöhen
        score++;
        UpdateScoreUI();
        PopBubble(bubbles[x, y].transform.position);

        targetLogs[currentTargetId].wasTouched = true;

        //targetLogs[currentTargetId].touchTime = gridGameUIManager.CurrentTime - targetLogs[currentTargetId].appearTime;
        targetLogs[currentTargetId].touchTime = gridGameUIManager.CurrentTime;

        // Bubble entfernen
        bubbles[x, y].SetActive(false);

        // Neue grüne Bubble wählen
        HighlightRandomBubble();

        // Wieder respawnen nach 2–3 Sekunden
        StartCoroutine(RespawnBubble(x, y));
    }

    /// <summary>
    /// Spielt Partikel- und Soundeffekte
    /// beim Platzen einer Blase ab.
    /// </summary>
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


    /// <summary>
    /// Aktiviert eine zuvor entfernte Blase nach
    /// einer zufälligen Wartezeit erneut.
    /// </summary>
    IEnumerator RespawnBubble(int x, int y)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 3.5f));

        bubbles[x, y].SetActive(true);
        bubbles[x, y].GetComponent<Renderer>().material = normalMaterial;
    }

    /// <summary>
    /// Aktiviert alle Blasen erneut und setzt
    /// ihre Materialien auf den Standardzustand zurück.
    /// </summary>
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

    /// <summary>
    /// Sucht automatisch den UI-Manager,
    /// falls dieser nicht manuell zugewiesen wurde.
    /// </summary>
    private void Awake()
    {
        if (gridGameUIManager == null)
        {
            gridGameUIManager = FindFirstObjectByType<GridGameUIManager>();
        }
    }
    void Update()
    {
        if (!gameActive) return;
        //currentTime += Time.deltaTime;
        CheckEyeTracking();
    }

    void CheckEyeTracking()
    {
        if (eyeTracking == null)
            return;

        var sample = eyeTracking.GetLatestSample();

        if (sample == null)
            return;

        if (!sample.hitObject)
            return;

        if (currentTarget == null)
            return;

        if (sample.hitObject != currentTarget)
            return;

        if (!targetLogs[currentTargetId].wasSeen)
        {
            targetLogs[currentTargetId].wasSeen = true;

            //targetLogs[currentTargetId].lookTime = gridGameUIManager.CurrentTime - targetLogs[currentTargetId].appearTime;
            targetLogs[currentTargetId].lookTime = gridGameUIManager.CurrentTime;
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

[System.Serializable]
public class TargetReactionData
{
    public int targetId;
    public int row;
    public int column;
    public Vector3 position;
    public float appearTime;
    public bool wasSeen;
    public float lookTime;
    public bool wasTouched;
    public float touchTime;
}