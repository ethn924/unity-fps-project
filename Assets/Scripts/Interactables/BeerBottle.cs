using UnityEngine;

// Gère la physique et les sons d'impact d'une bouteille
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class BeerBottle : MonoBehaviour
{
    private Rigidbody rb;
    private AudioSource audioSource;

    [Header("Audio")]
    public AudioClip impactSound;
    public float minTimeBetweenSounds = 0.1f;
    private float lastSoundTime = -999f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    public void Explode()
    {
        Explode(Random.insideUnitSphere, 5f);
    }

    // Fait tomber ou projette la bouteille sous l'effet d'une force
    public void Explode(Vector3 forceDirection, float forceIntensity)
    {
        rb.isKinematic = false; // Active les calculs physiques
        
        // Applique la force directionnelle avec une légère impulsion verticale
        Vector3 pushForce = (forceDirection + Vector3.up * 0.2f).normalized * forceIntensity;
        rb.AddForce(pushForce, ForceMode.Impulse);

        // Ajoute un effet de rotation (tournoiement)
        Vector3 randomTorque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ) * (forceIntensity * 0.5f);
        rb.AddTorque(randomTorque, ForceMode.Impulse);

        PlayImpactSound();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!rb.isKinematic) PlayImpactSound();
    }

    private void PlayImpactSound()
    {
        if (impactSound == null) return;
        if (Time.time - lastSoundTime < minTimeBetweenSounds) return;

        lastSoundTime = Time.time;
        audioSource.PlayOneShot(impactSound);
    }
}
