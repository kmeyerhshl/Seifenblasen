using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using TMPro;
using UnityEngine;

public class BubbleSpawner : MonoBehaviour
{
    [Header("Bubble Settings")]
    public GameObject bubblePrefab;
    public int maxBubbles = 15;
    public float spawnInterval = 1.5f;

    [Header("World Area Settings")]
    public Vector3 center = new Vector3(0, 0, 2.0f);
    public Vector3 areaSize = new Vector3(1.2f, 1.0f, 1.0f);
    public float minX;
    public float maxX;
    public float minY, maxY;
    public float minZ;
    public float maxZ;

    private List<BubbleData> bubbles = new List<BubbleData>();
    private Coroutine spawnRoutine;
    private bool spawningActive = false;

    public GameManager gameManager;
    private int nextSequenceNumber = 1;
    private int greenBubbleCount = 0;

    public class BubbleData
    {
        public GameObject obj;
        public Vector3 startPos;
        public float phaseX;
        public float phaseY;
        public float speed;
        public float spawnTime;
    }

    public List<BubbleData> GetActiveBubbles()
    {
        return bubbles;
    }

    void Start()
    {
        // Starte NICHT automatisch. Das macht GameTimer.
    }

    // ------------------- CONTROL -------------------

    public void StartSpawning()
    {
        Debug.Log("StartSpawning");
        if (gameManager.currentGameMode == GameManager.GameMode.Sequence)
        {
            maxBubbles = 10;
        }
        else
        {
            maxBubbles = 15; // oder deine Standardzahl
        }
        if (spawnRoutine != null) return; // läuft schon

        spawningActive = true;
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

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

    IEnumerator SpawnLoop()
    {
        while (spawningActive)
        {
            // Entferne bereits zerstörte Bubbles
            bubbles.RemoveAll(b => b.obj == null);

            if (bubbles.Count < maxBubbles)
            {
                SpawnBubble();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // ------------------- SPAWN -------------------

    public void OnGreenBubblePopped()
    {
        greenBubbleCount = Mathf.Max(0, greenBubbleCount - 1);
    }

    void SpawnBubble()
    {

        bool hasCalib = PlayerPrefs.HasKey("Calib_MinY") && PlayerPrefs.HasKey("Calib_MaxY");



        if (hasCalib)
        {
            minY = PlayerPrefs.GetFloat("Level_MinY",
                PlayerPrefs.GetFloat("Calib_MinY"));

            maxY = PlayerPrefs.GetFloat("Level_MaxY",
                PlayerPrefs.GetFloat("Calib_MaxY"));
        }
        else
        {
            minY = center.y - areaSize.y / 2f;
            maxY = center.y + areaSize.y / 2f;
        }

        //float minX = PlayerPrefs.GetFloat("Calib_MinX");
        //float maxX = PlayerPrefs.GetFloat("Calib_MaxX");
        //float minY = PlayerPrefs.GetFloat("Level_MinY", PlayerPrefs.GetFloat("Calib_MinY"));
        //float maxY = PlayerPrefs.GetFloat("Level_MaxY", PlayerPrefs.GetFloat("Calib_MaxY"));

        Vector3 pos = new Vector3(
            Random.Range(center.x - areaSize.x / 2f, center.x + areaSize.x / 2f),
            Random.Range(minY, maxY),
            Random.Range(center.z - areaSize.z / 2f, center.z + areaSize.z / 2f)
        );
        minX = center.x - areaSize.x / 2f;
        maxX = center.x + areaSize.x / 2f;
        minZ = center.z - areaSize.z / 2f;
        maxZ = center.z + areaSize.z / 2f;
        Debug.Log("Spawn Bubble: " + pos);
        Debug.Log("Spawn Bereich X: " + (center.x - areaSize.x / 2f) + " bis " + (center.x + areaSize.x / 2f));
        /*Vector3 pos = new Vector3(
            Random.Range(center.x - areaSize.x / 2f, center.x + areaSize.x / 2f),
            Random.Range(center.y - areaSize.y / 2f, center.y + areaSize.y / 2f),
            Random.Range(center.z - areaSize.z / 2f, center.z + areaSize.z / 2f)
        );*/

        GameObject bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);

        Rigidbody rb = bubble.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        bool isGreen = false;
        int seqNum = 0;

        if (gameManager.currentGameMode == GameManager.GameMode.ColorOnly)
        {
            //isGreen = Random.value < 0.3f; // 30% grün z.B.
            /*if (greenBubbleCount < 3)
            {
                isGreen = true;
                greenBubbleCount++;
            }*/
            if (greenBubbleCount == 0)
            {
                isGreen = true;
                greenBubbleCount++;
            }
            else
            {
                isGreen = Random.value < 0.3f;
                if (isGreen) greenBubbleCount++;
            }
        }
        else if (gameManager.currentGameMode == GameManager.GameMode.Sequence)
        {
            Debug.Log("Reihenfolge");
            isGreen = false;
            //seqNum = nextSequenceNumber;
            //nextSequenceNumber++;
            seqNum = gameManager.GetNextSequenceNumber();
        }

        // Farbe setzen (z.B. Material ändern), hier beispielhaft:
        Renderer rend = bubble.GetComponent<Renderer>();
        if (rend != null)
        {
            //rend.material.color = isGreen ? gameManager.highlightMaterial : Color.white;
            if (gameManager.currentGameMode == GameManager.GameMode.ColorOnly)
                rend.material.color = isGreen ? gameManager.highlightMaterial : Color.white;
            else
                rend.material.color = Color.white;
        }

        var handler = bubble.GetComponent<BubbleTouchHandler>();
        if (!handler)
            handler = bubble.AddComponent<BubbleTouchHandler>();

        var label = bubble.transform.Find("SequenceLabel");
        if (label != null)
        {
            handler.sequenceText = label.GetComponent<TextMeshPro>();
        }

        int id = gameManager.RegisterBubble(Time.time, pos);
        handler.Init(gameManager, isGreen, id, seqNum, Time.time);

        /*if (gameManager.currentGameMode == GameManager.GameMode.Sequence)
        {
            handler.UpdateSequenceVisual();
        }*/

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

    void Update()
    {
        bool hasLevelRange = PlayerPrefs.HasKey("Level_MinY") && PlayerPrefs.HasKey("Level_MaxY");
        bool hasCalib = PlayerPrefs.HasKey("Calib_MinY") && PlayerPrefs.HasKey("Calib_MaxY");

        float minY, maxY;

        if (hasLevelRange)
        {
            minY = PlayerPrefs.GetFloat("Level_MinY");
            maxY = PlayerPrefs.GetFloat("Level_MaxY");
            //Debug.Log("hasLevelRange");
        }
        else if (hasCalib)
        {
            minY = PlayerPrefs.GetFloat("Calib_MinY");
            maxY = PlayerPrefs.GetFloat("Calib_MaxY");
            Debug.Log("hasCalib");
        }
        else
        {
            minY = center.y - areaSize.y / 2f;
            maxY = center.y + areaSize.y / 2f;
            Debug.Log("minY: " + minY + ", maxY: " + maxY);
        }

        foreach (var b in bubbles)
        {
            if (b.obj == null) continue;

            Vector3 offset = new Vector3(
                Mathf.Sin(Time.time * b.speed + b.phaseX) * 0.05f,
                Mathf.Sin(Time.time * b.speed * 1.3f + b.phaseY) * 0.05f,
                0
            );

            Vector3 newPos = b.startPos + offset;

            newPos.x = Mathf.Clamp(newPos.x, center.x - areaSize.x / 2f, center.x + areaSize.x / 2f);
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
            newPos.z = Mathf.Clamp(newPos.z, center.z - areaSize.z / 2f, center.z + areaSize.z / 2f);

            // Defensive check: falls newPos.y trotzdem NaN sein sollte
            if (float.IsNaN(newPos.y))
            {
                Debug.LogError($"NaN detected in newPos.y! Resetting to center.y");
                newPos.y = center.y;
            }

            b.obj.transform.position = newPos;
        }
    }
}
