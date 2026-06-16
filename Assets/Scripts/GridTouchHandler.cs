using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using static GridGame;

public class GridTouchHandler : MonoBehaviour, IMixedRealityTouchHandler
{
    private GridGame gridGame;
    private int gridX, gridY;

    [Header("Pop Feedback")]
    public GameObject popEffectPrefab;
    public AudioClip popSound;

    private AudioSource audioSource;

    public void Initialize(GridGame game, int x, int y)
    {
        gridGame = game;
        gridX = x;
        gridY = y;
    }

    void Start()
    {
        // AudioSource vorbereiten
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D-Sound

        //gameManager = FindFirstObjectByType<GameManager>();
    }

    public void OnTouchStarted(HandTrackingInputEventData eventData)
    {
        // Hand prüfen (beispielhaft mit gameManager.handMode)
        var handness = eventData.Handedness;

        bool handAllowed = false;
        switch (gridGame.handMode)
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

        var renderer = GetComponent<Renderer>();
        // Nur wenn grün (highlightMaterial) -> zerplatzen
        if (renderer.sharedMaterial == gridGame.highlightMaterial)
        {
            // Bubble poppen
            PopBubble();
            gridGame.BubbleHit(gridX, gridY);
        }
    }

    void PopBubble()
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

        gameObject.SetActive(false);
    }

    public void OnTouchUpdated(HandTrackingInputEventData eventData) { }
    public void OnTouchCompleted(HandTrackingInputEventData eventData) { }
}
