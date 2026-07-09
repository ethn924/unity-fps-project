using UnityEngine;

public class MouseMovement : MonoBehaviour
{
    public float mouseSensitivity = 500f;

    // Angle actuel haut/bas et gauche/droite
    float xRotation = 0f;
    float yRotation = 0f;

    // Limites pour ne pas regarder � 360�
    public float topClamp = -90f;
    public float bottomClamp = 90f;

    // Le verrouillage du curseur au centre de l'�cran g�n�re un delta souris artificiel
    // au frame suivant (warp du curseur = mouvement d�tect�). On ignore ce premier frame.
    bool skipNextFrame = true;

    void Start()
    {
        // Cache et bloque le curseur au centre de l'�cran
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // R�cup�re le d�placement de la souris
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        if (skipNextFrame)
        {
            skipNextFrame = false;
            return;
        }

        // Rotation verticale (haut/bas)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp); // emp�che le retournement cam�ra

        // Rotation horizontale (gauche/droite)
        yRotation += mouseX; // += obligatoire, sinon la souris est invers�e

        // Applique les deux rotations sur cet objet (la cam�ra)
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}