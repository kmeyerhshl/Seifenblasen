using UnityEngine;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

/// <summary>
/// Erfasst und speichert Eye-Tracking-Daten über MRTK.
/// Während eines aktiven Spiels werden Blickrichtung, Blickpunkt,
/// Kopfausrichtung sowie Treffer auf Spielobjekte aufgezeichnet.
/// Die Daten dienen später der Analyse des Blickverhaltens
/// und der Berechnung von Eye-Reaktionszeiten.
/// </summary>
public class EyeTracking : MonoBehaviour
{
    // Aktiviert bzw. deaktiviert die Aufzeichnung
    public bool isTracking = false;

    // ------------------- RAYCAST SETTINGS -------------------

    [Header("Raycast Settings")]

    // Maximale Reichweite des Blickstrahls
    public float maxDistance = 5f;

    // Layer-Maske für die Blickerkennung
    public LayerMask gazeLayerMask = ~0;

    // ------------------- REFERENZEN -------------------

    [Header("Optional References")]

    // Referenz auf den zentralen Spielmanager
    public GameManager gameManager;

    // Referenz auf den Spieltimer
    public GameTimer gameTimer;

    // Liste aller aufgezeichneten Blickdaten
    private List<GazeSample> gazeSamples = new List<GazeSample>();

    /// <summary>
    /// Wird in jedem Frame aufgerufen.
    /// Liest die aktuellen Eye-Tracking-Daten aus,
    /// führt einen Blickstrahl-Raycast durch und speichert
    /// die gewonnenen Informationen.
    /// </summary>
    void Update()
    {
        // Nur aufzeichnen, wenn Tracking aktiviert wurde
        if (!isTracking)
            return;

        // MRTK Eye Gaze Provider abrufen
        var eyeProvider = CoreServices.InputSystem?.EyeGazeProvider;

        if (eyeProvider == null)
            return;

        // ------------------- BLICKDATEN AUSLESEN -------------------

        // Ursprung des Blickstrahls (Augenposition)
        Vector3 origin = eyeProvider.GazeOrigin;

        // Blickrichtung
        Vector3 direction = eyeProvider.GazeDirection;

        // Blickrichtung des Kopfes
        Vector3 headForward = Camera.main.transform.forward;

        // Winkel zwischen Kopf- und Augenrichtung
        float eyeHeadAngle =
            Vector3.Angle(headForward, direction);

        // Ray für die Blickerkennung erzeugen
        Ray ray = new Ray(origin, direction);

        Vector3 hitPoint;

        // Informationen über mögliche Blasentreffer
        int bubbleId = -1;
        bool hitBubble = false;

        // ------------------- RAYCAST -------------------

        bool hitSomething =
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxDistance);

        if (hitSomething)
        {
            // Treffpunkt speichern
            hitPoint = hit.point;

            // Prüfen, ob eine Blase getroffen wurde
            var bubble =
                hit.collider.GetComponent<BubbleTouchHandler>();

            Debug.Log("Hit: " + hit.collider.name);

            // ------------------- BLICK AUF BLASE -------------------

            if (bubble != null && !bubble.hasBeenLookedAt)
            {
                hitBubble = true;
                bubbleId = bubble.bubbleId;

                // Blase als bereits angesehen markieren
                bubble.hasBeenLookedAt = true;

                // Zeit vom Erscheinen bis zum ersten Blickkontakt berechnen
                bubble.firstLookTime =
                    Time.time - bubble.spawnTime;

                // Eye-Reaktionszeit protokollieren
                gameManager.LogEyeReactionTime(
                    bubble.bubbleId,
                    bubble.firstLookTime);
            }
        }
        else
        {
            // Falls kein Objekt getroffen wird,
            // wird ein Punkt entlang der Blickrichtung verwendet
            hitPoint =
                origin +
                direction * maxDistance;
        }

        // ------------------- DATENSATZ SPEICHERN -------------------

        gazeSamples.Add(new GazeSample
        {
            // Spielzeitpunkt der Messung
            time = gameTimer.currentTime,

            // Ursprung des Blickstrahls
            origin = origin,

            // Blickrichtung
            direction = direction,

            // Kopfausrichtung
            headForward = headForward,

            // Winkel zwischen Augen- und Kopfbewegung
            eyeHeadAngle = eyeHeadAngle,

            // Treffpunkt des Blickstrahls
            hitPoint = hitPoint,

            // Kennzeichnet Treffer auf eine Blase
            hitBubble = hitBubble,

            // ID der betrachteten Blase
            bubbleId = bubbleId
        });
    }

    /// <summary>
    /// Liefert alle bisher aufgezeichneten Blickdaten zurück.
    /// </summary>
    public List<GazeSample> GetData()
    {
        return gazeSamples;
    }

    /// <summary>
    /// Löscht alle gespeicherten Blickdaten.
    /// </summary>
    public void ClearData()
    {
        gazeSamples.Clear();
    }

    /// <summary>
    /// Setzt die Eye-Tracking-Daten zurück.
    /// Funktional identisch zu ClearData().
    /// </summary>
    public void ResetData()
    {
        gazeSamples.Clear();
    }

    // ------------------- DATENSTRUKTUR -------------------

    /// <summary>
    /// Repräsentiert einen einzelnen Eye-Tracking-Datensatz.
    /// Jeder Datensatz entspricht einer Messung in einem Frame.
    /// </summary>
    public class GazeSample
    {
        // Zeitpunkt der Messung
        public float time;

        // Ursprung des Blickstrahls
        public Vector3 origin;

        // Blickrichtung
        public Vector3 direction;

        // Blickrichtung des Kopfes
        public Vector3 headForward;

        // Winkel zwischen Kopf- und Blickrichtung
        public float eyeHeadAngle;

        // Treffpunkt des Blickstrahls
        public Vector3 hitPoint;

        // Gibt an, ob eine Blase getroffen wurde
        public bool hitBubble;

        // ID der betrachteten Blase
        public int bubbleId;
    }
}