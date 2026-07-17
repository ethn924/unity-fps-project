using UnityEngine;

public class Bullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision objectWeHit)
    {
        // Touche une cible
        if (objectWeHit.gameObject.CompareTag("Target"))
        {
            print("hit " + objectWeHit.gameObject.name + " !");
            CreateBulletImpactEffect(objectWeHit);
            Destroy(gameObject);
        }

        // Touche un mur (mur normal OU cube démolissable, tous deux tagués "Wall")
        if (objectWeHit.gameObject.CompareTag("Wall"))
        {
            print("hit a wall");
            CreateBulletImpactEffect(objectWeHit);

            // Si l'objet touché a un CubeDemolisher, on le démolit
            CubeDemolisher demolisher = objectWeHit.gameObject.GetComponent<CubeDemolisher>();
            if (demolisher != null)
            {
                demolisher.Demolish();
            }

            Destroy(gameObject);
        }

        // Touche une bouteille de bière
        if (objectWeHit.gameObject.CompareTag("Beer"))
        {
            print("hit a beer bottle");

            BeerBottle beerBottle = objectWeHit.gameObject.GetComponent<BeerBottle>();
            if (beerBottle != null)
            {
                // Calcule la direction de la force en fonction de la trajectoire de la balle
                beerBottle.Explode(transform.forward, 12f);
            }
            CreateBulletImpactEffect(objectWeHit);
            Destroy(gameObject);
        }
    }

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