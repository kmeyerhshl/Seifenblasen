using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;

public class BubblePop : MonoBehaviour
{
    public ParticleSystem popEffect;
    public AudioClip popSound;

    // Wird automatisch von Unity aufgerufen,
    // wenn ein anderes Collider-Objekt den Trigger betritt.
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter");
        // Blase sofort platzen lassen
        Pop();
        // Zusätzliche Prüfung für MRTK-Hand- oder Joint-Objekte.
        // In MRTK enthalten die Namen der Hand-Tracker häufig
        // "Hand" oder "Joint".
        if (other.name.Contains("Hand") || other.name.Contains("Joint"))
        {
            Pop();
        }
    }

    void Pop()
    {
        // Partikeleffekt an der aktuellen Position der Blase erzeugen
        if (popEffect != null)
            Instantiate(popEffect, transform.position, Quaternion.identity);
        // Soundeffekt an der Position der Blase abspielen
        if (popSound != null)
            AudioSource.PlayClipAtPoint(popSound, transform.position);

        Debug.Log("Pop");
        // Blasenobjekt aus der Szene entfernen
        Destroy(gameObject);
    }
}
