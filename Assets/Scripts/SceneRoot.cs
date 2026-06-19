using UnityEngine;

public class SceneRoot : MonoBehaviour
{
    // Abstand der Szene zur Benutzerposition
    public float distance = 1.5f;

    // Zusätzlicher vertikaler Versatz der Szene
    public float heightOffset = 0f;

    void Start()
    {
        // Referenz auf die Hauptkamera (Benutzerposition)
        Transform cam = Camera.main.transform;

        // Blickrichtung der Kamera ermitteln
        Vector3 forward = cam.forward;

        // Vertikale Komponente entfernen,
        // damit die Szene nur horizontal vor dem Benutzer platziert wird
        forward.y = 0;

        // Richtungsvektor normalisieren
        forward.Normalize();

        // Szene vor dem Benutzer positionieren
        transform.position =
            cam.position +
            forward * distance +
            Vector3.up * heightOffset;

        // Szene in Blickrichtung des Benutzers ausrichten
        transform.rotation =
            Quaternion.LookRotation(forward);
    }
}
