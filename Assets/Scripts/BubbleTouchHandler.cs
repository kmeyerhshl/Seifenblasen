using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using static GameManager;
using TMPro;
using System.Linq;

/// <summary>
/// Verwaltet die Interaktion einer einzelnen Blase.
/// Erkennt Berührungen über MRTK-Handtracking, überprüft die Spielregeln
/// des aktuellen Spielmodus und verarbeitet das Platzen der Blase.
/// </summary>
public class BubbleTouchHandler : MonoBehaviour, IMixedRealityTouchHandler
{
    // ------------------- FEEDBACK SETTINGS -------------------

    [Header("Pop Feedback")]

    // Partikeleffekt, der beim Platzen angezeigt wird
    public GameObject popEffectPrefab;

    // Soundeffekt beim Platzen
    public AudioClip popSound;

    // Verzögerung bis zur tatsächlichen Zerstörung der Blase
    public float destroyDelay = 0.05f;

    // Audioquelle zur Wiedergabe des Sounds
    private AudioSource audioSource;

    // Referenz auf den zentralen Spielmanager
    private GameManager gameManager;

    // Kennzeichnet grüne Zielblasen im Farbmodus
    private bool isGreenBubble;

    // Reihenfolgenummer im Sequenzmodus
    private int sequenceNumber;

    // Textanzeige für die Sequenznummer
    [SerializeField] public TextMeshPro sequenceText;

    // Zeit bis zur automatischen Zerstörung nicht relevanter Blasen
    [SerializeField] private float autoDestroyTime = 8f;

    // Zeitpunkt der Erzeugung
    public float spawnTime;

    // Speichert, ob die Blase bereits vom Eye Tracking erfasst wurde
    public bool hasBeenLookedAt = false;

    // Zeitpunkt des ersten Blickkontakts
    public float firstLookTime = -1f;

    // Eindeutige ID der Blase für Logging und Auswertung
    public int bubbleId;

    /// <summary>
    /// Initialisiert die Blase nach dem Erzeugen.
    /// Übergibt Spielmodus-relevante Informationen wie Farbe,
    /// ID, Sequenznummer und Spawnzeit.
    /// </summary>
    public void Init(GameManager gm, bool isGreen, int id,
                     int seqNum = 0, float spawnT = 0f)
    {
        gameManager = gm;
        isGreenBubble = isGreen;
        sequenceNumber = seqNum;
        spawnTime = spawnT;
        bubbleId = id;

        // Falls kein Textfeld gesetzt wurde,
        // automatisch eines in den Kindobjekten suchen
        if (sequenceText == null)
        {
            sequenceText = GetComponentInChildren<TextMeshPro>(true);
        }

        // Sequenznummer nur im Sequenzmodus anzeigen
        if (sequenceText != null)
        {
            bool showNumber =
                gameManager.currentGameMode ==
                GameManager.GameMode.Sequence &&
                sequenceNumber > 0;

            sequenceText.gameObject.SetActive(showNumber);

            if (showNumber)
                sequenceText.text = sequenceNumber.ToString();
        }
    }

    /// <summary>
    /// Initialisiert die Audioquelle und startet ggf.
    /// die automatische Zerstörung bestimmter Blasen.
    /// </summary>
    void Start()
    {
        // Audioquelle dynamisch hinzufügen
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // 3D-Sound für räumliche Audiowiedergabe
        audioSource.spatialBlend = 1f;

        // Im Farbmodus sollen falsche (weiße) Blasen
        // nach einer bestimmten Zeit verschwinden
        if (gameManager.currentGameMode ==
            GameManager.GameMode.ColorOnly &&
            !isGreenBubble)
        {
            Destroy(gameObject, autoDestroyTime);
        }
    }

    /// <summary>
    /// Setzt nachträglich die Referenz auf den GameManager.
    /// </summary>
    public void SetGameManager(GameManager gm)
    {
        gameManager = gm;
    }

    // ------------------- TOUCH INPUT -------------------

    /// <summary>
    /// Wird von MRTK aufgerufen, sobald eine Blase berührt wird.
    /// Prüft zunächst, ob die aktuell verwendete Hand
    /// gemäß der Spieleinstellungen erlaubt ist.
    /// </summary>
    public void OnTouchStarted(HandTrackingInputEventData eventData)
    {
        var handness = eventData.Handedness;

        bool handAllowed = false;

        switch (gameManager.handMode)
        {
            case HandMode.BothHands:
                handAllowed = true;
                break;

            case HandMode.LeftHandOnly:
                handAllowed =
                    handness ==
                    Microsoft.MixedReality.Toolkit.Utilities.Handedness.Left;
                break;

            case HandMode.RightHandOnly:
                handAllowed =
                    handness ==
                    Microsoft.MixedReality.Toolkit.Utilities.Handedness.Right;
                break;
        }

        // Berührung ignorieren, falls die falsche Hand verwendet wurde
        if (!handAllowed)
            return;

        // Blase verarbeiten
        PopBubble();
    }

    public void OnPointerDragged(HandTrackingInputEventData eventData) { }
    public void OnPointerUp(HandTrackingInputEventData eventData) { }
    public void OnPointerClicked(HandTrackingInputEventData eventData) { }

    // ------------------- GAME MODE LOGIC -------------------

    /// <summary>
    /// Prüft die Regeln des aktuellen Spielmodus
    /// und entscheidet, ob die Blase zerstört werden darf.
    /// </summary>
    void PopBubble()
    {
        // Keine Interaktion außerhalb eines aktiven Spiels
        if (gameManager == null || !gameManager.IsGameActive())
            return;

        switch (gameManager.currentGameMode)
        {
            // Standardmodus:
            // Jede Blase darf geplatzt werden
            case GameManager.GameMode.Standard:
                PopNormally();
                break;

            // Farbmodus:
            // Nur grüne Blasen zählen
            case GameManager.GameMode.ColorOnly:

                if (!isGreenBubble)
                {
                    Debug.Log("❌ Falsche Farbe!");
                    return;
                }

                PopNormally();
                break;

            // Sequenzmodus:
            // Blasen müssen in der richtigen Reihenfolge
            // ausgewählt werden
            case GameManager.GameMode.Sequence:

                if (sequenceNumber !=
                    gameManager.expectedSequenceNumber)
                {
                    Debug.Log(
                        $"❌ Falsche Reihenfolge! Erwartet: {gameManager.expectedSequenceNumber}");

                    return;
                }

                gameManager.RemoveSequenceNumber(sequenceNumber);

                PopNormally();
                break;
        }
    }

    // ------------------- POP LOGIC -------------------

    /// <summary>
    /// Führt das eigentliche Platzen der Blase aus:
    /// Effekte, Sound, Punktvergabe und Logging.
    /// </summary>
    void PopNormally()
    {
        // Reaktionszeit berechnen
        float reactionTime = Time.time - spawnTime;

        Debug.Log("Reaction time: " + reactionTime);

        // ------------------- VISUELLES FEEDBACK -------------------

        if (popEffectPrefab != null)
        {
            GameObject effect =
                Instantiate(
                    popEffectPrefab,
                    transform.position,
                    Quaternion.identity);

            Destroy(effect, 2f);
        }

        // Anzahl grüner Blasen aktualisieren
        if (gameManager.currentGameMode ==
            GameManager.GameMode.ColorOnly &&
            isGreenBubble)
        {
            gameManager.spawner.OnGreenBubblePopped();
        }

        // ------------------- AUDIO FEEDBACK -------------------

        if (popSound != null && audioSource != null)
            audioSource.PlayOneShot(popSound);

        // ------------------- SPIELSTATISTIKEN -------------------

        // Punktestand erhöhen
        gameManager.AddScore(1);

        // Hand-Reaktionszeit speichern
        gameManager.LogTouchReactionTime(
            bubbleId,
            reactionTime);

        // Falls die Blase vorher nicht angesehen wurde,
        // wird dieselbe Zeit als Eye-Reaktionszeit protokolliert
        if (!gameManager.bubbleLogs[bubbleId].wasSeen)
        {
            gameManager.LogEyeReactionTime(
                bubbleId,
                reactionTime);
        }

        // Blase entfernen
        Destroy(gameObject, destroyDelay);
    }

    /// <summary>
    /// Kapselt die Wiedergabe von Sound und Partikeleffekten.
    /// Aktuell nicht verwendet, kann aber für spätere Erweiterungen
    /// genutzt werden.
    /// </summary>
    private void PlayPopEffects()
    {
        if (popEffectPrefab != null)
        {
            GameObject effect =
                Instantiate(
                    popEffectPrefab,
                    transform.position,
                    Quaternion.identity);

            Destroy(effect, 2f);
        }

        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }
    }

    // ------------------- MRTK INTERFACE -------------------

    public void OnTouchUpdated(HandTrackingInputEventData eventData) { }

    public void OnTouchCompleted(HandTrackingInputEventData eventData) { }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }
}