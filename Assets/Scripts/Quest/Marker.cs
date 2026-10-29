// repere sur la carte : "X", "XX", numero de teleportation (quete 10), "Mine"...
public class Marker
{
    public string label;
    public int x;
    public int y;

    public Marker(string label, int x, int y)
    {
        this.label = label;
        this.x = x;
        this.y = y;
    }
}
