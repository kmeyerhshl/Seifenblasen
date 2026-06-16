using UnityEngine;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

public class EyeTracking : MonoBehaviour
{
    public bool isTracking = false;

    [Header("Raycast Settings")]
    public float maxDistance = 5f;
    public LayerMask gazeLayerMask = ~0; // alles

    [Header("Optional References")]
    public GameManager gameManager;
    public GameTimer gameTimer;

    private List<GazeSample> gazeSamples = new List<GazeSample>();

    void Update()
    {
        if (!isTracking) return;

        var eyeProvider = CoreServices.InputSystem?.EyeGazeProvider;
        if (eyeProvider == null) return;

        Vector3 origin = eyeProvider.GazeOrigin;
        Vector3 direction = eyeProvider.GazeDirection;
        Vector3 headForward = Camera.main.transform.forward;
        float eyeHeadAngle = Vector3.Angle(headForward, direction);

        Ray ray = new Ray(origin, direction);

        Vector3 hitPoint;

        // Bubble check (optional direkt hier oder im GameManager)
        int bubbleId = -1;
        bool hitBubble = false;
        //bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxDistance, gazeLayerMask);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxDistance);
        //bool hitSomething = Physics.SphereCast(ray, radius, out RaycastHit hit, maxDistance, gazeLayerMask);

        if (hitSomething)
        {
            hitPoint = hit.point;
            var bubble = hit.collider.GetComponent<BubbleTouchHandler>();
            Debug.Log("Hit: " + hit.collider.name);

            if (bubble != null && !bubble.hasBeenLookedAt)
            {
                /*var col = bubble.GetComponent<Collider>();
                if (col == null)
                {
                    Debug.LogError("❌ Bubble hat KEINEN Collider!");
                }
                else
                {
                    Debug.Log("✅ Collider gefunden: " + col.GetType().Name);
                }*/
                hitBubble = true;
                bubbleId = bubble.bubbleId;

                //if (!bubble.hasBeenLookedAt)
                //{
                    bubble.hasBeenLookedAt = true;
                    bubble.firstLookTime = Time.time - bubble.spawnTime;

                    gameManager.LogEyeReactionTime(bubble.bubbleId, bubble.firstLookTime);
                //}
            }
        }
        else
        {
            // fallback: Punkt in Blickrichtung
            hitPoint = origin + direction * maxDistance;
            /*float fieldZ = gameManager.spawner.center.z;
            float t = (fieldZ - origin.z) / direction.z;
            hitPoint = origin + direction * t;*/
        }

        gazeSamples.Add(new GazeSample
        {
            //time = Time.time,
            time = gameTimer.currentTime,
            origin = origin,
            direction = direction,
            headForward = headForward,
            eyeHeadAngle = eyeHeadAngle,
            hitPoint = hitPoint,
            hitBubble = hitBubble,
            bubbleId = bubbleId
        });
    }

    public List<GazeSample> GetData()
    {
        return gazeSamples;
    }

    public void ClearData()
    {
        gazeSamples.Clear();
    }

    public void ResetData()
    {
        gazeSamples.Clear();
    }

    // Datenstruktur
    public class GazeSample
    {
        public float time;
        public Vector3 origin;
        public Vector3 direction;
        public Vector3 headForward;
        public float eyeHeadAngle;
        public Vector3 hitPoint;
        public bool hitBubble;
        public int bubbleId;
    }

    /*private List<Vector3> gazePositions = new List<Vector3>();
    private Vector3 worldGaze;
    private List<Vector3> gazeLocalPositions = new List<Vector3>();
    public BubbleSpawner spawner;
    public Vector3 localGaze;

        void Update()
        {
        if (!isTracking) return;

        var eyeProvider = CoreServices.InputSystem?.EyeGazeProvider;

        if (eyeProvider == null)
        {
            Debug.Log("No Eye Provider");
            return;
        }

        Debug.Log("EyeTrackingEnabled: " + eyeProvider.IsEyeTrackingEnabled);

        Debug.Log("Origin: " + eyeProvider.GazeOrigin);
        Debug.Log("Dir: " + eyeProvider.GazeDirection);

        worldGaze = eyeProvider.GazeOrigin + eyeProvider.GazeDirection * 2f;

        localGaze = worldGaze - spawner.center;

        gazeLocalPositions.Add(localGaze);

            /*Vector3 origin = eyeProvider.GazeOrigin;
        Vector3 direction = eyeProvider.GazeDirection;

        latestGaze = origin + direction * 2f;

        gazePositions.Add(latestGaze);
            Debug.Log("gazePositions: " + latestGaze);*/

    /*}

            public Vector3 GetLatestGaze()
        {
            return localGaze;
        }

        public List<Vector3> GetData()
        {
            return gazeLocalPositions;
        }

        public void ClearData()
        {
            gazeLocalPositions.Clear();
        }*/
}
