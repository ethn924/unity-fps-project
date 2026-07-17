using UnityEngine;

// Gère le comportement de la balle lors d'un impact
public class Bullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision objectWeHit)
    {
        // Impact sur une cible
        if (objectWeHit.gameObject.CompareTag("Target"))
        {
            CreateBulletImpactEffect(objectWeHit);
            Destroy(gameObject);
        }

        // Impact sur un mur ou objet destructible
        if (objectWeHit.gameObject.CompareTag("Wall"))
        {
            CreateBulletImpactEffect(objectWeHit);
            CubeDemolisher demolisher = objectWeHit.gameObject.GetComponent<CubeDemolisher>();
            if (demolisher != null) demolisher.Demolish();
            Destroy(gameObject);
        }

        // Impact sur une bouteille
        if (objectWeHit.gameObject.CompareTag("Beer"))
        {
            BeerBottle beerBottle = objectWeHit.gameObject.GetComponent<BeerBottle>();
            if (beerBottle != null)
            {
                // Impulsion physique dans la direction de la balle
                beerBottle.Explode(transform.forward, 12f);
            }
            CreateBulletImpactEffect(objectWeHit);
            Destroy(gameObject);
        }
    }

    // Instancie l'effet visuel de l'impact (trou de balle, éclats)
    void CreateBulletImpactEffect(Collision objectWeHit)
    {
        ContactPoint contact = objectWeHit.contacts[0];
        GameObject hole = Instantiate(
            GlobalReferences.Instance.bulletImpactEffectPrefab,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );
        hole.transform.SetParent(objectWeHit.gameObject.transform);
    }
}
