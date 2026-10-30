using System;
using System.Collections.Generic;
using UnityEngine;

// positions de depart des 4 heros (H1 a H4) quand la quete n'en impose pas :
// partagees par l'editeur de quetes et par le QuestLoader pour qu'elles soient identiques
public static class HeroStartLayout
{
    public const int Count = 4;

    // quart de l'escalier occupe par chaque heros, escalier non tourne (comme sur l'image de l'escalier) :
    // H1 en bas a droite, H2 en bas a gauche, H3 en haut a droite, H4 en haut a gauche
    // 0 = gauche / haut, 1 = droite / bas
    static readonly int[] quarterX = { 1, 0, 1, 0 };
    static readonly int[] quarterY = { 1, 1, 0, 0 };

    // sur l'escalier : les heros tournent avec l'image (rotation 0, 90, 180 ou 270, sens des aiguilles d'une montre)
    public static Vector2Int[] OnStairs(int x, int y, int width, int height, int rotation)
    {
        int turns = (((rotation % 360) + 360) % 360) / 90;
        Vector2Int[] cells = new Vector2Int[Count];

        for (int i = 0; i < Count; i++)
        {
            int qx = quarterX[i];
            int qy = quarterY[i];
            for (int t = 0; t < turns; t++)
            {
                // un quart de tour dans le sens des aiguilles d'une montre : haut-gauche -> haut-droite -> bas-droite -> bas-gauche
                int nx = 1 - qy;
                int ny = qx;
                qx = nx;
                qy = ny;
            }
            cells[i] = new Vector2Int(x + (qx == 0 ? 0 : Mathf.Max(0, width - 1)),
                                      y + (qy == 0 ? 0 : Mathf.Max(0, height - 1)));
        }
        return cells;
    }

    // dans une salle : les 4 premieres cases libres, de gauche a droite puis de haut en bas
    public static Vector2Int[] InRoom(int mapWidth, int mapHeight, Func<int, int, bool> isFree)
    {
        List<Vector2Int> found = new List<Vector2Int>();
        for (int y = 0; y < mapHeight && found.Count < Count; y++)
            for (int x = 0; x < mapWidth && found.Count < Count; x++)
                if (isFree(x, y)) found.Add(new Vector2Int(x, y));

        // salle trop petite ou introuvable : les heros manquants se placent sur la derniere case trouvee
        Vector2Int fallback = found.Count > 0 ? found[found.Count - 1] : Vector2Int.zero;
        while (found.Count < Count) found.Add(fallback);
        return found.ToArray();
    }
}
