// une porte est sur le bord "side" de la case (x, y), elle remplace un mur
public class Door
{
    public int x;
    public int y;
    public Side side;
    public bool isOpen;
    public bool isSecret;
    public bool isFound;    // porte secrete trouvee par une recherche
    public bool isFalse;    // fausse porte : on la voit, mais elle ne s'ouvre jamais (quete 12)

    public Door(int x, int y, Side side, bool isSecret)
    {
        this.x = x;
        this.y = y;
        this.side = side;
        this.isSecret = isSecret;
    }

    // vrai si la porte est entre les cases (x1, y1) et (x2, y2), dans un sens ou dans l'autre
    public bool IsBetween(int x1, int y1, int x2, int y2)
    {
        int nx = x;
        int ny = y;
        if (side == Side.North) ny--;
        if (side == Side.South) ny++;
        if (side == Side.East) nx++;
        if (side == Side.West) nx--;

        bool sameOrder = x1 == x && y1 == y && x2 == nx && y2 == ny;
        bool otherOrder = x2 == x && y2 == y && x1 == nx && y1 == ny;
        return sameOrder || otherOrder;
    }
}
