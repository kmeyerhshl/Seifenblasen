using System.Collections;
using UnityEngine;
using TMPro; // Für Textanzeige (UI)
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using Microsoft.MixedReality.Toolkit;


/// <summary>
/// Zentrale Steuerung des Spiels.
/// Der GameManager verwaltet den Spielablauf, die Punktvergabe,
/// die verschiedenen Spielmodi, die Levelverwaltung, die Speicherung
/// von Eye-Tracking-Daten sowie die Erfassung und Auswertung von
/// Reaktionszeiten.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ------------------- REFERENZEN -------------------

    // Eye-Tracking-Komponente zur Aufzeichnung der Blickdaten
    public EyeTracking eyeTracker;

    // Referenz auf den Spieltimer
    private GameTimer gameTimer;

    // ------------------- SPIELEINSTELLUNGEN -------------------

    [Header("Game Settings")]

    // Dauer einer Spielrunde in Sekunden
    public float gameDuration = 30f;

    // Verantwortlich für Erzeugung und Verwaltung der Blasen
    public BubbleSpawner spawner;

    // ------------------- UI -------------------

    [Header("UI")]

    // Anzeige des aktuellen Punktestands
    public TextMeshProUGUI scoreText;

    // Anzeige von Spielanweisungen
    public TextMeshPro instructionText;

    // ------------------- LEVEL -------------------

    [Header("Levels")]

    // Aktuell gewählter Schwierigkeitsgrad
    public int currentLevel = 1;

    // Spielbereich für Level 1 (klein)
    public Vector3 level1Area = new Vector3(0.8f, 0.7f, 0.8f);

    // Spielbereich für Level 2 (mittel)
    public Vector3 level2Area = new Vector3(1.2f, 1.0f, 1.0f);

    // Spielbereich für Level 3 (groß)
    public Vector3 level3Area = new Vector3(1.6f, 1.3f, 1.0f);

    // ------------------- HIGHSCORE -------------------

    [Header("Highscore")]

    // UI-Anzeige der Highscores
    public TextMeshProUGUI highscoreText;

    // ------------------- STANDARDWERTE -------------------

    [Header("Default Play Area (no calibration)")]

    // Standard-Höhenbereich ohne vorherige Kalibrierung
    public float defaultMinY = 0f;
    public float defaultMaxY = 1f;

    // Gibt an, ob aktuell eine Spielrunde läuft
    private bool gameActive = false;

    // Verhindert mehrfaches Speichern eines Highscores
    private bool highscoreSaved = false;

    // Aktueller Punktestand
    public int score { get; private set; }

    // ------------------- FARBEN -------------------

    [Header("Colors")]

    // Vordefinierte Materialien für Farbmodi
    public Material red;
    public Material blue;
    public Material green;

    // Aktuell ausgewählte Zielfarbe
    public Color highlightMaterial;

    // =========================
    // GAME FLOW
    // =========================

    /// <summary>
    /// Definiert die verfügbaren Spielmodi.
    /// </summary>
    public enum GameMode
    {
        // Jede Blase darf geplatzt werden
        Standard,
        // Nur farblich markierte Blasen zählen
        ColorOnly,
        // Blasen müssen in einer bestimmten Reihenfolge gewählt werden
        Sequence
    }

    public GameMode currentGameMode = GameMode.Standard;

    public int expectedSequenceNumber = 1;
    private List<int> activeSequenceNumbers = new List<int>();
    private int nextSequenceNumberToAssign = 1;
    public List<float> reactionTimes = new List<float>();
    public List<float> eyeReactionTimes = new List<float>();

    [System.Serializable]
    public class BubbleReactionData
    {
        public int bubbleId;
        public Vector3 spawnPosition;

        public float spawnTime;

        public bool wasSeen = false;
        //public float firstLookTime = -1f;
        public float firstLookTime;

        public bool wasTouched = false;
        //public float touchTime = -1f;
        public float touchTime;
    }

    public class GazeLog
    {
        public float time;
        public Vector3 origin;
        public Vector3 direction;
        public Vector3 hitPoint;
        public int bubbleId;
        public bool hitBubble;
    }

    public List<GazeLog> gazeLogs = new List<GazeLog>();

    public Dictionary<int, BubbleReactionData> bubbleLogs = new Dictionary<int, BubbleReactionData>();

    private int nextBubbleId = 1;

    public void ResetSequence()
    {
        expectedSequenceNumber = 1;
        UpdateScoreUI();
    }

    /// <summary>
    /// Wird in jedem Frame während einer aktiven Spielrunde aufgerufen.
    /// Übernimmt die vom EyeTracking-System aufgezeichneten Blickdaten
    /// und speichert diese für die spätere Analyse.
    /// </summary>
    void Update()
    {
        if (!gameActive || eyeTracker == null) return;

        //var gazeData = eyeTracker.GetLatestGaze();
        var gazeData = eyeTracker.GetData();
        foreach (var s in gazeData)//        {
            gazeLogs.Add(new GazeLog
            {
                time = s.time,
                origin = s.origin,
                direction = s.direction,
                hitPoint = s.hitPoint,
                hitBubble = s.hitBubble,
            });
        //}

        //CheckGazeOnBubbles(gazeData);
    }

    /// <summary>
    /// Prüft, ob der aktuelle Blickstrahl eine aktive Blase trifft.
    /// Wird aktuell nicht verwendet, stellt jedoch eine alternative
    /// Methode zur Erkennung von Blickkontakten dar.
    /// </summary>
    void CheckGazeOnBubbles(Vector3 gazePoint)
    {
        var eyeProvider = CoreServices.InputSystem?.EyeGazeProvider;
        if (eyeProvider == null) return;

        Vector3 origin = eyeProvider.GazeOrigin;
        Vector3 gazeDir = eyeProvider.GazeDirection;

        foreach (var b in spawner.GetActiveBubbles())
        {
            if (b.obj == null) continue;

            Vector3 toBubble = (b.obj.transform.position - origin).normalized;

            float angle = Vector3.Angle(gazeDir, toBubble);

            if (angle < 8f) // Blick trifft Bubble
            {
                var handler = b.obj.GetComponent<BubbleTouchHandler>();

                if (!handler.hasBeenLookedAt)
                {
                    handler.hasBeenLookedAt = true;
                    handler.firstLookTime = Time.time - handler.spawnTime;

                    LogEyeReactionTime(handler.bubbleId, handler.firstLookTime);
                }
            }
        }
    }

    /// <summary>
    /// Registriert eine neu erzeugte Blase und erzeugt einen
    /// Datensatz für die spätere Auswertung.
    /// </summary>
    public int RegisterBubble(float spawnTime, Vector3 pos)
    {
        int id = nextBubbleId++;

        bubbleLogs[id] = new BubbleReactionData
        {
            bubbleId = id,
            spawnTime = spawnTime,
            spawnPosition = pos
        };

        return id;
    }

    /// <summary>
    /// Speichert die Eye-Reaktionszeit einer Blase.
    /// Die Zeit entspricht der Dauer zwischen Erscheinen der Blase
    /// und dem ersten Blickkontakt.
    /// </summary>
    public void LogEyeReactionTime(int id, float t)
    {
        if (bubbleLogs.ContainsKey(id))
        {
            bubbleLogs[id].wasSeen = true;
            bubbleLogs[id].firstLookTime = t;
        }
    }

    /// <summary>
    /// Speichert die Touch-Reaktionszeit einer Blase.
    /// Die Zeit entspricht der Dauer zwischen Erscheinen der Blase
    /// und der Berührung durch den Nutzer.
    /// </summary>
    public void LogTouchReactionTime(int id, float t)
    {
        if (bubbleLogs.ContainsKey(id))
        {
            bubbleLogs[id].wasTouched = true;
            bubbleLogs[id].touchTime = t;
        }
    }

    /// <summary>
    /// Exportiert sämtliche während einer Spielrunde erfassten Daten.
    /// Es werden getrennte Dateien für Spielinformationen,
    /// Blasendaten und Eye-Tracking-Daten erzeugt.
    /// </summary>
    public void SaveReactionSummary(List<EyeTracking.GazeSample> data)
    {
        string fileInfo = "info_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        string pathInfo = Path.Combine(Application.persistentDataPath, fileInfo);
        using (StreamWriter writer = new StreamWriter(pathInfo))
        {
            writer.WriteLine("Uhrzeit: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
            writer.WriteLine("Spielmodus: " + currentGameMode);
            writer.WriteLine("Level: " + currentLevel);
            writer.WriteLine("Dauer: " + gameDuration);
            writer.WriteLine("Punktzahl: " + score);
            /*foreach (var b in bubbleLogs.Values)
            {
                writer.WriteLine(
                  $"{b.bubbleId};{b.firstLookTime};{b.touchTime}");
            }
            /*writer.WriteLine("TOUCH REACTION TIMES:");
            foreach (var t in reactionTimes)
                writer.WriteLine(t);

            writer.WriteLine("\nEYE REACTION TIMES:");
            foreach (var t in eyeReactionTimes)
                writer.WriteLine(t);*/
            /*writer.WriteLine("FIELD DATA");
            writer.WriteLine("Center;" + spawner.center);
            writer.WriteLine("Size;" + spawner.areaSize);
            writer.WriteLine("Level;" + currentLevel);
            writer.WriteLine("MinY;" + PlayerPrefs.GetFloat("Level_MinY"));
            writer.WriteLine("MaxY;" + PlayerPrefs.GetFloat("Level_MaxY")); */
        }

        string fileBubble = "bubble_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        string pathBubble = Path.Combine(Application.persistentDataPath, fileBubble);
        using (StreamWriter writer = new StreamWriter(pathBubble))
        {
            writer.WriteLine("Spawn Bereich: ");
            writer.WriteLine($"{spawner.minX};{spawner.maxX};{spawner.minY};{spawner.maxY};{spawner.minZ};{spawner.maxZ}");
            writer.WriteLine("");
            writer.WriteLine("Positionen Seifenblasen:");
            foreach (var b in bubbleLogs.Values)
            {
                writer.WriteLine(
                    $"{b.bubbleId};" +
                    $"{b.spawnPosition.x};{b.spawnPosition.y};{b.spawnPosition.z};" +
                    $"{b.wasSeen};{b.firstLookTime};" +
                    $"{b.wasTouched};{b.touchTime}"
                );
            }
        }

        string fileGaze = "gaze_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        string pathGaze = Path.Combine(Application.persistentDataPath, fileGaze);
        using (StreamWriter writer = new StreamWriter(pathGaze))
        {
            foreach (var p in data)
            {
                writer.WriteLine($"{p.time};{p.origin.x};{p.origin.y};{p.origin.z};{p.direction.x};{p.direction.y};{p.direction.z};{p.headForward.x};{p.headForward.y};{p.headForward.z};{p.eyeHeadAngle};{p.hitPoint.x};{p.hitPoint.y};{p.hitPoint.z};{p.hitBubble};{p.bubbleId}");
            }
        }

        Debug.Log("Saved reaction summary: ");
    }

    /// <summary>
    /// Startet eine neue Spielrunde.
    /// Alle Messdaten werden zurückgesetzt, das Eye Tracking aktiviert
    /// und die Blasenerzeugung gestartet.
    /// </summary>
    public void StartGame()
    {
        Debug.Log("▶️ StartGame");
        //instructionText.text = "Fangen Sie so viele Seifenblasen wie möglich";

        eyeTracker.ClearData();
        gazeLogs.Clear();
        bubbleLogs.Clear();
        nextBubbleId = 1;
        eyeTracker.isTracking = true;
        Debug.Log("eyeTracker is true");

        score = 0;
        highscoreSaved = false;
        gameActive = true;

        UpdateScoreUI();
        ApplyLevelSettings();

        //expectedSequenceNumber = 1;
        expectedSequenceNumber = -1;
        activeSequenceNumbers.Clear();
        nextSequenceNumberToAssign = 1;

        spawner.ResetBubbles();
        spawner.StartSpawning();
    }

    /// <summary>
    /// Exportiert aufgezeichnete Blickdaten in eine Textdatei.
    /// Wird hauptsächlich für Debug- und Testzwecke verwendet.
    /// </summary>
    void SaveToFile(List<EyeTracking.GazeSample> data)
    {
        string fileName = "gazeData_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

        string path = Path.Combine(Application.persistentDataPath, fileName);

        using (StreamWriter writer = new StreamWriter(path))
        {
            foreach (var p in data)
            {
                writer.WriteLine($"{p.time};{p.origin.x};{p.origin.y};{p.origin.z};{p.hitPoint.x};{p.hitPoint.y};{p.hitPoint.z};{p.hitBubble}");
                //writer.WriteLine($"{p.x};{p.y};{p.z}");
            }
        }

        Debug.Log("Saved to: " + path);
    }

    /// <summary>
    /// Beendet die aktuelle Spielrunde.
    /// Das Eye Tracking wird gestoppt, alle Daten gespeichert
    /// und der Highscore aktualisiert.
    /// </summary>
    public void EndGame()
    {
        if (!gameActive) return;

        eyeTracker.isTracking = false;

        var data = eyeTracker.GetData();
        Debug.Log("Gaze samples: " + data.Count);
        //SaveToFile(data);
        SaveReactionSummary(data);

        Debug.Log("⏹ EndGame");
        gameActive = false;

        spawner.StopSpawning();
        spawner.ResetBubbles();

        if (!highscoreSaved)
        {
            highscoreSaved = true;
            SaveHighscore(score, "Reaction");
        }
        eyeTracker.ResetData();
    }

    /// <summary>
    /// Bricht die aktuelle Spielrunde ab und entfernt
    /// alle aktiven Blasen.
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("🔄 RestartGame");

        gameActive = false;
        spawner.StopSpawning();
        spawner.ResetBubbles();
    }

    /// <summary>
    /// Initialisiert wichtige Referenzen vor Spielbeginn.
    /// Wird automatisch beim Laden der Szene aufgerufen.
    /// </summary>
    private void Awake()
    {
        if (gameTimer == null)
        {
            gameTimer = FindFirstObjectByType<GameTimer>();
        }
        if (highlightMaterial == default)
        {
            highlightMaterial = Color.green;
        }
    }

    /// <summary>
    /// Schließt alle Menüs zur Spielkonfiguration.
    /// </summary>
    public void CloseAllSetupSubMenus()
    {
        gameTimer.levelButtons.SetActive(false);
        gameTimer.handButtons.SetActive(false);
        gameTimer.modeButtons.SetActive(false);
        gameTimer.colorButtons.SetActive(false);
    }

    /// <summary>
    /// Öffnet das Menü zur Auswahl des Schwierigkeitsgrades.
    /// </summary>
    public void SelectLevel()
    {
        CloseAllSetupSubMenus();
        gameTimer.levelButtons.SetActive(true);
    }

    /// <summary>
    /// Öffnet das Menü zur Auswahl der erlaubten Hand.
    /// </summary>
    public void SelectHand()
    {
        CloseAllSetupSubMenus();
        gameTimer.handButtons.SetActive(true);
    }

    /// <summary>
    /// Öffnet das Menü zur Auswahl des Spielmodus.
    /// </summary>
    public void SelectMode()
    {
        CloseAllSetupSubMenus();
        gameTimer.modeButtons.SetActive(true);
    }

    /// <summary>
    /// Aktiviert den gewählten Spielmodus und passt
    /// die Spielanweisung entsprechend an.
    /// </summary>
    public void SelectMode(int mode)
    {
        if (mode == 0)
        {
            currentGameMode = GameMode.Standard;
            gameTimer.modeButtons.SetActive(false);
            instructionText.text = "Fangen Sie so viele Seifenblasen wie möglich!";
        }
        else if (mode == 1)
        {
            currentGameMode = GameMode.ColorOnly;
            gameTimer.modeButtons.SetActive(false);
            instructionText.text = "Fangen Sie nur die bunten Seifenblasen!";
        }
        else if (mode == 2)
        {
            currentGameMode = GameMode.Sequence;
            gameTimer.modeButtons.SetActive(false);
            instructionText.text = "Fangen Sie die Seifenblase in der vorgegebenen Reihenfolge!";
        }
    }

    /// <summary>
    /// Vergibt eine neue Sequenznummer für eine erzeugte Blase.
    /// Die Nummer wird gleichzeitig zur Liste aktiver Ziele hinzugefügt.
    /// </summary>
    public int GetNextSequenceNumber()
    {
        Debug.Log("GetNextSequenceNumber");
        int number = nextSequenceNumberToAssign;
        nextSequenceNumberToAssign++;

        activeSequenceNumbers.Add(number);

        // Wenn noch kein Ziel gesetzt → eines wählen
        if (expectedSequenceNumber == -1)
            ChooseNewTarget();

        return number;
    }

    /// <summary>
    /// Entfernt eine Sequenznummer aus der Liste aktiver Ziele.
    /// Wird aufgerufen, wenn die zugehörige Blase erfolgreich
    /// ausgewählt wurde.
    /// </summary>
    public void RemoveSequenceNumber(int number)
    {
        Debug.Log("RemoveSequenceNumber");
        activeSequenceNumbers.Remove(number);

        if (number == expectedSequenceNumber)
            ChooseNewTarget();
    }

    /// <summary>
    /// Wählt zufällig eine der aktuell vorhandenen
    /// Sequenznummern als nächstes Ziel aus.
    /// </summary>
    void ChooseNewTarget()
    {
        Debug.Log("ChooseNewTarget");
        if (activeSequenceNumbers.Count == 0)
        {
            expectedSequenceNumber = -1;
            return;
        }

        int index = UnityEngine.Random.Range(0, activeSequenceNumbers.Count);
        expectedSequenceNumber = activeSequenceNumbers[index];

        UpdateScoreUI();
    }

    /// <summary>
    /// Öffnet das Menü zur Auswahl der Zielfarbe.
    /// </summary>
    public void SelectColor()
    {
        CloseAllSetupSubMenus();
        gameTimer.colorButtons.SetActive(true);
    }

    // =========================
    // LEVEL
    // =========================
    /// <summary>
    /// Setzt den gewünschten Schwierigkeitsgrad.
    /// </summary>
    public void SelectLevel(int level)
    {
        Debug.Log($"🎚 Level {level} gewählt");
        currentLevel = level;
        gameTimer.levelButtons.SetActive(false);
        //RestartGame();

        //FindFirstObjectByType<GameTimer>()?.StartCountdown();
    }

    /// <summary>
    /// Berechnet den für das aktuelle Level gültigen Spielbereich.
    /// Grundlage sind die zuvor ermittelten Kalibrierungsdaten
    /// des Nutzers.
    /// </summary>
    void ApplyLevelSettings()
    {
        bool hasCalib = PlayerPrefs.HasKey("Calib_MinY") && PlayerPrefs.HasKey("Calib_MaxY");

        if (!hasCalib)
        {
            Debug.LogWarning("⚠️ Keine Kalibrierung vorhanden – benutze Default-Y-Bereich");
            PlayerPrefs.SetFloat("Level_MinY", defaultMinY);
            PlayerPrefs.SetFloat("Level_MaxY", defaultMaxY);
            PlayerPrefs.Save();
            return;
        }

        float calibMinY = PlayerPrefs.GetFloat("Calib_MinY");
        float calibMaxY = PlayerPrefs.GetFloat("Calib_MaxY");

        Debug.Log($"Calib values found: MinY={calibMinY}, MaxY={calibMaxY}");

        if (calibMinY >= calibMaxY)
        {
            Debug.LogWarning("⚠️ Kalibrierungswerte ungültig (MinY >= MaxY). Benutze Default-Werte.");
            calibMinY = defaultMinY;
            calibMaxY = defaultMaxY;

            PlayerPrefs.SetFloat("Level_MinY", calibMinY);
            PlayerPrefs.SetFloat("Level_MaxY", calibMaxY);
            PlayerPrefs.Save();
            return;
        }

        float range = calibMaxY - calibMinY;

        float levelFactor = 1f;

        switch (currentLevel)
        {
            case 1: levelFactor = 0.4f; break;
            case 2: levelFactor = 0.7f; break;
            case 3: levelFactor = 1.0f; break;
        }

        float centerY = (calibMinY + calibMaxY) / 2f;
        float halfRange = (range * levelFactor) / 2f;

        float levelMinY = centerY - halfRange;
        float levelMaxY = centerY + halfRange;

        PlayerPrefs.SetFloat("Level_MinY", levelMinY);
        PlayerPrefs.SetFloat("Level_MaxY", levelMaxY);

        Debug.Log($"Level Y-Range: {levelMinY} – {levelMaxY}");

        // X/Z weiterhin über areaSize
        switch (currentLevel)
        {
            case 1: spawner.areaSize = level1Area; break;
            case 2: spawner.areaSize = level2Area; break;
            case 3: spawner.areaSize = level3Area; break;
        }
    }


    /*void ApplyLevelSettings()
    {
        bool hasCalib = PlayerPrefs.HasKey("Calib_MinY") && PlayerPrefs.HasKey("Calib_MaxY");

        if (hasCalib)
        {
            float calibMin = PlayerPrefs.GetFloat("Calib_MinY");
            float calibMax = PlayerPrefs.GetFloat("Calib_MaxY");
            Debug.Log($"Calib values found: MinY={calibMin}, MaxY={calibMax}");
        }

        if (!hasCalib)
        {
            Debug.LogWarning("⚠️ Keine Kalibrierung vorhanden – benutze Default-Y-Bereich");
            //PlayerPrefs.DeleteKey("Level_MinY");
            //PlayerPrefs.DeleteKey("Level_MaxY");
            PlayerPrefs.SetFloat("Level_MinY", defaultMinY);
            PlayerPrefs.SetFloat("Level_MaxY", defaultMaxY);
            PlayerPrefs.Save();
            return;
        }

        float calibMinY = PlayerPrefs.GetFloat("Calib_MinY");
        float calibMaxY = PlayerPrefs.GetFloat("Calib_MaxY");

        if (calibMinY >= calibMaxY)
        {
            Debug.LogWarning("⚠️ Kalibrierungswerte ungültig (MinY >= MaxY). Benutze Default-Werte.");
            calibMinY = defaultMinY;
            calibMaxY = defaultMaxY;

            PlayerPrefs.SetFloat("Level_MinY", calibMinY);
            PlayerPrefs.SetFloat("Level_MaxY", calibMaxY);
            PlayerPrefs.Save();
            return;
        }

        float range = calibMaxY - calibMinY;

        if (range <= 0f)
        {
            Debug.LogWarning("⚠️ Kalibrierungsbereich ungültig, benutze Standard-Spielbereich");
            return;
        }

        float levelFactor = 1f;

        switch (currentLevel)
        {
            case 1: levelFactor = 0.4f; break;
            case 2: levelFactor = 0.7f; break;
            case 3: levelFactor = 1.0f; break;
        }

        float centerY = (calibMinY + calibMaxY) / 2f;
        float halfRange = (range * levelFactor) / 2f;

        float levelMinY = centerY - halfRange;
        float levelMaxY = centerY + halfRange;

        PlayerPrefs.SetFloat("Level_MinY", levelMinY);
        PlayerPrefs.SetFloat("Level_MaxY", levelMaxY);

        Debug.Log($"Level Y-Range: {levelMinY} – {levelMaxY}");

        // X/Z weiterhin über areaSize
        switch (currentLevel)
        {
            case 1: spawner.areaSize = level1Area; break;
            case 2: spawner.areaSize = level2Area; break;
            case 3: spawner.areaSize = level3Area; break;
        }
    }*/

    // =========================
    // SCORE
    // =========================
    /// <summary>
    /// Erhöht den Punktestand um den angegebenen Wert.
    /// </summary>
    public void AddScore(int amount)
    {
        if (!gameActive) return;

        score += amount;
        UpdateScoreUI();
    }

    /// <summary>
    /// Aktualisiert die Anzeige des Punktestands.
    /// Im Sequenzmodus wird stattdessen das aktuelle Ziel angezeigt.
    /// </summary>
    public void UpdateScoreUI()
    {
        if (currentGameMode == GameManager.GameMode.Sequence)
        {
            scoreText.text = $"Next number: {expectedSequenceNumber}";
        }
        else
        {
            scoreText.text = $"Score: {score}";
        }
    }

    /// <summary>
    /// Gibt zurück, ob aktuell eine Spielrunde aktiv ist.
    /// </summary>
    public bool IsGameActive() => gameActive;

    // =========================
    // HIGHSCORE
    // =========================
    /// <summary>
    /// Speichert den aktuellen Punktestand in der Highscoreliste.
    /// Es werden maximal die fünf besten Ergebnisse gespeichert.
    /// </summary>
    void SaveHighscore(int newScore, string mode)
    {
        string prefix = mode;
        List<int> scores = new();
        List<string> dates = new();

        for (int i = 0; i < 5; i++)
        {
            int s = PlayerPrefs.GetInt($"{prefix}_Score_{i}", -1);
            string d = PlayerPrefs.GetString($"{prefix}_Date_{i}", "");

            if (s >= 0)
            {
                scores.Add(s);
                dates.Add(d);
            }
        }

        scores.Add(newScore);
        dates.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        var combined = new List<(int, string)>();
        for (int i = 0; i < scores.Count; i++)
            combined.Add((scores[i], dates[i]));

        combined.Sort((a, b) => b.Item1.CompareTo(a.Item1));
        combined = combined.GetRange(0, Mathf.Min(5, combined.Count));

        for (int i = 0; i < combined.Count; i++)
        {
            PlayerPrefs.SetInt($"{prefix}_Score_{i}", combined[i].Item1);
            PlayerPrefs.SetString($"{prefix}_Date_{i}", combined[i].Item2);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Definiert, welche Hand für die Interaktion zugelassen ist.
    /// </summary>
    public enum HandMode
    {
        BothHands,
        LeftHandOnly,
        RightHandOnly
    }

    public HandMode handMode = HandMode.BothHands;
    /// <summary>
    /// Erlaubt die Nutzung beider Hände.
    /// </summary>
    public void SetHandModeBoth()
    {
        handMode = HandMode.BothHands;
        Debug.Log("Hand mode set to BOTH");
        CloseAllSetupSubMenus();
    }

    /// <summary>
    /// Erlaubt ausschließlich die linke Hand.
    /// </summary>
    public void SetHandModeLeft()
    {
        handMode = HandMode.LeftHandOnly;
        Debug.Log("Hand mode set to LEFT only");
        CloseAllSetupSubMenus();
    }

    /// <summary>
    /// Erlaubt ausschließlich die rechte Hand.
    /// </summary>
    public void SetHandModeRight()
    {
        handMode = HandMode.RightHandOnly;
        Debug.Log("Hand mode set to RIGHT only");
        CloseAllSetupSubMenus();
    }

    /// <summary>
    /// Legt Rot als Zielfarbe für den Farbmodus fest.
    /// </summary>
    public void ColorRed()
    {
        highlightMaterial = Color.red;
        CloseAllSetupSubMenus();
    }

    /// <summary>
    /// Legt Blau als Zielfarbe für den Farbmodus fest.
    /// </summary>
    public void ColorBlue()
    {
        highlightMaterial = Color.blue;
        CloseAllSetupSubMenus();
    }

    /// <summary>
    /// Legt Grün als Zielfarbe für den Farbmodus fest.
    /// </summary>
    public void ColorGreen()
    {
        highlightMaterial = Color.green;
        CloseAllSetupSubMenus();
    }

}

