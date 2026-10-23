using System;

public class Map
{
    public int width;
    public int height;
    public Tile[,] tiles;

    public Map(string text)
    {
        string[] lines = text.Replace("\r", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        height = lines.Length;
        width = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        tiles = new Tile[width, height];

        for (int y = 0; y < height; y++)
        {
            string[] cells = lines[y].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int x = 0; x < width; x++)
                tiles[x, y] = new Tile(x, y, cells[x]);
        }
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public Tile Get(int x, int y)
    {
        if (!InBounds(x, y)) return null;
        return tiles[x, y];
    }

    // mur entre deux cases voisines si elles ne sont pas dans la meme zone
    public bool HasWall(int x1, int y1, int x2, int y2)
    {
        if (!InBounds(x1, y1) || !InBounds(x2, y2)) return true;
        return tiles[x1, y1].zone != tiles[x2, y2].zone;
    }
}
