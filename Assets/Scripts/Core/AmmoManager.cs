using TMPro;
using UnityEngine;

public class AmmoManager : MonoBehaviour
{
    // Bug corrigé : le type était GlobalReferences (copié-collé), il doit être AmmoManager
    public static AmmoManager Instance { get; set; }

    // UI
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