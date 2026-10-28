using UnityEngine;

public class BoardBuilder : MonoBehaviour
{
    public TextAsset boardFile;
    public float cellSize = 2f;
    public float wallHeight = 2.5f;
    public float wallThickness = 0.2f;

    // prefabs avec le pivot en bas au centre, mur oriente le long de X
    // laisser vide = cubes de secours
    public GameObject floorPrefab;
    public GameObject wallPrefab;

    public Material roomMaterial;
    public Material corridorMaterial;

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
        GameObject go;
        if (floorPrefab != null)
        {
            go = Instantiate(floorPrefab, transform);
            go.transform.position = CellToWorld(t.x, t.y);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(transform);
            go.transform.position = CellToWorld(t.x, t.y) + new Vector3(0f, -0.05f, 0f);
            go.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
        }
        go.name = "Floor_" + t.x + "_" + t.y + "_" + t.zone;

        Material m = t.IsCorridor() ? corridorMaterial : roomMaterial;
        if (m != null) go.GetComponentInChildren<Renderer>().sharedMaterial = m;
    }

    void PlaceWall(int x, int y, string side)
    {
        Vector3 c = CellToWorld(x, y);
        float h = cellSize * 0.5f;
        Vector3 pos = c;
        float rotY = 0f;

        if (side == "E") { pos += new Vector3(h, 0f, 0f); rotY = 90f; }
        if (side == "W") { pos += new Vector3(-h, 0f, 0f); rotY = 90f; }
        if (side == "S") { pos += new Vector3(0f, 0f, -h); }
        if (side == "N") { pos += new Vector3(0f, 0f, h); }

        GameObject go;
        if (wallPrefab != null)
        {
            go = Instantiate(wallPrefab, pos, Quaternion.Euler(0f, rotY, 0f), transform);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(transform);
            go.transform.position = pos + new Vector3(0f, wallHeight * 0.5f, 0f);
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            go.transform.localScale = new Vector3(cellSize, wallHeight, wallThickness);
        }
        go.name = "Wall_" + x + "_" + y + "_" + side;
    }
}
