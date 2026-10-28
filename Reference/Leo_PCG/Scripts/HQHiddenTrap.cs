using UnityEngine;

// Un piege reste present dans les donnees meme lorsqu'il est invisible.
// La detection et les degats seront relies plus tard au gameplay du groupe Git.
public class HQHiddenTrap : MonoBehaviour
{
    public int trapId;
    public Vector2Int cell;
    public bool isRevealed;
    public bool isTriggered;

    // Memorise l'identifiant et la case, sans reveler le piege au heros.
    public void Initialize(int id, Vector2Int cellPosition, bool revealForTesting)
    {
        trapId = id;
        cell = cellPosition;
        isTriggered = false;
        SetVisible(revealForTesting);
    }

    // La recherche de pieges devra appeler cette fonction.
    public void Reveal()
    {
        SetVisible(true);
    }

    // Point d'integration futur pour appliquer les regles de degats.
    public void Trigger()
    {
        isTriggered = true;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        isRevealed = visible;
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
        // La collision ne bloque jamais le passage d'un piege cache.
        foreach (Collider c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
    }
}
