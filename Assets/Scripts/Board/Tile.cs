public class Tile
{
    public int x;
    public int y;
    public string zone; // "." = couloir, "S1".."S22" = salle

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


    // state and behavior of the tile can be added here, such as whether it is occupied, what type of tile it is, etc.
    
}
