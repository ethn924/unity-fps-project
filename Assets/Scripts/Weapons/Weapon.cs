using System;
using System.Collections;
using UnityEngine;

// Ce script gère les fonctionnalités de l'arme : tir (auto, coup par coup, rafale), gestion des munitions, rechargement et animations.
public class Weapon : MonoBehaviour
{
    // --- ÉNUMÉRATIONS ---
    // Un enum permet de créer une liste de choix (visible sous forme de menu déroulant dans l'inspecteur Unity)
    public enum ShootingMode
    {
        Single, // Coup par coup (un clic = une balle)
        Burst,  // Rafale (un clic = trois balles d'affilée)
        Auto    // Automatique (rester appuyé pour tirer en continu)
    }

    [Header("Mode de tir")]
    [Tooltip("Sélectionnez le mode de tir de l'arme.")]
    public ShootingMode currentShootingMode;

    [Header("Configuration du Tir")]
    [Tooltip("Délai minimum en secondes entre chaque tir.")]
    public float shootingDelay = 0.2f;
    [Tooltip("Intensité de l'écartement des balles (précision). 0 = tir parfaitement droit.")]
    public float spreadIntensity;

    [Header("Rafale (Mode Burst)")]
    [Tooltip("Nombre de balles tirées par rafale.")]
    public int bulletsPerBurst = 3;
    private int burstBulletLeft; // Compteur interne pour savoir combien de balles il reste à tirer dans la rafale actuelle

    [Header("Balle (Prefab & Physique)")]
    [Tooltip("Le préfabriqué 3D de la balle qui sera instancié lors du tir.")]
    public GameObject bulletPrefab;
    [Tooltip("L'emplacement de sortie de la balle (bout du canon de l'arme).")]
    public Transform bulletSpawn;
    [Tooltip("Vitesse de projection de la balle.")]
    public float bulletVelocity = 30f;
    [Tooltip("Durée de vie maximum de la balle avant d'être détruite si elle ne touche rien.")]
    public float bulletPrefabLifeTime = 3f;

    [Header("Effets Visuels & Animations")]
    [Tooltip("Effet de flash de lumière au canon de l'arme (Muzzle Flash).")]
    public GameObject muzzleEffect;
    [Tooltip("L'Animator attaché à l'arme pour jouer le recul.")]
    public Animator weaponAnimator;
    [Tooltip("Nom exact de l'animation de recul configurée dans l'Animator.")]
    public string recoilClipName = "Recoil_M1911";

    [Header("Gestion des munitions")]
    [Tooltip("Nombre maximum de balles que contient un chargeur.")]
    public int magazineSize = 8;
    [Tooltip("Nombre total de balles en réserve au départ.")]
    public int totalAmmo = 32;
    [Tooltip("Temps nécessaire pour recharger l'arme (en secondes).")]
    public float reloadTime = 1.5f;

    // Variables internes pour le tir et les munitions
    public bool isShooting, readyToShoot;
    private bool allowReset = true; // Empêche les conflits lors de la réinitialisation du tir
    private int bulletsLeft;        // Nombre de balles actuellement dans le chargeur
    private bool isReloading = false; // Indique si le joueur est en plein rechargement

    private void Awake()
    {
        // Initialisation de l'état de l'arme
        readyToShoot = true;
        burstBulletLeft = bulletsPerBurst;
    }

    private void Start()
    {
        // Sécurité : si les variables de munitions sont configurées à 0 dans l'inspecteur Unity, 
        // on attribue des valeurs par défaut pour éviter de démarrer avec une arme vide.
        if (magazineSize <= 0) magazineSize = 8;
        if (totalAmmo <= 0) totalAmmo = 32;
        
        bulletsLeft = magazineSize; // On commence avec un chargeur plein
        UpdateHUD(); // Met à jour l'affichage sur l'écran
    }

    // Met à jour le texte de l'UI (ex: "8/32") via le gestionnaire de munitions AmmoManager
    private void UpdateHUD()
    {
        if (AmmoManager.Instance != null && AmmoManager.Instance.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = bulletsLeft + "/" + totalAmmo;
        }
    }

    // Coroutine pour gérer le rechargement de l'arme avec un délai
    private IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime); // Attente de la fin du rechargement

        // Calcule le nombre de balles manquantes dans le chargeur actuel
        int bulletsToLoad = magazineSize - bulletsLeft;
        // Détermine combien de balles on peut effectivement prendre dans la réserve
        int bulletsToTake = Mathf.Min(bulletsToLoad, totalAmmo);

        bulletsLeft += bulletsToTake; // Met les balles dans le chargeur
        totalAmmo -= bulletsToTake;   // Les retire de la réserve de secours

        isReloading = false;
        UpdateHUD(); // Rafraîchit l'affichage UI
    }

    void Update()
    {
        // Si on recharge, on bloque le tir et les autres actions
        if (isReloading) return;

        // Rechargement manuel si on appuie sur la touche 'R' (seulement si le chargeur n'est pas plein et qu'on a des balles en réserve)
        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && totalAmmo > 0)
        {
            StartCoroutine(Reload());
            return;
        }

        // Récupération de l'entrée de tir (clic gauche de la souris - KeyCode.Mouse0)
        if (currentShootingMode == ShootingMode.Auto)
        {
            isShooting = Input.GetKey(KeyCode.Mouse0); // Reste vrai tant qu'on maintient le clic enfoncé
        }
        else if (currentShootingMode == ShootingMode.Single || currentShootingMode == ShootingMode.Burst)
        {
            isShooting = Input.GetKeyDown(KeyCode.Mouse0); // Vrai uniquement sur le premier clic
        }

        // Si l'arme est prête à tirer et que le joueur clique
        if (readyToShoot && isShooting)
        {
            if (bulletsLeft > 0)
            {
                burstBulletLeft = bulletsPerBurst;
                FireWeapon();
            }
            else if (totalAmmo > 0)
            {
                // Si le chargeur est vide mais qu'on a de la réserve, on recharge automatiquement
                StartCoroutine(Reload());
            }
        }
    }

    // Gère la logique de création du projectile, d'animation et de son lors du tir
    private void FireWeapon()
    {
        if (bulletsLeft <= 0) return;

        bulletsLeft--; // Utilise une balle
        UpdateHUD();   // Met à jour l'affichage des munitions

        // Joue les particules de flash de feu au bout du canon
        muzzleEffect.GetComponent<ParticleSystem>().Play();

        // Joue le son de tir via le SoundManager
        if (SoundManager.Instance != null && SoundManager.Instance.shootingSound1911 != null)
        {
            SoundManager.Instance.shootingSound1911.PlayOneShot(SoundManager.Instance.shootingSound1911.clip);
        }

        readyToShoot = false; // L'arme n'est plus prête, le délai commence

        // Durée par défaut du délai avant le prochain tir
        float actualDelay = shootingDelay;

        // Gestion de l'animation de recul
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("RECOIL");

            // Calcule la longueur de l'animation pour s'assurer que le délai de tir 
            // est au moins aussi long que la durée visuelle du recul de l'arme
            float recoilClipLength = GetClipLength(recoilClipName);
            if (recoilClipLength > 0f)
            {
                actualDelay = Mathf.Max(shootingDelay, recoilClipLength);
            }
        }

        // Détermine la direction du tir en appliquant la dispersion (spread)
        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;

        // Instancie la balle physique au niveau du canon de l'arme
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);
        bullet.transform.forward = shootingDirection; // Oriente la balle vers la cible

        // Propulse la balle avec une force physique (Rigidbody)
        bullet.GetComponent<Rigidbody>().AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);

        // Lance le destructeur automatique pour nettoyer la balle après quelques secondes
        StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifeTime));

        // Lance le chronomètre pour autoriser un nouveau tir après le délai requis
        if (allowReset)
        {
            Invoke("ResetShot", actualDelay);
            allowReset = false;
        }

        // Si on est en mode Rafale (Burst) et qu'il reste des balles à tirer dans cette rafale
        if (currentShootingMode == ShootingMode.Burst && burstBulletLeft > 1)
        {
            burstBulletLeft--;
            Invoke("FireWeapon", actualDelay); // Appelle à nouveau FireWeapon après le délai
        }
    }

    // Recherche la durée d'une animation dans l'Animator par son nom
    private float GetClipLength(string clipName)
    {
        if (weaponAnimator == null || weaponAnimator.runtimeAnimatorController == null) return 0f;

        foreach (AnimationClip clip in weaponAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
            {
                return clip.length;
            }
        }
        return 0f;
    }

    // Réautorise le tir
    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }

    // Calcule la direction en faisant partir un rayon (Raycast) depuis le centre de la caméra (le viseur)
    public Vector3 CalculateDirectionAndSpread()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Rayon au centre de l'écran
        RaycastHit hit;

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point; // Si on touche un obstacle, on vise ce point précis
        }
        else
        {
            targetPoint = ray.GetPoint(100); // Sinon, on vise un point à 100 mètres devant nous
        }

        Vector3 direction = targetPoint - bulletSpawn.position;

        // Ajoute un écartement aléatoire (dispersion / spread) sur les axes X et Y
        float x = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);
        float y = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);

        return direction + new Vector3(x, y, 0);
    }

    // Coroutine pour détruire la balle au bout d'un certain temps
    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bullet != null)
        {
            Destroy(bullet);
        }
    }
}