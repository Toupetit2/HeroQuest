public class Tile
{
    public int x;
    public int y;
    public string zone; // "." = couloir, "S1".."S22" = salle
    public bool isWalkable = true;

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
}
