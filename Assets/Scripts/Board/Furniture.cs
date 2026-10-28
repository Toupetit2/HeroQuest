// un meuble couvre un rectangle de cases, (x, y) = case en haut a gauche
public class Furniture
{
    public FurnitureType type;
    public int x;
    public int y;
    public int width;      // en cases, deja tourne
    public int height;
    public int rotation;   // 0, 90, 180 ou 270 : seulement pour l'affichage

    public Furniture(FurnitureType type, int x, int y, int width, int height, int rotation)
    {
        this.type = type;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
        this.rotation = rotation;
    }
}
