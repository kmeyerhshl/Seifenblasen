using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    void Update()
    {
        if (Camera.main == null) return;

        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180f, 0); // Text richtig herum
    }
}
