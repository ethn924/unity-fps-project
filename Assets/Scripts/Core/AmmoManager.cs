using TMPro;
using UnityEngine;

// Singleton pour mettre à jour l'affichage de l'UI des munitions
public class AmmoManager : MonoBehaviour
{
    public static AmmoManager Instance { get; set; }

    public TextMeshProUGUI ammoDisplay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
}
