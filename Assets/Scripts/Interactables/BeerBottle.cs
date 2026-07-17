using UnityEngine;

// Ce script gère le comportement physique d'une bouteille de bière lorsqu'elle est touchée par un projectile ou qu'elle tombe.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class BeerBottle : MonoBehaviour
{
    private Rigidbody rb;
    private AudioSource audioSource;

    [Header("Paramètres audio")]
    [Tooltip("Le clip sonore joué lors d'un impact physique.")]
    public AudioClip impactSound;
    [Tooltip("Délai minimum en secondes requis entre deux sons d'impact consécutifs (évite le grésillement lors des roulades).")]
    public float minTimeBetweenSounds = 0.1f;

    // Stocke le moment précis où le dernier son a été joué
    private float lastSoundTime = -999f;

    void Start()
    {
        // Récupère automatiquement les composants Rigidbody et AudioSource attachés à la bouteille
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    // Version par défaut sans paramètre (surcharge pour la compatibilité)
    public void Explode()
    {
        // Si aucune direction n'est fournie, on pousse dans une direction aléatoire
        Explode(Random.insideUnitSphere, 5f);
    }

    // Fait réagir la bouteille à l'impact en simulant sa projection physique
    public void Explode(Vector3 forceDirection, float forceIntensity)
    {
        // Active les calculs de physique sur la bouteille (elle commence en 'isKinematic = true' pour ne pas tomber toute seule)
        rb.isKinematic = false;
        
        // Calcule le vecteur de poussée dans la direction de la balle avec une légère impulsion vers le haut (0.2f)
        Vector3 pushForce = (forceDirection + Vector3.up * 0.2f).normalized * forceIntensity;
        
        // Applique la force sous forme d'impulsion instantanée (parfait pour les chocs et les impacts de balle)
        rb.AddForce(pushForce, ForceMode.Impulse);

        // Ajoute un couple de rotation aléatoire (Torque) pour faire tournoyer la bouteille sur elle-même dans les airs
        Vector3 randomTorque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ) * (forceIntensity * 0.5f);
        
        rb.AddTorque(randomTorque, ForceMode.Impulse);

        // Joue immédiatement le son de bris/choc de la bouteille
        PlayImpactSound();
    }

    // Fonction intégrée de Unity appelée automatiquement lors d'un choc physique (ex: la bouteille retombe et touche le sol)
    private void OnCollisionEnter(Collision collision)
    {
        // Si la bouteille a été percutée et qu'elle n'est plus statique (cinématique), on joue le son d'impact
        if (!rb.isKinematic)
        {
            PlayImpactSound();
        }
    }

    // Joue le son de collision en s'assurant qu'il n'est pas spammé trop vite
    private void PlayImpactSound()
    {
        if (impactSound == null) return;

        // Si le temps écoulé depuis le dernier son est inférieur à minTimeBetweenSounds, on ne joue rien
        if (Time.time - lastSoundTime < minTimeBetweenSounds) return;

        // Met à jour l'horodatage et joue le son de manière asynchrone (sans couper les autres sons de l'objet)
        lastSoundTime = Time.time;
        audioSource.PlayOneShot(impactSound);
    }
}