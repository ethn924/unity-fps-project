using UnityEngine;

// Gère la rotation de la caméra avec la souris
public class MouseMovement : MonoBehaviour
{
    public float mouseSensitivity = 500f;
    public float topClamp = -90f;
    public float bottomClamp = 90f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private bool skipNextFrame = true; // Évite un sursaut de caméra initial

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Cache et verrouille le curseur
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    void Update()
    {
        // Ignore les mouvements de souris au tout début pour éviter les sauts brusques
        if (Time.timeSinceLevelLoad < 0.2f)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            xRotation = 0f;
            yRotation = 0f;
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        if (skipNextFrame)
        {
            skipNextFrame = false;
            return;
        }

        // Calcule et limite la rotation verticale (haut/bas)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp);

        // Rotation horizontale (gauche/droite)
        yRotation += mouseX;

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
