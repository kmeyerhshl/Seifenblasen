using UnityEngine;

public class StartButton : MonoBehaviour
{
    // Referenzen auf die zentralen Spielkomponenten
    public GameTimer gameTimer;
    public GameManager gameManager;
    public EyeTracking eyeTracker;

    // Zeit, die der Button berührt werden muss,
    // bevor das Spiel tatsächlich startet
    public float holdTime = 1.0f;

    // Aktuelle Berührungsdauer
    private float timer = 0f;

    // Gibt an, ob der Button aktuell berührt wird
    private bool isTouching = false;

    // Verhindert mehrfaches Auslösen während derselben Berührung
    private bool triggered = false;

    /// <summary>
    /// Wird aufgerufen, sobald der Button berührt wird.
    /// Startet die Zeitmessung für den Hold-Mechanismus.
    /// </summary>
    public void OnTouchStarted()
    {
        isTouching = true;
        timer = 0f;
        triggered = false;

        Debug.Log("OnTouchStarted");
    }

    /// <summary>
    /// Wird aufgerufen, wenn die Berührung endet.
    /// Setzt den Timer zurück.
    /// </summary>
    public void OnTouchCompleted()
    {
        isTouching = false;
        timer = 0f;

        Debug.Log("OnTouchCompleted");
    }

    /// <summary>
    /// Prüft während der Berührung,
    /// ob die erforderliche Haltezeit erreicht wurde.
    /// </summary>
    void Update()
    {
        // Nur weitermachen, wenn aktuell berührt wird
        // und der Button noch nicht ausgelöst wurde
        if (!isTouching || triggered)
            return;

        // Berührungsdauer erhöhen
        timer += Time.deltaTime;

        // Haltezeit erreicht → Spiel starten
        if (timer >= holdTime)
        {
            triggered = true;
            isTouching = false;

            // Countdown und Spielstart auslösen
            gameTimer.StartCountdown();

            // Vorherige Eye-Tracking-Daten löschen
            eyeTracker.ClearData();

            // Alte Blickdaten entfernen
            gameManager.gazeLogs.Clear();

            // Alte Bubble-Reaktionsdaten entfernen
            gameManager.bubbleLogs.Clear();
        }
    }
}
