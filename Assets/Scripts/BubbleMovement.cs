using UnityEngine;

public class BubbleMovement : MonoBehaviour
{
    public float speed = 0.2f;
    public Vector3 direction;

    void Start()
    {
        // Zufällige Richtung
        direction = Random.insideUnitSphere;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }
}
