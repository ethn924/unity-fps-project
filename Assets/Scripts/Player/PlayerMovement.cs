using UnityEngine;

// Gère les déplacements physiques, la gravité et le saut du joueur
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;

    [Header("Paramètres")]
    public float speed = 12f;
    public float gravity = -19.62f; // Force d'attraction vers le bas
    public float jumpHeight = 3f;

    [Header("Détection du sol")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private Vector3 velocity;
    private bool isGrounded;
    private bool isMoving;
    private Vector3 lastPosition = Vector3.zero;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Détection de contact avec le sol
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // Maintient le joueur au sol
        }

        // Déplacement horizontal clavier
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        // Gestion du saut
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); // Formule physique du saut
        }

        // Application constante de la gravité
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Détecte le mouvement
        isMoving = (lastPosition != transform.position && isGrounded);
        lastPosition = transform.position;
    }
}
