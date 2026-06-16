using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using static GameManager;
using TMPro;
using System.Linq;

public class BubbleTouchHandler : MonoBehaviour, IMixedRealityTouchHandler
{
    [Header("Pop Feedback")]
    public GameObject popEffectPrefab;
    public AudioClip popSound;
    public float destroyDelay = 0.05f;

    private AudioSource audioSource;
    private GameManager gameManager;
    private bool isGreenBubble;
    private int sequenceNumber;
    [SerializeField] public TextMeshPro sequenceText;
    [SerializeField] private float autoDestroyTime = 8f;
    public float spawnTime;
    public bool hasBeenLookedAt = false;
    public float firstLookTime = -1f;
    public int bubbleId;

    public void Init(GameManager gm, bool isGreen, int id, int seqNum = 0, float spawnT = 0f)
    {
        gameManager = gm;
        isGreenBubble = isGreen;
        sequenceNumber = seqNum;
        spawnTime = spawnT;
        bubbleId = id;

        if (sequenceText == null)
        {
            sequenceText = GetComponentInChildren<TextMeshPro>(true);
        }

        if (sequenceText != null)
        {
            bool showNumber =
                gameManager.currentGameMode == GameManager.GameMode.Sequence &&
                sequenceNumber > 0;

            sequenceText.gameObject.SetActive(showNumber);

            if (showNumber)
                sequenceText.text = sequenceNumber.ToString();
        }
    }

    void Start()
    {
        // AudioSource vorbereiten
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D-Sound

        if (gameManager.currentGameMode == GameManager.GameMode.ColorOnly && !isGreenBubble)
        {
            Destroy(gameObject, autoDestroyTime);
        }

        //gameManager = FindFirstObjectByType<GameManager>();
    }
    public void SetGameManager(GameManager gm)
    {
        gameManager = gm;
    }

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
                handAllowed = handness == Microsoft.MixedReality.Toolkit.Utilities.Handedness.Left;
                break;
            case HandMode.RightHandOnly:
                handAllowed = handness == Microsoft.MixedReality.Toolkit.Utilities.Handedness.Right;
                break;
        }

        if (!handAllowed)
            return;

        // Bubble poppen!
        PopBubble();
    }

    public void OnPointerDragged(HandTrackingInputEventData eventData) { }
    public void OnPointerUp(HandTrackingInputEventData eventData) { }
    public void OnPointerClicked(HandTrackingInputEventData eventData) { }

    /*void PopBubble()
    {
        // 1️⃣ Partikel-Effekt
        if (popEffectPrefab != null)
        {
            GameObject effect = Instantiate(popEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 2️⃣ Sound
        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }

        if (gameManager != null && gameManager.IsGameActive())
        {
            gameManager.AddScore(1);
        }

        // 3️⃣ Bubble zerstören
        Destroy(gameObject, destroyDelay);
    }*/
    void PopBubble()
    {

        if (gameManager == null || !gameManager.IsGameActive())
            return;

        switch (gameManager.currentGameMode)
        {
            case GameManager.GameMode.Standard:
                PopNormally();
                break;

            case GameManager.GameMode.ColorOnly:
                if (!isGreenBubble)
                {
                    Debug.Log("❌ Falsche Farbe!");
                    return;
                }
                PopNormally();
                break;

            case GameManager.GameMode.Sequence:
                if (sequenceNumber != gameManager.expectedSequenceNumber)
                {
                    Debug.Log($"❌ Falsche Reihenfolge! Erwartet: {gameManager.expectedSequenceNumber}");
                    return;
                }
                gameManager.RemoveSequenceNumber(sequenceNumber);
                //gameManager.expectedSequenceNumber++;
                //gameManager.UpdateScoreUI();
                //FindObjectsOfType<BubbleTouchHandler>().ToList().ForEach(b => b.UpdateSequenceVisual());
                PopNormally();
                break;
        }

    }


    void PopNormally()
    {
        float reactionTime = Time.time - spawnTime;
        Debug.Log("Reaction time: " + reactionTime);
        // Effekt
        if (popEffectPrefab != null)
        {
            GameObject effect = Instantiate(popEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (gameManager.currentGameMode == GameManager.GameMode.ColorOnly && isGreenBubble)
        {
            gameManager.spawner.OnGreenBubblePopped();
        }

        // Sound
        if (popSound != null && audioSource != null)
            audioSource.PlayOneShot(popSound);

        gameManager.AddScore(1);
        gameManager.LogTouchReactionTime(bubbleId, reactionTime);
        if (!gameManager.bubbleLogs[bubbleId].wasSeen)
        {
            gameManager.LogEyeReactionTime(bubbleId, reactionTime);
        }
        Destroy(gameObject, destroyDelay);
    }


    private void PlayPopEffects()
    {
        if (popEffectPrefab != null)
        {
            GameObject effect = Instantiate(popEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }
    }

    public void OnTouchUpdated(HandTrackingInputEventData eventData) { }
    public void OnTouchCompleted(HandTrackingInputEventData eventData) { }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }

    /*public void UpdateSequenceVisual()
   {
       if (gameManager.currentGameMode != GameManager.GameMode.Sequence)
           return;

       bool isActive = sequenceNumber == gameManager.expectedSequenceNumber;

       // Text
       if (sequenceText != null)
           sequenceText.color = isActive ? Color.green : Color.gray;

       // Optional: Bubble-Farbe
       Renderer rend = GetComponent<Renderer>();
       if (rend != null)
           rend.material.color = isActive ? Color.green : Color.white;
   }*/


}
