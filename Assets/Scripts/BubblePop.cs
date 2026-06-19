using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;

public class BubblePop : MonoBehaviour
{
    public ParticleSystem popEffect;
    public AudioClip popSound;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter");
        Pop();
        // Optional: Prüfen, ob es ein MRTK-Handobjekt ist
        if (other.name.Contains("Hand") || other.name.Contains("Joint"))
        {
            Pop();
        }
    }

    void Pop()
    {
        if (popEffect != null)
            Instantiate(popEffect, transform.position, Quaternion.identity);

        if (popSound != null)
            AudioSource.PlayClipAtPoint(popSound, transform.position);

        Debug.Log("Pop");
        Destroy(gameObject);
    }
}
