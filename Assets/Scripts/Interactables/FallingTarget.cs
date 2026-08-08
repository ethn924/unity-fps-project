using UnityEngine;

// Cible qui bascule et tombe quand une balle la touche
[RequireComponent(typeof(Rigidbody))]
public class FallingTarget : MonoBehaviour
{
    [Header("Chute")]
    public float fallForce = 3f; // Force de la poussée qui fait basculer la cible

    [Header("Audio")]
    public AudioClip fallSound;

    private Rigidbody rb;
    private bool hasFallen;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // La cible reste figée tant qu'elle n'a pas été touchée
    }

    // Appelé par Bullet à l'impact : fait basculer la cible dans la direction du tir
    public void Fall(Vector3 bulletDirection, Vector3 hitPoint)
    {
        if (hasFallen) return; // Une cible déjà tombée ne réagit plus
        hasFallen = true;

        rb.isKinematic = false; // Active les calculs physiques

        // Force appliquée au point d'impact : la plaque bascule de façon naturelle
        rb.AddForceAtPosition(bulletDirection.normalized * fallForce, hitPoint, ForceMode.Impulse);

        if (fallSound != null)
            AudioSource.PlayClipAtPoint(fallSound, hitPoint);
    }
}
