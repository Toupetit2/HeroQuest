using UnityEngine;

public class BoardBuilder : MonoBehaviour
{
    public TextAsset boardFile;
    public float cellSize = 2f;
    public float wallHeight = 2.5f;
    public float wallThickness = 0.2f;
    public float floorThickness = 0.1f;

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
                if (map.HasWall(x, y, x + 1, y)) PlaceWall(x, y, "E");
                if (map.HasWall(x, y, x, y + 1)) PlaceWall(x, y, "S");

                // bords nord et ouest du plateau
                if (y == 0) PlaceWall(x, y, "N");
                if (x == 0) PlaceWall(x, y, "W");
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

        // FitInBox pose le bas a y = 0, on descend le sol pour que son dessus soit a y = 0
        floor.transform.SetParent(transform);
        floor.transform.position = CellToWorld(t.x, t.y) - new Vector3(0f, floorThickness, 0f);
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

    void PlaceWall(int x, int y, string side)
    {
        Vector3 pos = CellToWorld(x, y);
        float h = cellSize * 0.5f;
        float rotY = 0f;

        if (side == "E") { pos += new Vector3(h, 0f, 0f); rotY = 90f; }
        if (side == "W") { pos += new Vector3(-h, 0f, 0f); rotY = 90f; }
        if (side == "S") { pos += new Vector3(0f, 0f, -h); }
        if (side == "N") { pos += new Vector3(0f, 0f, h); }

        // conteneur vide, cree a l'origine sans rotation pour mesurer le modele
        GameObject wall = new GameObject("Wall_" + x + "_" + y + "_" + side);

        GameObject model;
        if (wallPrefab != null) model = Instantiate(wallPrefab);
        else model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.transform.SetParent(wall.transform, false);

        // le modele prend la taille du mur et se pose sur le sol, quel que soit son pivot
        FitInBox(wall, model, new Vector3(cellSize, wallHeight, wallThickness));

        // on place ensuite le conteneur sur le bord de la case
        wall.transform.SetParent(transform);
        wall.transform.position = pos;
        wall.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
    }

    // redimensionne le modele pour qu'il remplisse une boite de taille "size",
    // centree en X et Z et posee sur y = 0
    // on met l'echelle sur le conteneur (a l'origine, sans rotation) et pas sur le modele :
    // un modele importe peut etre tourne (ex : -90 en X depuis Blender), ses axes ne seraient plus ceux du monde
    void FitInBox(GameObject container, GameObject model, Vector3 size)
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
