using System;
using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    // Tir
    public bool isShooting, readyToShoot;
    bool allowReset = true;
    public float shootingDelay = 0.2f;

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

    public GameObject muzzleEffect;

    public Animator weaponAnimator;

    // Nom exact du clip d'animation de recul (doit correspondre au nom dans l'Animator)
    public string recoilClipName = "Recoil_M1911";

    public enum ShootingMode
    {
        Single,
        Burst,
        Auto
    }

    public ShootingMode currentShootingMode;

    [Header("Ammo Settings")]
    public int magazineSize = 8;
    public int totalAmmo = 32;
    public float reloadTime = 1.5f;

    private int bulletsLeft;
    private bool isReloading = false;

    private void Awake()
    {
        readyToShoot = true;
        burstBulletLeft = bulletsPerBurst;
    }

    private void Start()
    {
        if (magazineSize <= 0) magazineSize = 8;
        if (totalAmmo <= 0) totalAmmo = 32;
        bulletsLeft = magazineSize;
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (AmmoManager.Instance != null && AmmoManager.Instance.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = bulletsLeft + "/" + totalAmmo;
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);

        int bulletsToLoad = magazineSize - bulletsLeft;
        int bulletsToTake = Mathf.Min(bulletsToLoad, totalAmmo);

        bulletsLeft += bulletsToTake;
        totalAmmo -= bulletsToTake;

        isReloading = false;
        UpdateHUD();
    }

    void Update()
    {
        if (isReloading) return;

        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && totalAmmo > 0)
        {
            StartCoroutine(Reload());
            return;
        }

        if (currentShootingMode == ShootingMode.Auto)
        {
            isShooting = Input.GetKey(KeyCode.Mouse0);
        }
        else if (currentShootingMode == ShootingMode.Single ||
            currentShootingMode == ShootingMode.Burst)
        {
            isShooting = Input.GetKeyDown(KeyCode.Mouse0);
        }

        if (readyToShoot && isShooting)
        {
            if (bulletsLeft > 0)
            {
                burstBulletLeft = bulletsPerBurst;
                FireWeapon();
            }
            else if (totalAmmo > 0)
            {
                StartCoroutine(Reload());
            }
        }
    }

    private void FireWeapon()
    {
        if (bulletsLeft <= 0) return;

        bulletsLeft--;
        UpdateHUD();

        muzzleEffect.GetComponent<ParticleSystem>().Play();

        // Son de tir (corrigé : l'appel manquait, le son n'était jamais joué)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.shootingSound1911.PlayOneShot(SoundManager.Instance.shootingSound1911.clip);
        }

        readyToShoot = false;

        // Calcule la durée réelle du délai avant le prochain tir :
        // soit shootingDelay (config manuelle), soit la durée de l'anim de recul si elle est plus longue
        float actualDelay = shootingDelay;

        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("RECOIL");

            float recoilClipLength = GetClipLength(recoilClipName);
            if (recoilClipLength > 0f)
            {
                actualDelay = Mathf.Max(shootingDelay, recoilClipLength);
            }
        }

        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);
        bullet.transform.forward = shootingDirection;

        bullet.GetComponent<Rigidbody>().AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);

        StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifeTime));

        if (allowReset)
        {
            Invoke("ResetShot", actualDelay);
            allowReset = false;
        }

        if (currentShootingMode == ShootingMode.Burst && burstBulletLeft > 1)
        {
            burstBulletLeft--;
            Invoke("FireWeapon", actualDelay);
        }
    }

    // Cherche la durée du clip d'animation par son nom dans l'Animator Controller
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

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }

    public Vector3 CalculateDirectionAndSpread()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100);
        }

        Vector3 direction = targetPoint - bulletSpawn.position;

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