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
        // Pas de démolition : la bouteille entière réagit à l'impact
        rb.isKinematic = false;
        rb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);

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