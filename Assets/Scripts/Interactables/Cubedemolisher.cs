using Hanzzz.MeshDemolisher;
using System.Collections.Generic;
using UnityEngine;

public class CubeDemolisher : MonoBehaviour
{
    [Header("Points de cassure")]
    public List<Transform> breakPoints = new List<Transform>();

    [Header("Matériau")]
    public Material interiorMaterial;

    [Header("Physique")]
    public float explosionForce = 5f;
    public float explosionRadius = 1f;

    private bool alreadyExploded = false;

    public void Demolish()
    {
        if (alreadyExploded) return;
        alreadyExploded = true;

        MeshDemolisher meshDemolisher = new MeshDemolisher();

        bool isValid = meshDemolisher.VerifyDemolishInput(gameObject, breakPoints);
        if (!isValid)
        {
            Debug.LogWarning("CubeDemolisher : mesh ou breakPoints invalides.");
            return;
        }

        List<GameObject> brokenPieces = meshDemolisher.Demolish(gameObject, breakPoints, interiorMaterial);

        foreach (GameObject piece in brokenPieces)
        {
            piece.transform.position = transform.position;
            piece.transform.rotation = transform.rotation;
            piece.transform.localScale = transform.localScale;

            Rigidbody rb = piece.AddComponent<Rigidbody>();
            MeshCollider collider = piece.AddComponent<MeshCollider>();
            collider.convex = true;

            rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
        }

        Destroy(gameObject);
    }
}