using UnityEngine;

// Identifies a generated item without relying on its GameObject name or prefab.
// The game rules can later inspect this when a hero searches or interacts.
public class HQPlacedPiece : MonoBehaviour
{
    public HeroQuestSimplePCG.ContentKind kind;
    public int id; // The Dungeon props and Dungeon ID lists are separate by kind.
    public int roomId;
    public Vector2Int anchor;
    public Vector2Int footprint;
    public bool isQuestObjective;
}
