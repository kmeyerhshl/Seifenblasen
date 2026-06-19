using UnityEngine;

public class SceneRoot : MonoBehaviour
{
    public float distance = 1.5f;
    public float heightOffset = 0f;

    void Start()
    {
        Transform cam = Camera.main.transform;

        Vector3 forward = cam.forward;
        forward.y = 0; // kein Hoch-/Runterkippen
        forward.Normalize();

        transform.position =
            cam.position +
            forward * distance +
            Vector3.up * heightOffset;

        transform.rotation = Quaternion.LookRotation(forward);
    }
}
