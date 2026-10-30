using System;
using System.Collections.Generic;
using UnityEngine;

// champ de vision des heros (logique seulement, aucun affichage)
// une case est vue si, depuis la case d'un heros, la ligne droite jusqu'a elle ne traverse
// ni mur, ni porte fermee (ou secrete non trouvee, ou fausse), ni case bloquee
// les meubles et les monstres ne cachent rien
public static class QuestVision
{
    // renvoie un tableau [x, y] : vrai si la case est vue par au moins un des heros
    public static bool[,] Compute(GameState gameState, List<Vector2Int> viewers)
    {
        Map map = gameState.map;
        bool[,] seen = new bool[map.width, map.height];
        HashSet<long> openDoors = OpenDoorEdges(gameState);

        foreach (Vector2Int v in viewers)
        {
            if (!map.InBounds(v.x, v.y)) continue;

            for (int y = 0; y < map.height; y++)
            {
                for (int x = 0; x < map.width; x++)
                {
                    if (seen[x, y]) continue;
                    if (HasLineOfSight(map, openDoors, v.x, v.y, x, y)) seen[x, y] = true;
                }
            }
        }
        return seen;
    }

    // les bords (entre deux cases) ou une porte ouverte laisse passer le regard
    static HashSet<long> OpenDoorEdges(GameState gameState)
    {
        HashSet<long> edges = new HashSet<long>();
        if (gameState.doorList == null) return edges;

        foreach (Door d in gameState.doorList)
        {
            if (!d.isOpen || d.isFalse) continue;
            if (d.isSecret && !d.isFound) continue;

            int nx = d.x;
            int ny = d.y;
            if (d.side == Side.North) ny--;
            if (d.side == Side.South) ny++;
            if (d.side == Side.East) nx++;
            if (d.side == Side.West) nx--;
            edges.Add(Edge(d.x, d.y, nx, ny));
        }
        return edges;
    }

    // meme cle dans les deux sens
    static long Edge(int ax, int ay, int bx, int by)
    {
        long a = ax * 64 + ay;
        long b = bx * 64 + by;
        return a < b ? a * 4096 + b : b * 4096 + a;
    }

    // peut-on voir de la case (ax, ay) a la case voisine (bx, by) ?
    static bool CanPass(Map map, HashSet<long> openDoors, int ax, int ay, int bx, int by)
    {
        if (!map.InBounds(bx, by)) return false;
        if (!map.HasWall(ax, ay, bx, by)) return true;
        return openDoors.Contains(Edge(ax, ay, bx, by));
    }

    // un pas de la ligne : droit, ou en diagonale (il suffit qu'un des deux chemins en L soit libre)
    static bool CanStep(Map map, HashSet<long> openDoors, int px, int py, int dx, int dy)
    {
        int qx = px + dx;
        int qy = py + dy;
        if (dx == 0 || dy == 0) return CanPass(map, openDoors, px, py, qx, qy);

        bool viaX = CanPass(map, openDoors, px, py, qx, py) && CanPass(map, openDoors, qx, py, qx, qy);
        bool viaY = CanPass(map, openDoors, px, py, px, qy) && CanPass(map, openDoors, px, qy, qx, qy);
        return viaX || viaY;
    }

    // ligne de Bresenham de la case de depart a la case cible
    static bool HasLineOfSight(Map map, HashSet<long> openDoors, int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        int x = x0;
        int y = y0;

        while (x != x1 || y != y1)
        {
            int px = x;
            int py = y;
            int e2 = 2 * err;
            int stepX = 0;
            int stepY = 0;
            if (e2 >= dy) { err += dy; x += sx; stepX = sx; }
            if (e2 <= dx) { err += dx; y += sy; stepY = sy; }

            if (!CanStep(map, openDoors, px, py, stepX, stepY)) return false;

            // une case bloquee se voit, mais on ne voit rien derriere
            bool isTarget = x == x1 && y == y1;
            if (!isTarget && map.tiles[x, y].blocked) return false;
        }
        return true;
    }
}
