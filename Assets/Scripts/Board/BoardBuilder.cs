using System.Collections.Generic;
using UnityEngine;

public class BoardBuilder : MonoBehaviour
{
    public TextAsset boardFile;
    public float cellSize = 2f;
    public float wallHeight = 2.5f;
    public float wallThickness = 0.2f;
    public float floorThickness = 0.1f;

    // les murs descendent un peu sous le sol : aucun jour possible entre le mur et la dalle
    public float wallSink = 0.05f;

    // le mur doit etre oriente le long de X ; taille et pivot sont ajustes automatiquement (sols aussi)
    // laisser vide = cubes de secours
    public GameObject floorPrefab;
    public GameObject wallPrefab;

    public Material roomMaterial;
    public Material corridorMaterial;

    // sol particulier pour une zone ("." = couloir, "S1".."S22" = salle)
    // une zone absente de la liste garde le sol par defaut
    public ZoneStyle[] zoneStyles;

    public Map map;

    // chaque mur est range sous ses deux noms : "5_3_E" et "6_3_W" donnent le meme mur
    Dictionary<string, GameObject> walls = new Dictionary<string, GameObject>();

    // sommets de la grille ou un pilier de coin a deja ete pose (evite les doublons)
    HashSet<long> posts = new HashSet<long>();

    [ContextMenu("Generate")]
    public void Generate()
    {
        Clear();
        map = new Map(boardFile.text);

        for (int y = 0; y < map.height; y++)
        {
            for (int x = 0; x < map.width; x++)
            {
                Tile t = map.Get(x, y);
                PlaceFloor(t);

                // on ne teste que l'est et le sud pour ne pas poser deux fois le meme mur
                if (map.HasWall(x, y, x + 1, y)) PlaceWall(x, y, Side.East);
                if (map.HasWall(x, y, x, y + 1)) PlaceWall(x, y, Side.South);

                // bords nord et ouest du plateau
                if (y == 0) PlaceWall(x, y, Side.North);
                if (x == 0) PlaceWall(x, y, Side.West);
            }
        }

        Debug.Log("Plateau genere : " + map.width + " x " + map.height);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
        walls.Clear();
        posts.Clear();
    }

    // le mur sur le bord "side" de la case (x, y), ou null s'il n'y en a pas
    public GameObject GetWall(int x, int y, Side side)
    {
        GameObject wall;
        walls.TryGetValue(WallKey(x, y, side), out wall);
        return wall;
    }

    string WallKey(int x, int y, Side side)
    {
        return x + "_" + y + "_" + side;
    }

    // range le mur sous son nom, et sous le nom du meme bord vu depuis la case voisine
    void RegisterWall(int x, int y, Side side, GameObject wall)
    {
        walls[WallKey(x, y, side)] = wall;

        if (side == Side.East) walls[WallKey(x + 1, y, Side.West)] = wall;
        if (side == Side.West) walls[WallKey(x - 1, y, Side.East)] = wall;
        if (side == Side.South) walls[WallKey(x, y + 1, Side.North)] = wall;
        if (side == Side.North) walls[WallKey(x, y - 1, Side.South)] = wall;
    }

    // centre de la case en coordonnees monde (y du plateau vers -Z)
    public Vector3 CellToWorld(int x, int y)
    {
        return transform.position + new Vector3(x * cellSize, 0f, -y * cellSize);
    }

    void PlaceFloor(Tile t)
    {
        ZoneStyle style = FindStyle(t.zone);

        // prefab : celui de la zone, sinon celui par defaut, sinon un cube
        GameObject prefab = floorPrefab;
        if (style != null && style.floorPrefab != null) prefab = style.floorPrefab;

        // conteneur vide, cree a l'origine sans rotation pour mesurer le modele
        GameObject floor = new GameObject("Floor_" + t.x + "_" + t.y + "_" + t.zone);

        GameObject model;
        if (prefab != null) model = Instantiate(prefab);
        else model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.transform.SetParent(floor.transform, false);

        FitInBox(floor, model, new Vector3(cellSize, floorThickness, cellSize));

        // materiau : celui de la zone s'il existe ;
        // sinon un prefab propre a la zone garde ses couleurs, et le reste prend salle / couloir
        Material m = null;
        if (style != null && style.material != null) m = style.material;
        else if (style == null || style.floorPrefab == null) m = t.IsCorridor() ? corridorMaterial : roomMaterial;

        if (m != null)
        {
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = m;
        }

        floor.transform.SetParent(transform);
        floor.transform.position = CellToWorld(t.x, t.y);

        // on mesure le sol reel et on cale son DESSUS exactement au niveau du plateau :
        // une dalle plate (epaisseur 0) ou plus fine que prevu laissait sinon un vide sous les murs
        Bounds fb = GetBounds(floor);
        if (fb.size != Vector3.zero)
            floor.transform.position += new Vector3(0f, transform.position.y - fb.max.y, 0f);
    }

    // renvoie le style de la zone, ou null si elle n'en a pas
    ZoneStyle FindStyle(string zone)
    {
        if (zoneStyles == null) return null;
        foreach (ZoneStyle s in zoneStyles)
        {
            if (s.zone == zone) return s;
        }
        return null;
    }

    void PlaceWall(int x, int y, Side side)
    {
        Vector3 pos = CellToWorld(x, y);
        float h = cellSize * 0.5f;
        float rotY = 0f;

        if (side == Side.East) { pos += new Vector3(h, 0f, 0f); rotY = 90f; }
        if (side == Side.West) { pos += new Vector3(-h, 0f, 0f); rotY = 90f; }
        if (side == Side.South) { pos += new Vector3(0f, 0f, -h); }
        if (side == Side.North) { pos += new Vector3(0f, 0f, h); }

        // conteneur vide, cree a l'origine sans rotation pour mesurer le modele
        GameObject wall = new GameObject("Wall_" + x + "_" + y + "_" + side);

        GameObject model;
        if (wallPrefab != null) model = Instantiate(wallPrefab);
        else model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.transform.SetParent(wall.transform, false);

        // le modele prend la taille du mur et se pose sur le sol, quel que soit son pivot
        // (il est un peu plus haut et enfonce de wallSink dans le sol)
        FitInBox(wall, model, new Vector3(cellSize, wallHeight + wallSink, wallThickness));

        // on place ensuite le conteneur sur le bord de la case
        wall.transform.SetParent(transform);
        wall.transform.position = pos - new Vector3(0f, wallSink, 0f);
        wall.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        RegisterWall(x, y, side, wall);

        // un pilier a chaque bout : comble le coin vide ou deux murs se rejoignent
        // sommets en demi-cases : l'est de (x, y) touche (2x+1, 2y-1) et (2x+1, 2y+1), etc.
        if (side == Side.East || side == Side.West)
        {
            int vx = side == Side.East ? 2 * x + 1 : 2 * x - 1;
            PlacePost(vx, 2 * y - 1);
            PlacePost(vx, 2 * y + 1);
        }
        else
        {
            int vy = side == Side.South ? 2 * y + 1 : 2 * y - 1;
            PlacePost(2 * x - 1, vy);
            PlacePost(2 * x + 1, vy);
        }
    }

    // petit pilier a un sommet de la grille ; vx et vy sont en demi-cases
    void PlacePost(int vx, int vy)
    {
        long key = ((long)(vx + 1000) << 32) | (long)(vy + 1000);
        if (!posts.Add(key)) return;

        // un peu plus large et plus haut que les murs : il les recouvre sans faire de scintillement
        float margin = 0.01f;
        float side = wallThickness + margin;
        float height = wallHeight + wallSink + margin;

        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "Post_" + vx + "_" + vy;

        // meme materiau que les murs, pour que le coin ait la meme pierre
        Material wallMaterial = null;
        if (wallPrefab != null)
        {
            Renderer wr = wallPrefab.GetComponentInChildren<Renderer>(true);
            if (wr != null) wallMaterial = wr.sharedMaterial;
        }
        if (wallMaterial != null) post.GetComponent<Renderer>().sharedMaterial = wallMaterial;

        // la boite physique du cube est inutile : les murs ont deja leurs collisions
        Collider c = post.GetComponent<Collider>();
        if (c != null)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }

        post.transform.SetParent(transform);
        post.transform.localScale = new Vector3(side, height, side);
        post.transform.position = transform.position
            + new Vector3(vx * cellSize * 0.5f, -wallSink - margin * 0.5f + height * 0.5f, -vy * cellSize * 0.5f);
    }

    // redimensionne le modele pour qu'il remplisse une boite de taille "size",
    // centree en X et Z et posee sur y = 0
    // on met l'echelle sur le conteneur (a l'origine, sans rotation) et pas sur le modele :
    // un modele importe peut etre tourne (ex : -90 en X depuis Blender), ses axes ne seraient plus ceux du monde
    public void FitInBox(GameObject container, GameObject model, Vector3 size)
    {
        Bounds b = GetBounds(model);
        if (b.size == Vector3.zero)
        {
            Debug.LogWarning(model.name + " n'a pas de Renderer, impossible de le mesurer");
            return;
        }

        // si un axe est plat (ex : un Quad), on ne le redimensionne pas
        float sx = b.size.x > 0f ? size.x / b.size.x : 1f;
        float sy = b.size.y > 0f ? size.y / b.size.y : 1f;
        float sz = b.size.z > 0f ? size.z / b.size.z : 1f;
        container.transform.localScale = new Vector3(sx, sy, sz);

        // on decale le modele pour que le bas touche le sol et que le centre soit sur le pivot
        b = GetBounds(model);
        model.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
    }

    // boite qui englobe tous les Renderer de l'objet et de ses enfants
    Bounds GetBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds();

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }
}
