using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BeerBottle : MonoBehaviour
{
    private Rigidbody rb;
    private AudioSource audioSource;

    [Header("Son")]
    // Son joué au tir et à chaque collision physique
    public AudioClip impactSound;

    // Empêche de spammer le son si plusieurs collisions arrivent très proches dans le temps
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

    public void Explode(Vector3 forceDirection, float forceIntensity)
    {
        rb.isKinematic = false;
        
        // Applique une force dans la direction de l'impact avec un petit mouvement ascendant
        Vector3 pushForce = (forceDirection + Vector3.up * 0.2f).normalized * forceIntensity;
        rb.AddForce(pushForce, ForceMode.Impulse);

        // Ajoute un couple de rotation pour faire tournoyer la bouteille
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
        // Joue le son à chaque collision physique (sol, murs, autres objets)
        PlayImpactSound();
    }

    private void PlayImpactSound()
    {
        if (impactSound == null) return;

        // Évite de jouer le son trop souvent en rafale (ex: rebonds rapprochés)
        if (Time.time - lastSoundTime < minTimeBetweenSounds) return;

        lastSoundTime = Time.time;
        audioSource.PlayOneShot(impactSound);
    }
}