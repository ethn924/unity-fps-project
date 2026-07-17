using System;
using System.Collections;
using UnityEngine;

// Gère le tir, les munitions, le rechargement et le recul de l'arme
public class Weapon : MonoBehaviour
{
    public enum ShootingMode { Single, Burst, Auto }

    [Header("Mode de Tir")]
    public ShootingMode currentShootingMode;

    [Header("Paramètres de Tir")]
    public float shootingDelay = 0.2f;
    public float spreadIntensity;

    [Header("Mode Rafale")]
    public int bulletsPerBurst = 3;
    private int burstBulletLeft;

    [Header("Projectile")]
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 30f;
    public float bulletPrefabLifeTime = 3f;

    [Header("Visuels & Anim")]
    public GameObject muzzleEffect;
    public Animator weaponAnimator;
    public string recoilClipName = "Recoil_M1911";

    [Header("Munitions")]
    public int magazineSize = 8;
    public int totalAmmo = 32;
    [Tooltip("Si activé, recharge automatiquement quand le chargeur est vide et qu'on tente de tirer.")]
    public bool autoReloadOnEmpty = true;
    public float reloadTime = 1.5f;

    public bool isShooting, readyToShoot;
    private bool allowReset = true;
    private int bulletsLeft;
    private bool isReloading = false;

    private void Awake()
    {
        readyToShoot = true;
        burstBulletLeft = bulletsPerBurst;
    }

    private void Start()
    {
        // Initialisation sécurisée des munitions au démarrage
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

        if (SoundManager.Instance != null && SoundManager.Instance.reloadingSound1911 != null)
        {
            SoundManager.Instance.reloadingSound1911.Play();
        }

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

        // Rechargement manuel ('R')
        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && totalAmmo > 0)
        {
            StartCoroutine(Reload());
            return;
        }

        // Détection du clic de tir selon le mode
        if (currentShootingMode == ShootingMode.Auto)
        {
            isShooting = Input.GetKey(KeyCode.Mouse0);
        }
        else
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
            else if (totalAmmo > 0 && autoReloadOnEmpty)
            {
                StartCoroutine(Reload());
            }
            else
            {
                // Son de chargeur vide
                if (SoundManager.Instance != null && SoundManager.Instance.emptyMagazineSound1911 != null)
                {
                    SoundManager.Instance.emptyMagazineSound1911.Play();
                }
            }
        }
    }

    private void FireWeapon()
    {

        if (bulletsLeft == 0 && isShooting)
        {
            SoundManager.Instance.emptyMagazineSound1911.Play();
        }

        if (bulletsLeft <= 0) return;

        bulletsLeft--;
        UpdateHUD();

        muzzleEffect.GetComponent<ParticleSystem>().Play();

        if (SoundManager.Instance != null && SoundManager.Instance.shootingSound1911 != null)
        {
            SoundManager.Instance.shootingSound1911.PlayOneShot(SoundManager.Instance.shootingSound1911.clip);
        }

        readyToShoot = false;
        float actualDelay = shootingDelay;

        // Déclenche l'animation de recul et ajuste le délai
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("RECOIL");
            float recoilClipLength = GetClipLength(recoilClipName);
            if (recoilClipLength > 0f)
            {
                actualDelay = Mathf.Max(shootingDelay, recoilClipLength);
            }
        }

        // Création et projection de la balle
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

        // Gère la rafale
        if (currentShootingMode == ShootingMode.Burst && burstBulletLeft > 1)
        {
            burstBulletLeft--;
            Invoke("FireWeapon", actualDelay);
        }
    }

    private float GetClipLength(string clipName)
    {
        if (weaponAnimator == null || weaponAnimator.runtimeAnimatorController == null) return 0f;
        foreach (AnimationClip clip in weaponAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName) return clip.length;
        }
        return 0f;
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }

    // Calcule la trajectoire de tir depuis le centre de la caméra avec dispersion
    public Vector3 CalculateDirectionAndSpread()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        Vector3 targetPoint = Physics.Raycast(ray, out hit) ? hit.point : ray.GetPoint(100);

        Vector3 direction = targetPoint - bulletSpawn.position;
        float x = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);
        float y = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);

        return direction + new Vector3(x, y, 0);
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bullet != null) Destroy(bullet);
    }
}
