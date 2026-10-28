public class Trap
{
    public TrapType type;
    public int x;
    public int y;
    public TrapState state = TrapState.Cache;

    public Trap(TrapType type, int x, int y)
    {
        this.type = type;
        this.x = x;
        this.y = y;
    }
}
