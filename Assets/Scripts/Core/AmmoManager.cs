using TMPro;
using UnityEngine;

// Ce script gère l'accès global au texte d'affichage des munitions (HUD).
// Il utilise le patron de conception Singleton (Instance unique) pour que n'importe quel autre script 
// (comme l'arme) puisse facilement mettre à jour le texte à l'écran.
public class AmmoManager : MonoBehaviour
{
    // L'unique instance du manager accessible publiquement depuis n'importe où
    // (Exemple d'utilisation dans un autre script : AmmoManager.Instance.ammoDisplay.text = "...")
    public static AmmoManager Instance { get; set; }

    [Header("Référence UI")]
    [Tooltip("Faites glisser ici le composant TextMeshProUGUI du Canvas servant à afficher les munitions.")]
    public TextMeshProUGUI ammoDisplay;

    private void Awake()
    {
        // Patron Singleton : s'assure qu'il n'existe qu'un seul AmmoManager actif dans toute la scène.
        // Si une copie existe déjà, on la détruit pour éviter les conflits de références.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this; // Enregistre cette instance comme la référence globale
        }
    }
}
