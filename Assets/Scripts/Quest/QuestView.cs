using System;
using System.Collections.Generic;
using UnityEngine;

// affiche le contenu d'une quete a partir du GameState (aucune logique ici)
// a mettre sur le meme GameObject que le BoardBuilder
// laisser un prefab vide = cube de couleur
public class QuestView : MonoBehaviour
{
    public BoardBuilder board;

    public GameObject doorPrefab;
    public GameObject secretDoorPrefab;
    public GameObject blockedPrefab;
    public GameObject stairsPrefab;
    public Texture2D stairsTexture;   // optionnel : image de l'escalier (sinon Resources/HeroQuest/StairsTexture.png)
    public GameObject allyPrefab;
    public GameObject trapPrefab;
    public FurniturePrefab[] furniturePrefabs;
    public MonsterPrefab[] monsterPrefabs;

    public float doorHeight = 2f;
    public float doorThickness = 0.3f;
    public float blockedHeight = 2.5f;

    // brouillard : tout ce qui n'est pas dans le champ de vision des heros est cache
    // (la roche inaccessible n'est jamais vue, donc toujours sous le brouillard)
    // c'est le shader "Dynamic Vertical Fog" : un plan transparent qui voile tout ce qui est SOUS lui
    // il faut une camera Perspective ; la texture de profondeur est activee ici sur la camera principale
    public bool useFog = true;
    public Material fogMaterialAsset;        // DynamicVerticalFogMaterial ; vide = Resources/HeroQuest/DynamicVerticalFogMaterial
    public float fogBelowWall = 0.3f;        // le plan est un peu sous le haut des murs : on voit ou sont les murs
    public Color fogColor = new Color(0.5f, 0.55f, 0.62f, 1f);   // l'alpha regle la transparence du brouillard
    public float fogDensity = 4f;            // epaisseur : plus c'est haut, plus le brouillard est opaque
    public float fogNoise = 6f;              // taille des nuages (plus grand = plus fin)
    public float fogSpeed = 0.02f;           // vitesse de deplacement des nuages
    public float stairsHeight = 0.2f;
    public float furnitureHeight = 1f;
    public float monsterHeight = 1.5f;

    // ignore le brouillard pour verifier la quete complete
    public bool showAll;
    // montre les objets caches (portes secretes, monstres caches, pieges) pour verifier la quete
    public bool showHidden;

    public float trapHeight = 0.1f;

    GameState gameState;
    Transform root;

    bool[,] seenCells;                 // cases vues par au moins un heros ; null = pas de heros, on garde les salles "revealed"
    GameObject fogObject;             // un seul maillage : un carre par case sous le brouillard
    Mesh fogMesh;
    Material fogMaterial;
    int lastSignature = int.MinValue; // ce qui compte pour la vue : positions, portes, options

    // chaque objet pose et les cases qu'il couvre (pour le brouillard)
    Dictionary<GameObject, RectInt> placed = new Dictionary<GameObject, RectInt>();
    Dictionary<Monster, GameObject> monsterObjects = new Dictionary<Monster, GameObject>();
    Dictionary<Ally, GameObject> allyObjects = new Dictionary<Ally, GameObject>();
    Dictionary<Door, GameObject> doorObjects = new Dictionary<Door, GameObject>();
    Dictionary<Trap, GameObject> trapObjects = new Dictionary<Trap, GameObject>();
    Dictionary<Color, Material> colorMaterials = new Dictionary<Color, Material>();

    public void Show(GameState gameState)
    {
        Clear();
        this.gameState = gameState;

        root = new GameObject("Quest").transform;
        root.SetParent(transform, false);

        RectInt s = gameState.stairs;
        HQStairsVisual.SetTexture(stairsTexture);
        // l'image Resources/HeroQuest/StairsTexture.png est posee a plat sur l'escalier ; sans image, on garde le prefab
        if (!PlaceTexturedStairs(s, gameState.stairsRotation))
            Place("Stairs", stairsPrefab, s, stairsHeight, 0, Color.green);

        foreach (Door door in gameState.doorList) PlaceDoor(door);

        foreach (Tile tile in gameState.map.tiles)
        {
            if (tile.blocked)
                Place("Blocked", blockedPrefab, new RectInt(tile.x, tile.y, 1, 1), blockedHeight, 0, Color.gray);
        }

        foreach (Furniture f in gameState.furnitureList)
        {
            RectInt rect = new RectInt(f.x, f.y, f.width, f.height);
            Place(f.type.ToString(), FindFurniturePrefab(f.type), rect, furnitureHeight, f.rotation, new Color(0.9f, 0.7f, 0.1f));
        }

        foreach (Monster m in gameState.monsterList)
        {
            RectInt rect = new RectInt(m.position.x, m.position.y, 1, 1);
            GameObject go = Place(m.data.monsterClass.ToString(), FindMonsterPrefab(m.data.monsterClass), rect, monsterHeight, 0, Color.red);
            placed.Remove(go); // les monstres bougent : geres a part dans Refresh
            monsterObjects[m] = go;
        }

        foreach (Ally a in gameState.allyList)
        {
            RectInt rect = new RectInt(a.position.x, a.position.y, 1, 1);
            GameObject go = Place("Ally_" + a.id, allyPrefab, rect, monsterHeight, 0, Color.blue);
            placed.Remove(go); // les allies bougent aussi
            allyObjects[a] = go;
        }

        foreach (Trap t in gameState.trapList)
        {
            RectInt rect = new RectInt(t.x, t.y, 1, 1);
            GameObject go = Place("Trap_" + t.type, trapPrefab, rect, trapHeight, 0, new Color(1f, 0.5f, 0f));
            placed.Remove(go); // visibilite selon l'etat du piege : geree dans Refresh
            trapObjects[t] = go;
        }

        BuildFog();
        lastSignature = int.MinValue;
        Refresh();
    }

    // la vue change quand un heros bouge, qu'une porte s'ouvre ou qu'un monstre se deplace
    void Update()
    {
        if (gameState == null) return;
        int signature = Signature();
        if (signature == lastSignature) return;
        Refresh();
    }

    // met a jour la visibilite (brouillard), les portes et la position des monstres
    // a appeler quand une porte s'ouvre, qu'un heros regarde ou qu'un monstre bouge
    [ContextMenu("Refresh")]
    public void Refresh()
    {
        if (gameState == null) return;

        UpdateVisibility();

        foreach (KeyValuePair<GameObject, RectInt> pair in placed)
            pair.Key.SetActive(showAll || IsRevealed(pair.Value));

        foreach (KeyValuePair<Monster, GameObject> pair in monsterObjects)
        {
            Vector2Int p = pair.Key.position;
            RectInt rect = new RectInt(p.x, p.y, 1, 1);
            pair.Value.transform.position = RectCenter(rect);
            pair.Value.SetActive(showAll || ((showHidden || !pair.Key.isHidden) && IsRevealed(rect)));
        }

        // un piege n'a une tuile sur le plateau que s'il s'est declenche
        foreach (KeyValuePair<Trap, GameObject> pair in trapObjects)
            pair.Value.SetActive(showHidden || (pair.Key.state == TrapState.Declenche
                && (showAll || IsRevealed(new RectInt(pair.Key.x, pair.Key.y, 1, 1)))));

        foreach (KeyValuePair<Ally, GameObject> pair in allyObjects)
        {
            Vector2Int p = pair.Key.position;
            RectInt rect = new RectInt(p.x, p.y, 1, 1);
            pair.Value.transform.position = RectCenter(rect);
            pair.Value.SetActive(showAll || IsRevealed(rect));
        }

        // une porte visible remplace son mur ; une porte cachee laisse le mur en place
        foreach (KeyValuePair<Door, GameObject> pair in doorObjects)
        {
            Door door = pair.Key;
            bool visible = (!door.isSecret || door.isFound || showHidden) && (showAll || IsDoorSeen(door));
            pair.Value.SetActive(visible);

            GameObject wall = board.GetWall(door.x, door.y, door.side);
            if (wall != null) wall.SetActive(!visible);
        }

        UpdateFog();
        lastSignature = Signature();
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        // on remet les murs caches par les portes
        foreach (Door door in doorObjects.Keys)
        {
            GameObject wall = board.GetWall(door.x, door.y, door.side);
            if (wall != null) wall.SetActive(true);
        }

        if (root != null)
        {
            if (Application.isPlaying) Destroy(root.gameObject);
            else DestroyImmediate(root.gameObject);
        }

        placed.Clear();
        monsterObjects.Clear();
        allyObjects.Clear();
        doorObjects.Clear();
        trapObjects.Clear();
        if (fogMesh != null)
        {
            if (Application.isPlaying) Destroy(fogMesh);
            else DestroyImmediate(fogMesh);
        }
        fogMesh = null;
        if (fogMaterial != null)
        {
            if (Application.isPlaying) Destroy(fogMaterial);
            else DestroyImmediate(fogMaterial);
        }
        fogMaterial = null;
        fogObject = null;
        seenCells = null;
        lastSignature = int.MinValue;
        gameState = null;
    }

    void PlaceDoor(Door door)
    {
        GameObject wall = board.GetWall(door.x, door.y, door.side);
        if (wall == null)
        {
            Debug.LogWarning("Pas de mur pour la porte (" + door.x + ", " + door.y + ", " + door.side + ")");
            return;
        }

        GameObject prefab = door.isSecret ? secretDoorPrefab : doorPrefab;
        Color color = door.isSecret ? new Color(0.5f, 0.2f, 0.6f) : new Color(0.55f, 0.3f, 0.1f);
        GameObject go = CreateFitted("Door_" + door.x + "_" + door.y + "_" + door.side, prefab,
            new Vector3(board.cellSize, doorHeight, doorThickness), color);

        // la porte prend la place et l'orientation du mur
        go.transform.position = wall.transform.position;
        go.transform.rotation = wall.transform.rotation;
        doorObjects[door] = go;
    }

    // pose un objet sur un rectangle de cases ; rotation 90 ou 270 = objet tourne d'un quart de tour
    GameObject Place(string name, GameObject prefab, RectInt rect, float height, int rotation, Color color)
    {
        // le modele est mesure avant rotation : un objet tourne a ses cotes inverses
        bool turned = rotation == 90 || rotation == 270;
        float sizeX = (turned ? rect.height : rect.width) * board.cellSize;
        float sizeZ = (turned ? rect.width : rect.height) * board.cellSize;

        GameObject go = CreateFitted(name + "_" + rect.x + "_" + rect.y, prefab, new Vector3(sizeX, height, sizeZ), color);
        go.transform.position = RectCenter(rect);
        go.transform.rotation = Quaternion.Euler(0f, rotation, 0f);

        placed[go] = rect;
        return go;
    }

    // cases ou se trouvent les heros (une case de depart tant qu'un heros n'est pas encore place)
    List<Vector2Int> Viewers()
    {
        List<Vector2Int> list = new List<Vector2Int>();
        List<Vector2Int> starts = gameState.heroStartCells;

        int count = gameState.heroList.Count;
        if (count == 0)
        {
            if (starts != null) list.AddRange(starts);
            return list;
        }

        for (int i = 0; i < count; i++)
        {
            Vector2Int p = gameState.heroList[i].position;
            if (p == Vector2Int.zero && starts != null && i < starts.Count) p = starts[i];
            list.Add(p);
        }
        return list;
    }

    void UpdateVisibility()
    {
        seenCells = null;
        if (!useFog) return;

        List<Vector2Int> viewers = Viewers();
        if (viewers.Count == 0) return;
        seenCells = QuestVision.Compute(gameState, viewers);
    }

    // une case est visible si un heros la voit ; sans heros, on retombe sur les salles revelees
    bool IsCellVisible(int x, int y)
    {
        if (!gameState.map.InBounds(x, y)) return false;
        if (seenCells != null) return seenCells[x, y];
        return gameState.map.Get(x, y).revealed;
    }

    // valeur qui change des que la vue doit etre recalculee
    int Signature()
    {
        int h = 17;
        foreach (Vector2Int v in Viewers()) h = h * 31 + v.x * 64 + v.y;
        foreach (Door d in gameState.doorList) h = h * 31 + (d.isOpen ? 1 : 0) + (d.isFound ? 2 : 0);
        foreach (Monster m in gameState.monsterList) h = h * 31 + m.position.x * 64 + m.position.y;
        foreach (Ally a in gameState.allyList) h = h * 31 + a.position.x * 64 + a.position.y;
        h = h * 31 + (showAll ? 1 : 0) + (showHidden ? 2 : 0) + (useFog ? 4 : 0);
        return h;
    }

    // plan de brouillard : un seul objet, mis a jour a chaque Refresh
    void BuildFog()
    {
        GameObject go = new GameObject("Fog");
        go.transform.SetParent(root, false);

        fogMesh = new Mesh();
        fogMesh.name = "FogMesh";
        fogMesh.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = fogMesh;

        fogMaterial = CreateFogMaterial();
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = fogMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        fogObject = go;

        // le shader mesure la profondeur de la scene derriere le plan
        Camera cam = Camera.main;
        if (cam != null)
        {
            if (cam.orthographic)
                Debug.LogWarning("HeroQuest : le brouillard vertical demande une camera en projection Perspective.");
            EnableDepthTexture(cam);
        }
    }

    // active la texture de profondeur de la camera, sans reference directe a URP
    // (une reference directe ne compile pas si le script est dans un assembly qui ne voit pas URP)
    static void EnableDepthTexture(Camera cam)
    {
        // pipeline integre (sans effet sous URP)
        cam.depthTextureMode |= DepthTextureMode.Depth;

        // URP : UniversalAdditionalCameraData.requiresDepthTexture
        try
        {
            Type type = null;
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
                if (type != null) break;
            }
            if (type == null) return;

            Component data = cam.GetComponent(type);
            if (data == null) data = cam.gameObject.AddComponent(type);

            System.Reflection.PropertyInfo property = type.GetProperty("requiresDepthTexture");
            if (property != null && property.CanWrite) property.SetValue(data, true, null);
        }
        catch (Exception e)
        {
            Debug.LogWarning("HeroQuest : impossible d'activer la texture de profondeur sur la camera : " + e.Message);
        }
    }

    // un carre de brouillard sur chaque case non visible, a hauteur (mur - fogBelowWall)
    // le shader voile ce qui est en dessous : sol et bas des murs caches, haut des murs visible
    void UpdateFog()
    {
        if (fogObject == null || fogMesh == null) return;

        Map map = gameState.map;
        float planeY = board.transform.position.y + board.wallHeight - fogBelowWall;
        float h = board.cellSize * 0.5f;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int y = 0; y < map.height; y++)
        {
            for (int x = 0; x < map.width; x++)
            {
                if (!useFog || showAll || IsCellVisible(x, y)) continue;

                Vector3 c = board.CellToWorld(x, y);
                Vector3[] corners =
                {
                    new Vector3(c.x - h, planeY, c.z - h),
                    new Vector3(c.x - h, planeY, c.z + h),
                    new Vector3(c.x + h, planeY, c.z + h),
                    new Vector3(c.x + h, planeY, c.z - h)
                };

                int first = vertices.Count;
                foreach (Vector3 corner in corners)
                {
                    vertices.Add(root.InverseTransformPoint(corner));
                    normals.Add(Vector3.up);
                    // UV continues sur tout le plateau (10 unites = 1), pour que les nuages ne se repetent pas par case
                    uvs.Add(new Vector2(corner.x / 10f, corner.z / 10f));
                }
                triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
                triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 3);
            }
        }

        fogMesh.Clear();
        fogMesh.SetVertices(vertices);
        fogMesh.SetNormals(normals);
        fogMesh.SetUVs(0, uvs);
        fogMesh.SetTriangles(triangles, 0);
        fogMesh.RecalculateBounds();
        fogObject.SetActive(vertices.Count > 0);
    }

    // copie du materiau Dynamic Vertical Fog avec nos reglages (l'asset reste intact)
    Material CreateFogMaterial()
    {
        Material source = fogMaterialAsset;
        if (source == null) source = Resources.Load<Material>("HeroQuest/DynamicVerticalFogMaterial");

        if (source != null)
        {
            return new Material(source);
        }

        // pas de materiau : plan opaque uni, qui cache aussi tout ce qui est sous lui
        Debug.LogWarning("HeroQuest : DynamicVerticalFogMaterial introuvable (champ Fog Material Asset de QuestView, "
            + "ou Resources/HeroQuest). Brouillard uni utilise a la place.");
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material fallback = new Material(shader);
        if (fallback.HasProperty("_BaseColor")) fallback.SetColor("_BaseColor", fogColor);
        fallback.color = fogColor;
        return fallback;
    }

    // escalier = image a plat qui couvre toutes ses cases ; renvoie false si l'image est introuvable
    bool PlaceTexturedStairs(RectInt rect, int rotation)
    {
        GameObject go = HQStairsVisual.Create("Stairs_" + rect.x + "_" + rect.y,
            rect.width * board.cellSize, rect.height * board.cellSize, rotation);
        if (go == null) return false;

        go.transform.position = RectCenter(rect) + new Vector3(0f, HQStairsVisual.HeightAboveFloor, 0f);
        go.transform.SetParent(root, true);
        placed[go] = rect;
        return true;
    }

    // conteneur + modele ajuste a la taille "size", range sous la racine de la quete
    GameObject CreateFitted(string name, GameObject prefab, Vector3 size, Color color)
    {
        GameObject container = new GameObject(name);

        GameObject model;
        if (prefab != null) model = Instantiate(prefab);
        else
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.GetComponent<Renderer>().sharedMaterial = ColorMaterial(color);
        }
        model.transform.SetParent(container.transform, false);

        board.FitInBox(container, model, size);
        container.transform.SetParent(root, true);
        return container;
    }

    // centre d'un rectangle de cases, au niveau du sol
    Vector3 RectCenter(RectInt rect)
    {
        Vector3 topLeft = board.CellToWorld(rect.x, rect.y);
        return topLeft + new Vector3((rect.width - 1) * board.cellSize * 0.5f, 0f, -(rect.height - 1) * board.cellSize * 0.5f);
    }

    // vrai si au moins une case du rectangle est revelee
    bool IsRevealed(RectInt rect)
    {
        for (int y = rect.y; y < rect.y + rect.height; y++)
        {
            for (int x = rect.x; x < rect.x + rect.width; x++)
            {
                if (IsCellVisible(x, y)) return true;
            }
        }
        return false;
    }

    // une porte est vue si une des deux cases qu'elle separe est revelee
    bool IsDoorSeen(Door door)
    {
        int nx = door.x;
        int ny = door.y;
        if (door.side == Side.North) ny--;
        if (door.side == Side.South) ny++;
        if (door.side == Side.East) nx++;
        if (door.side == Side.West) nx--;

        return IsRevealed(new RectInt(door.x, door.y, 1, 1)) || IsRevealed(new RectInt(nx, ny, 1, 1));
    }

    GameObject FindFurniturePrefab(FurnitureType type)
    {
        if (furniturePrefabs == null) return null;
        foreach (FurniturePrefab p in furniturePrefabs)
        {
            if (p.type == type) return p.prefab;
        }
        return null;
    }

    GameObject FindMonsterPrefab(MonsterClass type)
    {
        if (monsterPrefabs == null) return null;
        foreach (MonsterPrefab p in monsterPrefabs)
        {
            if (p.type == type) return p.prefab;
        }
        return null;
    }

    // un materiau par couleur pour les cubes de secours
    Material ColorMaterial(Color color)
    {
        Material m;
        if (colorMaterials.TryGetValue(color, out m) && m != null) return m;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        m = new Material(shader);
        m.color = color;
        colorMaterials[color] = m;
        return m;
    }
}
