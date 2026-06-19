using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using TMPro;
using UnityEngine;

// Verwaltet das Erzeugen, Speichern und Bewegen der Blasen im Spiel.
/// Unterstützt verschiedene Spielmodi (Farbmodus, Sequenzmodus)
/// und berücksichtigt die während der Kalibrierung festgelegten Grenzen.
public class BubbleSpawner : MonoBehaviour
{
    // ------------------- INSPECTOR SETTINGS -------------------

    [Header("Bubble Settings")]

    // Prefab der zu erzeugenden Blase
    public GameObject bubblePrefab;

    // Maximale Anzahl gleichzeitig aktiver Blasen
    public int maxBubbles = 15;

    // Zeitabstand zwischen zwei Spawnvorgängen
    public float spawnInterval = 1.5f;

    [Header("World Area Settings")]

    // Mittelpunkt des Spawnbereichs
    public Vector3 center = new Vector3(0, 0, 2.0f);

    // Größe des Bereichs, in dem Blasen erscheinen dürfen
    public Vector3 areaSize = new Vector3(1.2f, 1.0f, 1.0f);

    // Aktuelle Grenzen des Spielbereichs
    public float minX;
    public float maxX;
    public float minY, maxY;
    public float minZ;
    public float maxZ;

    // Liste aller aktuell aktiven Blasen
    private List<BubbleData> bubbles = new List<BubbleData>();

    // Referenz auf die Spawn-Coroutine
    private Coroutine spawnRoutine;

    // Gibt an, ob aktuell Blasen erzeugt werden
    private bool spawningActive = false;

    // Referenz auf den zentralen Spielmanager
    public GameManager gameManager;

    // Laufende Nummerierung für den Sequenzmodus
    private int nextSequenceNumber = 1;

    // Anzahl aktuell vorhandener grüner Blasen
    private int greenBubbleCount = 0;

    /// <summary>
    /// Speichert alle relevanten Informationen einer einzelnen Blase.
    /// Wird für die spätere Bewegung und Verwaltung benötigt.
    /// </summary>
    public class BubbleData
    {
        // Referenz auf das Blasenobjekt
        public GameObject obj;

        // Ursprüngliche Spawnposition
        public Vector3 startPos;

        // Zufällige Phasenverschiebungen für die Sinusbewegung
        public float phaseX;
        public float phaseY;

        // Individuelle Bewegungsgeschwindigkeit
        public float speed;

        // Zeitpunkt der Erzeugung
        public float spawnTime;
    }

    /// <summary>
    /// Liefert alle aktuell aktiven Blasen zurück.
    /// </summary>
    public List<BubbleData> GetActiveBubbles()
    {
        return bubbles;
    }

    void Start()
    {
        // Kein automatischer Start.
        // Das Spawning wird vom GameTimer gesteuert.
    }

    // ------------------- CONTROL -------------------

    /// <summary>
    /// Startet das kontinuierliche Erzeugen neuer Blasen.
    /// </summary>
    public void StartSpawning()
    {
        Debug.Log("StartSpawning");

        // Im Sequenzmodus weniger Blasen erzeugen,
        // um die Übersichtlichkeit zu erhöhen.
        if (gameManager.currentGameMode == GameManager.GameMode.Sequence)
        {
            maxBubbles = 10;
        }
        else
        {
            maxBubbles = 15;
        }

        // Verhindert mehrfaches Starten der Coroutine
        if (spawnRoutine != null) return;

        spawningActive = true;
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    /// <summary>
    /// Stoppt die Erzeugung neuer Blasen.
    /// </summary>
    public void StopSpawning()
    {
        Debug.Log("StopSpawning");

        spawningActive = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    /// <summary>
    /// Entfernt alle vorhandenen Blasen
    /// und setzt interne Zähler zurück.
    /// </summary>
    public void ResetBubbles()
    {
        Debug.Log("ResetBubbles");

        StopSpawning();

        foreach (var b in bubbles)
        {
            if (b.obj != null)
                Destroy(b.obj);
        }

        bubbles.Clear();
        nextSequenceNumber = 1;
        greenBubbleCount = 0;
    }

    // ------------------- SPAWN LOOP -------------------

    /// <summary>
    /// Coroutine für das periodische Erzeugen neuer Blasen.
    /// </summary>
    IEnumerator SpawnLoop()
    {
        while (spawningActive)
        {
            // Bereits zerstörte Blasen entfernen
            bubbles.RemoveAll(b => b.obj == null);

            // Nur neue Blasen erzeugen,
            // wenn die maximale Anzahl noch nicht erreicht ist.
            if (bubbles.Count < maxBubbles)
            {
                SpawnBubble();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // ------------------- SPAWN -------------------

    /// <summary>
    /// Wird aufgerufen, wenn eine grüne Blase zerstört wurde.
    /// Reduziert den Zähler für aktive grüne Blasen.
    /// </summary>
    public void OnGreenBubblePopped()
    {
        greenBubbleCount = Mathf.Max(0, greenBubbleCount - 1);
    }

    /// <summary>
    /// Erzeugt eine neue Blase an einer zufälligen Position
    /// innerhalb des definierten Spielbereichs.
    /// </summary>
    void SpawnBubble()
    {
        // Prüfen, ob Kalibrierungsdaten vorhanden sind
        bool hasCalib =
            PlayerPrefs.HasKey("Calib_MinY") &&
            PlayerPrefs.HasKey("Calib_MaxY");

        // Höhenbereich aus Kalibrierung übernehmen
        if (hasCalib)
        {
            minY = PlayerPrefs.GetFloat(
                "Level_MinY",
                PlayerPrefs.GetFloat("Calib_MinY"));

            maxY = PlayerPrefs.GetFloat(
                "Level_MaxY",
                PlayerPrefs.GetFloat("Calib_MaxY"));
        }
        else
        {
            // Standardbereich verwenden
            minY = center.y - areaSize.y / 2f;
            maxY = center.y + areaSize.y / 2f;
        }

        // Zufällige Spawnposition berechnen
        Vector3 pos = new Vector3(
            Random.Range(center.x - areaSize.x / 2f,
                         center.x + areaSize.x / 2f),

            Random.Range(minY, maxY),

            Random.Range(center.z - areaSize.z / 2f,
                         center.z + areaSize.z / 2f)
        );

        // Grenzen des Spielbereichs speichern
        minX = center.x - areaSize.x / 2f;
        maxX = center.x + areaSize.x / 2f;
        minZ = center.z - areaSize.z / 2f;
        maxZ = center.z + areaSize.z / 2f;

        Debug.Log("Spawn Bubble: " + pos);

        // Neue Blase erzeugen
        GameObject bubble =
            Instantiate(bubblePrefab, pos, Quaternion.identity);

        // Physik deaktivieren, da die Bewegung manuell erfolgt
        Rigidbody rb = bubble.GetComponent<Rigidbody>();

        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        bool isGreen = false;
        int seqNum = 0;

        // ------------------- SPIELMODUS LOGIK -------------------

        if (gameManager.currentGameMode ==
            GameManager.GameMode.ColorOnly)
        {
            // Sicherstellen, dass immer mindestens
            // eine grüne Blase vorhanden ist
            if (greenBubbleCount == 0)
            {
                isGreen = true;
                greenBubbleCount++;
            }
            else
            {
                // 30 % Wahrscheinlichkeit für grüne Blasen
                isGreen = Random.value < 0.3f;

                if (isGreen)
                    greenBubbleCount++;
            }
        }
        else if (gameManager.currentGameMode ==
                 GameManager.GameMode.Sequence)
        {
            Debug.Log("Reihenfolge");

            // Nächste Sequenznummer vom GameManager beziehen
            seqNum = gameManager.GetNextSequenceNumber();
        }

        // ------------------- VISUELLE DARSTELLUNG -------------------

        Renderer rend = bubble.GetComponent<Renderer>();

        if (rend != null)
        {
            if (gameManager.currentGameMode ==
                GameManager.GameMode.ColorOnly)
            {
                rend.material.color =
                    isGreen ?
                    gameManager.highlightMaterial :
                    Color.white;
            }
            else
            {
                rend.material.color = Color.white;
            }
        }

        // Touch-Handler hinzufügen oder referenzieren
        var handler = bubble.GetComponent<BubbleTouchHandler>();

        if (!handler)
            handler = bubble.AddComponent<BubbleTouchHandler>();

        // Referenz auf das Textfeld für die Sequenznummer
        var label = bubble.transform.Find("SequenceLabel");

        if (label != null)
        {
            handler.sequenceText =
                label.GetComponent<TextMeshPro>();
        }

        // Blase beim GameManager registrieren
        int id =
            gameManager.RegisterBubble(
                Time.time,
                pos);

        // Initialisierung der Blase
        handler.Init(
            gameManager,
            isGreen,
            id,
            seqNum,
            Time.time);

        // Bewegungsdaten speichern
        bubbles.Add(new BubbleData
        {
            obj = bubble,
            startPos = bubble.transform.position,
            phaseX = Random.value * Mathf.PI * 2f,
            phaseY = Random.value * Mathf.PI * 2f,
            speed = Random.Range(0.3f, 0.6f),
            spawnTime = Time.time
        });
    }

    // ------------------- MOVEMENT -------------------

    /// <summary>
    /// Aktualisiert in jedem Frame die Position aller Blasen.
    /// Die Bewegung erfolgt über Sinusfunktionen,
    /// wodurch ein schwebender Effekt entsteht.
    /// </summary>
    void Update()
    {
        // Höhenbereich bestimmen
        bool hasLevelRange =
            PlayerPrefs.HasKey("Level_MinY") &&
            PlayerPrefs.HasKey("Level_MaxY");

        bool hasCalib =
            PlayerPrefs.HasKey("Calib_MinY") &&
            PlayerPrefs.HasKey("Calib_MaxY");

        float minY;
        float maxY;

        if (hasLevelRange)
        {
            minY = PlayerPrefs.GetFloat("Level_MinY");
            maxY = PlayerPrefs.GetFloat("Level_MaxY");
        }
        else if (hasCalib)
        {
            minY = PlayerPrefs.GetFloat("Calib_MinY");
            maxY = PlayerPrefs.GetFloat("Calib_MaxY");
        }
        else
        {
            minY = center.y - areaSize.y / 2f;
            maxY = center.y + areaSize.y / 2f;
        }

        foreach (var b in bubbles)
        {
            if (b.obj == null)
                continue;

            // Schwebende Bewegung durch Sinusfunktionen
            Vector3 offset = new Vector3(
                Mathf.Sin(Time.time * b.speed + b.phaseX) * 0.05f,
                Mathf.Sin(Time.time * b.speed * 1.3f + b.phaseY) * 0.05f,
                0
            );

            Vector3 newPos = b.startPos + offset;

            // Position innerhalb des Spielbereichs begrenzen
            newPos.x = Mathf.Clamp(
                newPos.x,
                center.x - areaSize.x / 2f,
                center.x + areaSize.x / 2f);

            newPos.y = Mathf.Clamp(
                newPos.y,
                minY,
                maxY);

            newPos.z = Mathf.Clamp(
                newPos.z,
                center.z - areaSize.z / 2f,
                center.z + areaSize.z / 2f);

            // Sicherheitsprüfung gegen ungültige Werte
            if (float.IsNaN(newPos.y))
            {
                Debug.LogError(
                    "NaN detected in newPos.y! Resetting to center.y");

                newPos.y = center.y;
            }

            // Neue Position anwenden
            b.obj.transform.position = newPos;
        }
    }
}