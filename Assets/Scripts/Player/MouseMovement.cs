using UnityEngine;

// Ce script contrôle les mouvements de la caméra avec la souris (regarder autour de soi).
// On l'attache généralement à la caméra principale (Main Camera).
public class MouseMovement : MonoBehaviour
{
    [Header("Configuration de la souris")]
    [Tooltip("Vitesse de rotation de la caméra.")]
    public float mouseSensitivity = 500f;

    [Header("Limites de rotation verticale (Évite le tournis)")]
    [Tooltip("Angle maximum pour regarder vers le haut (degré négatif).")]
    public float topClamp = -90f;
    [Tooltip("Angle maximum pour regarder vers le bas (degré positif).")]
    public float bottomClamp = 90f;

    // Variables pour stocker l'accumulation des mouvements de la souris
    private float xRotation = 0f; // Rotation haut/bas (autour de l'axe X)
    private float yRotation = 0f; // Rotation gauche/droite (autour de l'axe Y)

    // Variable technique : évite un sursaut de la caméra au tout premier mouvement après verrouillage du curseur
    private bool skipNextFrame = true;

    void Start()
    {
        // Cache le curseur de la souris à l'écran et le bloque au centre de la fenêtre de jeu
        Cursor.lockState = CursorLockMode.Locked;

        // Force la caméra à regarder bien en face (rotation 0, 0, 0) dès le lancement
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    void Update()
    {
        // Durant les 0.2 premières secondes, on force la caméra à rester neutre.
        // Cela évite que le recentrage automatique de la souris par Windows au démarrage n'incline brutalement la caméra vers le bas.
        if (Time.timeSinceLevelLoad < 0.2f)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            xRotation = 0f;
            yRotation = 0f;
            return;
        }

        // Récupère les mouvements de la souris (axes standard de Unity)
        // Time.deltaTime permet de rendre la vitesse fluide et identique peu importe le nombre d'images par seconde (FPS) de l'ordinateur.
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Si on doit ignorer la frame actuelle pour éviter le bug de sursaut, on le fait ici
        if (skipNextFrame)
        {
            skipNextFrame = false;
            return;
        }

        // Rotation verticale : bouger la souris vers le haut (Y positif) doit faire lever la caméra (rotation X négative)
        xRotation -= mouseY;
        // Empêche la caméra de faire un tour complet (ce qui mettrait la tête du joueur à l'envers)
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp);

        // Rotation horizontale : bouger la souris vers la droite (X positif) fait tourner la caméra vers la droite
        yRotation += mouseX;

        // Applique les angles calculés sous forme de rotation (Quaternion) à l'objet caméra
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}