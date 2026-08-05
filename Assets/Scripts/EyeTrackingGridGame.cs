using UnityEngine;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

public class EyeTrackingGridGame : MonoBehaviour
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
    public GridGameUIManager uiManager;

    // Liste aller aufgezeichneten Blickdaten
    private List<GridGazeSample> gazeSamples = new List<GridGazeSample>();

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
        float eyeHeadAngle = Vector3.Angle(headForward, direction);

        // Ray für die Blickerkennung erzeugen
        Ray ray = new Ray(origin, direction);

        Vector3 hitPoint;

        // Informationen über mögliche Blasentreffer
        int bubbleId = -1;
        //bool hitBubble = false;

        // ------------------- RAYCAST -------------------

        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxDistance);
        GameObject hitObject = null;

        if (hitSomething)
        {
            // Treffpunkt speichern
            hitPoint = hit.point;
            hitObject = hit.collider.gameObject;

            // Prüfen, ob eine Blase getroffen wurde
            var bubble = hit.collider.GetComponent<BubbleTouchHandler>();

            Debug.Log("Hit: " + hit.collider.name);

            // ------------------- BLICK AUF BLASE -------------------

            if (bubble != null && !bubble.hasBeenLookedAt)
            {
                //hitBubble = true;
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
            hitPoint = origin + direction * maxDistance;
        }

        // ------------------- DATENSATZ SPEICHERN -------------------
        
        gazeSamples.Add(new GridGazeSample
        {
            time = uiManager.CurrentTime,
            //time = Time.time - uiManager.gameDuration,

            origin = origin,
            direction = direction,

            headForward = headForward,
            eyeHeadAngle = eyeHeadAngle,

            hitPoint = hitPoint,

            hitSomething = hitSomething,
            hitObject = hitObject
        });
    }

    public GridGazeSample GetLatestSample()
    {
        if (gazeSamples.Count == 0)
            return null;

        return gazeSamples[gazeSamples.Count - 1];
    }

    /// <summary>
    /// Liefert alle bisher aufgezeichneten Blickdaten zurück.
    /// </summary>
    public List<GridGazeSample> GetData()
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
    public class GridGazeSample
    {
        public float time;

        public Vector3 origin;
        public Vector3 direction;

        public Vector3 headForward;
        public float eyeHeadAngle;

        public Vector3 hitPoint;

        public bool hitSomething;

        public GameObject hitObject;
    }
}

