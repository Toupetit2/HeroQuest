using System;
using System.Collections.Generic;
using UnityEngine;

// Charge une quête officielle fixée dans un fichier texte, sans utiliser la PCG.
// Les cartes incomplètement retranscrites restent marquées PARTIAL / REFERENCE ONLY.
public class HQCampaignQuestLoader : MonoBehaviour
{
    public HeroQuestSimplePCG board;
    [Range(1, 14)] public int selectedQuest = 1;
    public string currentTitle = "";
    [TextArea(2, 5)] public string loadReport = "Choisissez une quête.";
    public HQCampaignQuestData loadedQuest;
    public bool showReferenceMap = true;

    private const int Width = 26;
    private const int Height = 19;
    private Transform campaignRoot;
    private Dictionary<Vector2Int, Transform> wallAt;
    private HashSet<Vector2Int> occupied;
    private HashSet<GameObject> removedWalls;
    private Map logicalMap;
    private int stairsRoom = -1;

    public Texture2D ReferenceMap => Resources.Load<Texture2D>(
        "CampaignMaps/quest_" + selectedQuest.ToString("00"));

    public void LoadSelectedQuest() { LoadQuest(selectedQuest); }

    public void LoadQuest(int questNumber)
    {
        if (board == null) board = GetComponent<HeroQuestSimplePCG>();
        if (board == null || board.boardFile == null)
        {
            loadReport = "Plateau / Board File absent. Lancez la scène avec Plateau.prefab.";
            Debug.LogError(loadReport, this);
            return;
        }
        if (questNumber < 1 || questNumber > 14) return;
        TextAsset source = Resources.Load<TextAsset>("Campaign/quest_" + questNumber.ToString("00"));
        if (source == null)
        {
            loadReport = "Fichier de quête introuvable : " + questNumber;
            Debug.LogError(loadReport, this);
            return;
        }
        HQCampaignQuestData quest;
        try { quest = JsonUtility.FromJson<HQCampaignQuestData>(source.text); }
        catch (Exception ex) { loadReport = "JSON invalide : " + ex.Message; Debug.LogError(loadReport); return; }
        if (quest == null || quest.questNumber != questNumber)
        {
            loadReport = "Numéro incorrect dans le fichier de quête.";
            return;
        }

        ClearCampaign();
        board.ClearGenerated();
        logicalMap = new Map(board.boardFile.text);
        occupied = new HashSet<Vector2Int>();
        removedWalls = new HashSet<GameObject>();
        wallAt = IndexWalls();
        campaignRoot = new GameObject("Campagne_Quete_" + questNumber.ToString("00")).transform;
        campaignRoot.SetParent(transform, false);
        loadedQuest = quest;
        selectedQuest = questNumber;
        currentTitle = quest.title;

        // Réserve d'abord TOUTE la zone d'escalier, et interdit ses monstres.
        // On ne transforme pas une carte officielle en version procédurale.
        stairsRoom = -1;
        if (quest.objects != null)
            foreach (HQCampaignObjectData item in quest.objects)
                if (item != null && item.group == "stairs")
                {
                    Vector2Int c = ConvertTopLeft(item.x, item.y,
                        Mathf.Max(1, item.height));
                    if (Inside(c)) stairsRoom = GetRoom(c);
                    break;
                }

        int doorCount = 0, pieceCount = 0, warnings = 0;
        if (quest.doors != null)
            foreach (HQCampaignDoorData door in quest.doors)
                if (door != null)
                {
                    if (CreateDoor(door)) doorCount++;
                    else warnings++;
                }
        if (quest.objects != null)
        {
            foreach (HQCampaignObjectData item in quest.objects)
                if (item != null && item.group == "stairs")
                {
                    if (CreatePiece(item)) pieceCount++;
                    else warnings++;
                }
            foreach (HQCampaignObjectData item in quest.objects)
                if (item != null && item.group != "stairs")
                {
                    if (CreatePiece(item)) pieceCount++;
                    else warnings++;
                }
        }
        bool complete = quest.digitization != null &&
                        quest.digitization.StartsWith("COMPLETE", StringComparison.OrdinalIgnoreCase);
        loadReport = quest.questNumber + " - " + quest.title + " : " + doorCount + " portes / " +
                     pieceCount + " objets, " + warnings + " placements rejetés. " +
                     (complete ? "Données complètes déclarées." :
                     "TRANSCRIPTION PARTIELLE : affichez la carte d'origine pour comparaison.");
        Debug.Log(loadReport, this);
    }

    private Dictionary<Vector2Int, Transform> IndexWalls()
    {
        Transform walls = board.wallsRoot != null ? board.wallsRoot : transform.Find("Walls");
        var dict = new Dictionary<Vector2Int, Transform>();
        if (walls == null) return dict;
        foreach (Transform child in walls.GetComponentsInChildren<Transform>(true))
        {
            if (child == walls || child.GetComponent<MeshRenderer>() == null) continue;
            Vector3 local = transform.InverseTransformPoint(child.position);
            Vector2Int key = new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.z));
            if (!dict.ContainsKey(key)) dict.Add(key, child);
        }
        return dict;
    }

    private Vector2Int ConvertTopLeft(int x, int y, int height)
        => new Vector2Int(x, Height - y - height);

    private bool Inside(Vector2Int p)
        => p.x >= 0 && p.y >= 0 && p.x < Width && p.y < Height;

    private Vector3 CellLocal(Vector2Int tile)
        => board.firstCellLocalCenter + new Vector3(tile.x * board.cellSize, 0,
                                                     tile.y * board.cellSize);

    private Vector2Int MidpointKey(Vector2Int a, Vector2Int b)
    {
        Vector3 mid = (CellLocal(a) + CellLocal(b)) * 0.5f;
        return new Vector2Int(Mathf.RoundToInt(mid.x), Mathf.RoundToInt(mid.z));
    }

    private int GetRoom(Vector2Int grid)
    {
        if (!Inside(grid)) return -1;
        string zone = logicalMap.Get(grid.x, Height - 1 - grid.y).zone;
        int room;
        return zone != null && zone.StartsWith("S") && int.TryParse(zone.Substring(1), out room)
            ? room : 0;
    }

    private bool CreateDoor(HQCampaignDoorData record)
    {
        Vector2Int a = ConvertTopLeft(record.x, record.y, 1);
        Vector2Int b = a;
        if (record.direction == "E") b.x++;
        else if (record.direction == "S") b.y--;
        else return false;
        if (!Inside(a) || !Inside(b)) return false;
        Transform wall;
        if (!wallAt.TryGetValue(MidpointKey(a, b), out wall))
        {
            Debug.LogWarning("Quête " + selectedQuest + ": mur introuvable pour porte (" +
                             record.x + ", " + record.y + ", " + record.direction + ")", this);
            return false;
        }
        float yaw = a.x != b.x ? 90f : 0f;
        Quaternion rotation = transform.rotation * Quaternion.Euler(0, yaw, 0);
        GameObject door;
        if (board.closedDoorPrefab != null)
            door = Instantiate(board.closedDoorPrefab, wall.position,
                rotation * Quaternion.Euler(board.closedDoorPrefabRotationOffset), campaignRoot);
        else
            door = DemoCube("Porte", wall.position, rotation,
                            new Vector3(board.cellSize * .8f, 2f, .2f),
                            new Color(.65f, .37f, .15f));
        door.name = record.secret ? "SecretDoor" : "ClosedDoor";
        HQGeneratedDoor controller = door.GetComponent<HQGeneratedDoor>();
        if (controller == null) controller = door.AddComponent<HQGeneratedDoor>();
        controller.Initialize(board, wall.gameObject, GetRoom(a), GetRoom(b), record.secret);
        if (!record.secret) removedWalls.Add(wall.gameObject);
        occupied.Add(a);
        occupied.Add(b); // pour ne pas bloquer les portes
        return true;
    }

    private bool CreatePiece(HQCampaignObjectData record)
    {
        HeroQuestSimplePCG.ContentKind kind;
        bool isStairs = record.group == "stairs";
        if (record.group == "monster") kind = HeroQuestSimplePCG.ContentKind.Monster;
        else if (record.group == "furniture" || isStairs) kind = HeroQuestSimplePCG.ContentKind.Furniture;
        else if (record.group == "obstacle") kind = HeroQuestSimplePCG.ContentKind.Obstacle;
        else if (record.group == "trap") kind = HeroQuestSimplePCG.ContentKind.Trap;
        else return false;

        Vector2Int size = new Vector2Int(Mathf.Max(1, record.width), Mathf.Max(1, record.height));
        Vector2Int anchor = ConvertTopLeft(record.x, record.y, size.y);
        if (!FootprintValid(anchor, size)) return false;
        int room = GetRoom(anchor);
        // Regle choisie par l'equipe : AUCUN ennemi dans la salle de l'escalier.
        if (kind == HeroQuestSimplePCG.ContentKind.Monster && stairsRoom >= 0 && room == stairsRoom)
        {
            Debug.LogWarning("Monstre ignoré dans salle de l'escalier S" + stairsRoom, this);
            return false;
        }

        GameObject prefab = isStairs ? board.stairsPrefab : null;
        float heightOffset = 0;
        if (prefab == null)
            foreach (HeroQuestSimplePCG.Prop p in board.catalog)
                if (p != null && p.kind == kind && p.id == record.id && p.prefab != null)
                { prefab = p.prefab; heightOffset = p.heightOffset; break; }

        Vector3 localCenter = CellLocal(anchor) +
            new Vector3((size.x - 1) * board.cellSize * .5f, 0,
                        (size.y - 1) * board.cellSize * .5f);
        Vector3 world = transform.TransformPoint(localCenter) + transform.up * heightOffset;
        Quaternion rotation = transform.rotation * Quaternion.Euler(0, record.angle, 0);
        GameObject go;
        if (prefab != null) go = Instantiate(prefab, world, rotation, campaignRoot);
        else
        {
            float h = isStairs || kind == HeroQuestSimplePCG.ContentKind.Trap ? .12f :
                      kind == HeroQuestSimplePCG.ContentKind.Obstacle ? .4f : 1.1f;
            Color c = isStairs ? Color.green : kind == HeroQuestSimplePCG.ContentKind.Monster ? Color.red :
                      kind == HeroQuestSimplePCG.ContentKind.Furniture ? new Color(.95f,.69f,.12f) :
                      kind == HeroQuestSimplePCG.ContentKind.Trap ? Color.magenta : Color.gray;
            // Dimensions EXACTES de chaque case, sans double rotation du cube de démo.
            go = DemoCube("Bloc", world + transform.up * (h * .5f), transform.rotation,
                          new Vector3(size.x * board.cellSize - 0.12f, h,
                                      size.y * board.cellSize - 0.12f), c);
        }
        go.name = record.group + "_" + record.id + "_" + record.x + "_" + record.y;
        HQPlacedPiece piece = go.GetComponent<HQPlacedPiece>();
        if (piece == null) piece = go.AddComponent<HQPlacedPiece>();
        piece.kind = kind;
        piece.id = record.id;
        piece.roomId = room;
        piece.anchor = anchor;
        piece.footprint = size;
        if (kind == HeroQuestSimplePCG.ContentKind.Trap)
        {
            HQHiddenTrap trap = go.GetComponent<HQHiddenTrap>();
            if (trap == null) trap = go.AddComponent<HQHiddenTrap>();
            trap.Initialize(record.id, anchor, false);
        }
        foreach (Vector2Int c in OccupiedTiles(anchor, size)) occupied.Add(c);
        return true;
    }

    private IEnumerable<Vector2Int> OccupiedTiles(Vector2Int origin, Vector2Int size)
    {
        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++) yield return origin + new Vector2Int(x, y);
    }

    private bool FootprintValid(Vector2Int origin, Vector2Int size)
    {
        int zone = GetRoom(origin);
        if (zone < 0) return false;
        foreach (Vector2Int c in OccupiedTiles(origin, size))
            if (!Inside(c) || GetRoom(c) != zone || occupied.Contains(c)) return false;
        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++)
        {
            Vector2Int c = origin + new Vector2Int(x, y);
            Transform wall;
            if (x + 1 < size.x && wallAt.TryGetValue(MidpointKey(c, c + Vector2Int.right), out wall)
                && !removedWalls.Contains(wall.gameObject)) return false;
            if (y + 1 < size.y && wallAt.TryGetValue(MidpointKey(c, c + Vector2Int.up), out wall)
                && !removedWalls.Contains(wall.gameObject)) return false;
        }
        return true;
    }

    private GameObject DemoCube(string name, Vector3 position, Quaternion rotation,
                                Vector3 size, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(campaignRoot, true);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = size;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.color = color;
            go.GetComponent<Renderer>().material = mat;
        }
        return go;
    }

    [ContextMenu("Effacer campagne")]
    public void ClearCampaign()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith("Campagne_Quete_")) continue;
            foreach (HQGeneratedDoor door in child.GetComponentsInChildren<HQGeneratedDoor>(true))
                if (door.originalWall != null) door.originalWall.SetActive(true);
            child.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        loadedQuest = null;
        loadReport = "Quête effacée.";
    }
}
