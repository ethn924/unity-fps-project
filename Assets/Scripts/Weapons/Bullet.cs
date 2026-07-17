using UnityEngine;

// Ce script est attaché au projectile (la balle) et gère ce qui se passe lors d'un impact physique (collision).
public class Bullet : MonoBehaviour
{
    // Cette fonction de Unity est automatiquement appelée lorsque la balle rentre en collision avec un autre objet
    private void OnCollisionEnter(Collision objectWeHit)
    {
        // CAS 1 : On touche une cible d'entraînement (taguée "Target")
        if (objectWeHit.gameObject.CompareTag("Target"))
        {
            print("hit " + objectWeHit.gameObject.name + " !");
            CreateBulletImpactEffect(objectWeHit); // Génère l'effet visuel d'impact (trou de balle, poussière)
            Destroy(gameObject); // Détruit l'objet de la balle pour ne pas encombrer le jeu
        }

        // CAS 2 : On touche un mur ou un élément de décor (tagué "Wall")
        if (objectWeHit.gameObject.CompareTag("Wall"))
        {
            print("hit a wall");
            CreateBulletImpactEffect(objectWeHit); // Génère l'effet d'impact

            // Si le mur est destructible (possède le script CubeDemolisher), on déclenche sa démolition
            CubeDemolisher demolisher = objectWeHit.gameObject.GetComponent<CubeDemolisher>();
            if (demolisher != null)
            {
                demolisher.Demolish();
            }

            Destroy(gameObject); // Détruit la balle après impact
        }

        // CAS 3 : On touche une bouteille de bière en verre (taguée "Beer")
        if (objectWeHit.gameObject.CompareTag("Beer"))
        {
            print("hit a beer bottle");

            // Récupère le script de la bouteille
            BeerBottle beerBottle = objectWeHit.gameObject.GetComponent<BeerBottle>();
            if (beerBottle != null)
            {
                // Fait exploser/tomber la bouteille en appliquant une force physique dans la direction de la balle.
                // transform.forward représente la direction exacte dans laquelle se déplace la balle.
                // 12f correspond à l'intensité de la force d'impact (poussée).
                beerBottle.Explode(transform.forward, 12f);
            }
            
            CreateBulletImpactEffect(objectWeHit); // Génère l'effet d'impact
            Destroy(gameObject); // Détruit la balle pour éviter qu'elle ne rebondisse à l'infini
        }
    }

    // Instancie l'effet visuel (trou de balle / étincelles / poussière) au point exact de l'impact
    void CreateBulletImpactEffect(Collision objectWeHit)
    {
        // Récupère le premier point de contact de la collision
        ContactPoint contact = objectWeHit.contacts[0];

        // Instancie l'effet visuel à la position du contact, orienté selon la normale de la surface (la direction perpendiculaire à la surface touchée)
        GameObject hole = Instantiate(
            GlobalReferences.Instance.bulletImpactEffectPrefab,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );

        // Attache l'effet visuel en tant qu'enfant de l'objet touché pour qu'il le suive s'il bouge
        hole.transform.SetParent(objectWeHit.gameObject.transform);
    }
}