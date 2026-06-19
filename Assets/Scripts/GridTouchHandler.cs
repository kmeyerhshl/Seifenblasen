using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using static GridGame;

public class GridTouchHandler : MonoBehaviour, IMixedRealityTouchHandler
{
    // Referenz auf die Spiellogik des Grid-Spiels
    private GridGame gridGame;

    // Position der Bubble innerhalb des Grids
    private int gridX, gridY;

    [Header("Pop Feedback")]
    // Partikeleffekt beim Zerplatzen einer Bubble
    public GameObject popEffectPrefab;

    // Soundeffekt beim Zerplatzen
    public AudioClip popSound;

    // Audioquelle zur Wiedergabe des Sounds
    private AudioSource audioSource;

    /// <summary>
    /// Initialisiert den Touch-Handler mit der zugehörigen
    /// Spielinstanz und den Grid-Koordinaten.
    /// </summary>
    public void Initialize(GridGame game, int x, int y)
    {
        gridGame = game;
        gridX = x;
        gridY = y;
    }

    void Start()
    {
        // Audioquelle für Soundeffekte erstellen
        audioSource = gameObject.AddComponent<AudioSource>();

        // Sound nicht automatisch beim Start abspielen
        audioSource.playOnAwake = false;

        // Räumlicher 3D-Sound
        audioSource.spatialBlend = 1f;
    }

    /// <summary>
    /// Wird ausgelöst, sobald die Bubble mit der Hand berührt wird.
    /// </summary>
    public void OnTouchStarted(HandTrackingInputEventData eventData)
    {
        // Ermitteln, welche Hand die Bubble berührt hat
        var handness = eventData.Handedness;

        bool handAllowed = false;

        // Prüfen, ob die verwendete Hand im Spielmodus erlaubt ist
        switch (gridGame.handMode)
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

        // Ungültige Hand → Eingabe ignorieren
        if (!handAllowed)
            return;

        var renderer = GetComponent<Renderer>();

        // Nur die aktuell hervorgehobene Ziel-Bubble darf getroffen werden
        if (renderer.sharedMaterial == gridGame.highlightMaterial)
        {
            // Pop-Effekt auslösen
            PopBubble();

            // Treffer an die Spiellogik melden
            gridGame.BubbleHit(gridX, gridY);
        }
    }

    /// <summary>
    /// Spielt Pop-Effekt und Sound ab und blendet die Bubble aus.
    /// </summary>
    void PopBubble()
    {
        // Partikeleffekt erzeugen
        if (popEffectPrefab != null)
        {
            GameObject effect =
                Instantiate(
                    popEffectPrefab,
                    transform.position,
                    Quaternion.identity);

            Destroy(effect, 2f);
        }

        // Soundeffekt abspielen
        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }

        // Bubble vorübergehend deaktivieren
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Wird während einer Berührung kontinuierlich aufgerufen.
    /// Für dieses Spiel nicht benötigt.
    /// </summary>
    public void OnTouchUpdated(HandTrackingInputEventData eventData) { }

    /// <summary>
    /// Wird aufgerufen, wenn die Berührung endet.
    /// Für dieses Spiel nicht benötigt.
    /// </summary>
    public void OnTouchCompleted(HandTrackingInputEventData eventData) { }
}
