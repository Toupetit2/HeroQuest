public class Tile
{
    public int x;
    public int y;
    public string zone; // "." = couloir, "S1".."S22" = salle

    public bool blocked;          // case bloquee ou bloc tombe
    public bool revealed;         // false = cachee par le brouillard
    public Furniture furniture;   // meuble qui couvre la case, null si aucun
    public Trap trap;             // piege sur la case, null si aucun
    public bool rock;             // roche pleine : inaccessible dans cette quete

    public Tile(int x, int y, string zone)
    {
        this.x = x;
        this.y = y;
        this.zone = zone;
    }

    public bool IsCorridor()
    {
        return zone == ".";
    }


    // une fosse reste praticable : le saut et les degats sont geres par le gameplay
    public bool isWalkable()
    {
        return !blocked && furniture == null;
    }
}
