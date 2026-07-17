using UnityEngine;

// Ce script contrôle le déplacement physique du joueur, la gravité et le saut.
// On l'attache à l'objet racine du joueur (Player) qui possède un composant CharacterController.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;

    [Header("Paramètres de déplacement")]
    [Tooltip("Vitesse de déplacement au sol.")]
    public float speed = 12f;
    [Tooltip("Force de la gravité appliquée. Doit rester négative pour attirer le joueur vers le bas.")]
    public float gravity = -19.62f; // -9.81 * 2 pour une sensation de chute plus dynamique
    [Tooltip("Hauteur maximale du saut.")]
    public float jumpHeight = 3f;

    [Header("Détection du sol (Ground Check)")]
    [Tooltip("Objet vide placé au niveau des pieds du joueur pour tester s'il touche le sol.")]
    public Transform groundCheck;
    [Tooltip("Rayon de la sphère de détection du sol.")]
    public float groundDistance = 0.4f;
    [Tooltip("Couches (Layers) considérées comme du sol (ex: Ground, Default).")]
    public LayerMask groundMask;

    // Variables internes pour le calcul physique
    private Vector3 velocity; // Vitesse de chute/saut actuelle
    private bool isGrounded;  // Indique si le joueur touche le sol à cette image
    private bool isMoving;    // Indique si le joueur est en train de marcher
    private Vector3 lastPosition = Vector3.zero; // Stocke la position de la frame précédente pour calculer le mouvement

    void Start()
    {
        // Récupère automatiquement le composant CharacterController attaché à cet objet
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1. Détection du sol : on dessine une sphère virtuelle aux pieds du joueur. 
        // Si elle touche un objet possédant un Layer présent dans groundMask, isGrounded devient vrai (true).
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Si le joueur est au sol et que sa vitesse de descente est négative (il tombe), on réinitialise sa vitesse verticale.
        // On met -2f plutôt que 0f pour s'assurer qu'il reste bien plaqué contre le sol et les pentes.
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        // 2. Déplacement horizontal : récupère les entrées clavier (Z/S/Q/D ou flèches directionnelles).
        // x correspond aux mouvements latéraux (Horizontal), z aux mouvements avant/arrière (Vertical).
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Calcule la direction du mouvement par rapport à l'orientation du joueur.
        // transform.right pointe vers la droite du joueur, transform.forward vers l'avant.
        Vector3 move = transform.right * x + transform.forward * z;

        // Déplace le joueur dans cette direction en fonction de sa vitesse et du temps écoulé
        controller.Move(move * speed * Time.deltaTime);

        // 3. Gestion du saut : si la touche Saut (Espace par défaut) est pressée et que le joueur est au sol.
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Formule physique standard pour calculer la vitesse verticale requise pour atteindre une hauteur donnée :
            // Vitesse = RacineCarrée(Hauteur * -2 * Gravité)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 4. Application de la gravité : la vitesse verticale augmente constamment vers le bas.
        velocity.y += gravity * Time.deltaTime;

        // Applique le vecteur vitesse verticale (chute ou saut) sur le joueur
        controller.Move(velocity * Time.deltaTime);

        // 5. Détection de mouvement (utile pour jouer des sons de pas ou des animations plus tard)
        if (lastPosition != transform.position && isGrounded)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }

        // Met à jour la position précédente pour la prochaine frame
        lastPosition = transform.position;
    }
}