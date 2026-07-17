using UnityEngine;

public class MouseMovement : MonoBehaviour
{
    public float mouseSensitivity = 500f;

    // Angle actuel haut/bas et gauche/droite
    float xRotation = 0f;
    float yRotation = 0f;

    // Limites pour ne pas regarder à 360°
    public float topClamp = -90f;
    public float bottomClamp = 90f;

    // Le verrouillage du curseur au centre de l'écran génère un delta souris artificiel
    // au frame suivant (warp du curseur = mouvement détecté). On ignore ce premier frame.
    bool skipNextFrame = true;

    void Start()
    {
        // Cache et bloque le curseur au centre de l'écran
        Cursor.lockState = CursorLockMode.Locked;

        // Corrigé : force une rotation neutre dès le spawn, sinon la rotation
        // laissée dans l'éditeur (ex: caméra inclinée) reste affichée le temps
        // que la 1ère frame (ignorée ci-dessous) soit passée.
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    void Update()
    {
        // On attend que l'initialisation de la scène soit stabilisée pour éviter le "mouse warp" au spawn
        if (Time.timeSinceLevelLoad < 0.2f)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            xRotation = 0f;
            yRotation = 0f;
            return;
        }

        // Récupère le déplacement de la souris
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        if (skipNextFrame)
        {
            skipNextFrame = false;
            return;
        }

        // Rotation verticale (haut/bas)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp); // empêche le retournement caméra

        // Rotation horizontale (gauche/droite)
        yRotation += mouseX; // += obligatoire, sinon la souris est inversée

        // Applique les deux rotations sur cet objet (la caméra)
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}