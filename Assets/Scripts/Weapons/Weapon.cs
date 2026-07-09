using System;
using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Camera playerCamera;

    // Tir
    public bool isShooting, readyToShoot;
    bool allowReset = true;
    public float shootingDelay = 2f;

    // Rafale (mode Burst)
    public int bulletsPerBurst = 3;
    public int burstBulletLeft;

    // Dispersion des tirs (précision)
    public float spreadIntensity;

    // Balle
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 30f;
    public float bulletPrefabLifeTime = 3f;

    public enum ShootingMode
    {
        Single,
        Burst,
        Auto
    }

    public ShootingMode currentShootingMode;

    private void Awake()
    {
        readyToShoot = true;
        burstBulletLeft = bulletsPerBurst;
    }

    void Update()
    {
        // Détecte l'input selon le mode de tir choisi
        if (currentShootingMode == ShootingMode.Auto)
        {
            isShooting = Input.GetKey(KeyCode.Mouse0); // maintenu = tir continu
        }
        else if (currentShootingMode == ShootingMode.Single ||
            currentShootingMode == ShootingMode.Burst)
        {
            isShooting = Input.GetKeyDown(KeyCode.Mouse0); // un seul clic = un seul tir
        }

        if (readyToShoot && isShooting)
        {
            burstBulletLeft = bulletsPerBurst;
            FireWeapon();
        }
    }

    private void FireWeapon()
    {
        readyToShoot = false;

        // Calcule où vise le joueur (avec dispersion aléatoire)
        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;

        // Crée la balle au point de tir
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);
        bullet.transform.forward = shootingDirection;

        // Propulse la balle (bulletPrefab doit avoir un Rigidbody)
        bullet.GetComponent<Rigidbody>().AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);

        // Détruit la balle après un délai pour éviter d'accumuler des objets en mémoire
        StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifeTime));

        // Gère le délai entre chaque tir
        if (allowReset)
        {
            Invoke("ResetShot", shootingDelay);
            allowReset = false;
        }

        // Gère la rafale (plusieurs balles à la suite)
        if (currentShootingMode == ShootingMode.Burst && burstBulletLeft > 1)
        {
            burstBulletLeft--;
            Invoke("FireWeapon", shootingDelay);
        }
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }

    public Vector3 CalculateDirectionAndSpread()
    {
        // Envoie un rayon invisible depuis le centre de l'écran pour savoir où on vise
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point; // touche un objet
        }
        else
        {
            targetPoint = ray.GetPoint(100); // vise dans le vide, à 100m
        }

        Vector3 direction = targetPoint - bulletSpawn.position;

        // Ajoute une dispersion aléatoire (précision de l'arme)
        float x = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);
        float y = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);

        return direction + new Vector3(x, y, 0);
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(bullet);
    }
}