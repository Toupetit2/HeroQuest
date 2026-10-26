using UnityEngine;

// Doors start CLOSED. Once opened, they stay open for this quest (rulebook p. 12).
// Secret doors stay visually and physically a WALL until discovered by searching.
public class HQGeneratedDoor : MonoBehaviour
{
    public GameObject originalWall;
    public HeroQuestSimplePCG dungeon;
    public int zoneA;
    public int zoneB;
    public bool isSecret;
    public bool isDiscovered;
    public bool isOpen;

    public void Initialize(HeroQuestSimplePCG owner, GameObject wall,
                           int a, int b, bool secret)
    {
        dungeon = owner;
        originalWall = wall;
        zoneA = a;
        zoneB = b;
        isSecret = secret;
        isOpen = false;

        // CLOSED NORMAL DOOR: visible and blocking movement. Never requires a search.
        // SECRET DOOR: looks like the original wall until a search discovers it.
        // Fog-of-war / line of sight will be a separate system in a later version.
        isDiscovered = !secret;
        if (originalWall != null) originalWall.SetActive(secret);
        SetVisuals(!secret);
    }

    // Your future hero line-of-sight system should call this for ordinary doors.
    public void RevealDoor()
    {
        if (isSecret) return; // A secret door REQUIRES a search, not normal line of sight.
        isDiscovered = true;
        if (originalWall != null) originalWall.SetActive(false);
        SetVisuals(!isOpen);
    }

    // Call ONLY after a successful search for secret doors.
    [ContextMenu("Discover secret door (test)")]
    public void DiscoverSecretDoor()
    {
        if (!isSecret || isDiscovered) return;
        isDiscovered = true;
        if (originalWall != null) originalWall.SetActive(false);
        SetVisuals(true);
    }

    [ContextMenu("Open door")]
    public void Open()
    {
        if (isOpen || !isDiscovered) return;
        isOpen = true;
        if (originalWall != null) originalWall.SetActive(false);
        SetVisuals(false); // No obstruction, as if replaced with an open doorway.
        if (dungeon != null)
        {
            dungeon.RevealRoom(zoneA);
            dungeon.RevealRoom(zoneB);
        }
    }

    // The original rules do NOT allow an opened door to be closed again.
    private void SetVisuals(bool visible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
        foreach (Collider c in GetComponentsInChildren<Collider>(true)) c.enabled = visible;
    }

    private void OnMouseDown()
    {
        if (isDiscovered && !isOpen) Open(); // Interactive prototype only.
    }
}
