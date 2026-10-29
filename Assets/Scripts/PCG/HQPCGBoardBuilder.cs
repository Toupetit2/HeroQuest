using UnityEngine;

// Construit une représentation 3D de board.txt, SANS modifier le fichier Git.
// Le sol et les murs restent fixes ; HeroQuestSimplePCG ne change que leur contenu.
[DefaultExecutionOrder(-200)]
public class HQPCGBoardBuilder : MonoBehaviour
{
    public TextAsset boardFile;
    public float cellSize = 2f;
    public Vector3 firstCellLocalCenter = new Vector3(-25f, 0f, -18f);
    public GameObject floorPrefab;   // Facultatif
    public GameObject wallPrefab;    // Facultatif

    private const string GroundName = "Ground";
    private const string WallName = "Walls";
    private Material floorMaterial;
    private Material corridorMaterial;
    private Material wallMaterial;

    // Cree les dalles et les murs si la scene n'en contient pas deja.
    private void Awake()
    {
        BuildBoard();
    }

    [ContextMenu("Build board from Git map")]
    public void BuildBoard()
    {
        if (transform.Find(GroundName) != null && transform.Find(WallName) != null)
            return; // Ne pas dupliquer le plateau.
        if (boardFile == null)
        {
            Debug.LogError("BoardBuilder: assign Git Assets/Data/Board/board.txt", this);
            return;
        }
        Map map;
        try { map = new Map(boardFile.text); }
        catch (System.Exception ex)
        {
            Debug.LogError("BoardBuilder: invalid map: " + ex.Message, this);
            return;
        }
        if (map.width != 26 || map.height != 19)
        {
            Debug.LogError("BoardBuilder: expected 26x19.", this);
            return;
        }
        Transform ground = new GameObject(GroundName).transform;
        ground.SetParent(transform, false);
        Transform walls = new GameObject(WallName).transform;
        walls.SetParent(transform, false);
        floorMaterial = NewMaterial(new Color(0.57f, 0.55f, 0.51f));
        corridorMaterial = NewMaterial(new Color(0.45f, 0.43f, 0.4f));
        wallMaterial = NewMaterial(new Color(0.23f, 0.25f, 0.3f));

        for (int y = 0; y < map.height; y++)
        for (int x = 0; x < map.width; x++)
        {
            // Map.y == 0 en haut du TXT ; z du monde augmente du bas vers le haut.
            Vector3 center = firstCellLocalCenter +
                new Vector3(x * cellSize, 0, (map.height - 1 - y) * cellSize);
            string zone = map.Get(x, y).zone;
            CreatePart("Tile_" + x + "_" + y, ground, center + Vector3.down * 0.11f,
                new Vector3(cellSize, 0.2f, cellSize), floorPrefab,
                zone == "." ? corridorMaterial : floorMaterial);

            // Une seule fois chaque frontiere interieure ; les bords exterieurs
            // sont ajoutes separement. Map.HasWall vient directement de Git.
            if (x == 0) CreateWall(walls, center + Vector3.left * cellSize * 0.5f, true);
            if (y == 0) CreateWall(walls, center + Vector3.forward * cellSize * 0.5f, false);
            if (map.HasWall(x, y, x + 1, y))
                CreateWall(walls, center + Vector3.right * cellSize * 0.5f, true);
            if (map.HasWall(x, y, x, y + 1))
                CreateWall(walls, center + Vector3.back * cellSize * 0.5f, false);
        }
    }

    // Cree un mur centre ENTRE deux cases (le generateur de portes cherche ce centre).
    private void CreateWall(Transform parent, Vector3 localMidpoint, bool acrossX)
    {
        Vector3 size = acrossX ?
            new Vector3(0.18f, 2f, cellSize) : new Vector3(cellSize, 2f, 0.18f);
        CreatePart("Wall", parent, localMidpoint + Vector3.up, size, wallPrefab, wallMaterial);
    }

    private GameObject CreatePart(string name, Transform parent, Vector3 localPosition,
                                  Vector3 size, GameObject prefab, Material fallbackMaterial)
    {
        GameObject go;
        if (prefab != null)
            go = Instantiate(prefab, parent);
        else
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        // L'echelle et le pivot des modeles importes se reglent dans leur prefab.
        if (prefab == null)
        {
            go.transform.localScale = size;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && fallbackMaterial != null) r.sharedMaterial = fallbackMaterial;
        }
        return go;
    }

    private Material NewMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            if (floorMaterial != null) Destroy(floorMaterial);
            if (corridorMaterial != null) Destroy(corridorMaterial);
            if (wallMaterial != null) Destroy(wallMaterial);
        }
    }
}
