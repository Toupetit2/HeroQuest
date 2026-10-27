using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HeroQuest PCG, simple version. The floor and original wall layout NEVER change.
/// The TXT describes cells; the script randomly opens selected internal walls
/// and places movable content. Compatible with Git Map.cs/Tile.cs and the runtime HQPCGBoardBuilder.
/// </summary>
public class HeroQuestSimplePCG : MonoBehaviour
{
    public enum ContentKind { Furniture, Monster, Trap, Obstacle }

    [Serializable]
    public class Prop
    {
        public string description;
        public int id;                // 13 = chest; 10 = orc; 8 = pit, depending on ContentKind
        public ContentKind kind;
        public GameObject prefab;
        [Min(1)] public int weight = 1;
        public Vector2Int sizeInCells = Vector2Int.one;
        public bool canRotate = true;
        public bool againstWall;
        public float heightOffset;    // If your prefab pivot is not centered on the ground
    }

    [Header("Board (already built in Unity)")]
    public TextAsset boardFile;
    public Transform wallsRoot;
    public Vector3 firstCellLocalCenter = new Vector3(-25f, 0f, -18f);
    public float cellSize = 2f;

    [Header("Generation")]
    public int seed = 12345;
    public bool generateOnPlay = false;
    [Range(0, 1)] public float extraDoorChance = 0.14f;
    [Range(1, 22)] public int availableRooms = 6;
    [Min(0)] public int furnitureCount = 5;
    [Min(0)] public int monsterCount = 8;
    [Min(0)] public int trapCount = 2;
    [Min(0)] public int blockedSquareCount = 0;
    public bool placeStairs = true;

    [Header("Original HeroQuest-inspired rules")]
    [Tooltip("Use the miniature and furniture stock quantities in rulebook page 4.")]
    public bool respectPhysicalPieceLimits = true;
    [Tooltip("Rare named enemies should normally be placed by a quest, not at random.")]
    public bool includeQuestBosses = false;
    [Tooltip("Do not show monsters and furniture until a door into their room opens.")]
    public bool hideUnexploredRoomContents = true;
    [Tooltip("Prototype convenience: display hidden traps to check their positions.")]
    public bool revealTrapsForTesting = false;
    [Tooltip("Only optional loop connections may become secret doors; never the sole entrance to a room.")]
    [Range(0, 1)] public float secretDoorChance = 0.08f;
    [Tooltip("An additional quest chest is placed in a different accessible room, if possible.")]
    public bool placeObjectiveChest = true;
    public GameObject objectiveChestPrefab;
    [TextArea] public string questGoal = "Reach the objective chest, then return to the stairs (goal not enforced yet).";
    [TextArea] public string lastSummary = "Choose settings, then generate.";
    [TextArea] public string chosenRoomIds = "";

    [Header("Prefabs (optional for the first demo)")]
    public GameObject closedDoorPrefab;
    [Tooltip("Rotation locale additionnelle pour le modele de porte uniquement. Pour une porte prefab couchee, essayer X = 90 ou -90. Laisser 0,0,0 pour les cubes de demo.")]
    public Vector3 closedDoorPrefabRotationOffset = Vector3.zero;
    public GameObject stairsPrefab;
    public List<Prop> catalog = new List<Prop>();
    [Tooltip("With an empty catalog, create colored cubes for a first test.")]
    public bool useDemoObjects = true;

    private const int Width = 26;
    private const int Height = 19;
    private const string GeneratedName = "PCG_Generated";
    private int[,] cells;
    private Dictionary<Vector2Int, Transform> wallAt;
    private HashSet<Transform> openedWalls;
    private HashSet<Transform> secretWalls;
    private HashSet<Vector2Int> reserved;
    private HashSet<Vector2Int> blockedByFurniture;
    private HashSet<int> activeZones;
    private List<int> activeRoomIds;
    private Dictionary<int, List<Vector2Int>> roomCells;
    private Transform generatedRoot;
    private System.Random random;
    private Dictionary<int, List<GameObject>> hiddenContents;
    private HashSet<int> revealedRooms;
    private Dictionary<string, int> piecesUsed;
    private int startRoomId;
    private int objectiveRoomId;

    private struct DoorCandidate
    {
        public Transform wall;
        public Vector2Int a;
        public Vector2Int b;
        public int zoneA;
        public int zoneB;
    }

    private void Start()
    {
        if (generateOnPlay) Generate();
    }

    [ContextMenu("Generate dungeon")]
    public void Generate()
    {
        ClearGenerated();
        chosenRoomIds = "";
        if (!ReadBoard() || !FindWalls())
        {
            lastSummary = "The board could not be loaded. Check the Console.";
            return;
        }

        random = new System.Random(seed);
        reserved = new HashSet<Vector2Int>();
        blockedByFurniture = new HashSet<Vector2Int>();
        openedWalls = new HashSet<Transform>();
        secretWalls = new HashSet<Transform>();
        hiddenContents = new Dictionary<int, List<GameObject>>();
        revealedRooms = new HashSet<int>();
        piecesUsed = new Dictionary<string, int>();
        startRoomId = -1;
        objectiveRoomId = -1;
        activeZones = new HashSet<int> { 0 }; // 0 is the common corridor
        activeRoomIds = new List<int>();

        // The boxed game contains 21 door pieces. In a classic-like quest,
        // don't select more rooms than could receive a door each.
        if (respectPhysicalPieceLimits && availableRooms > 21)
        {
            lastSummary = "Classic piece limit: only 21 door pieces exist. Choose 21 rooms or fewer.";
            Debug.LogWarning("PCG: " + lastSummary, this);
            return;
        }
        List<DoorCandidate> candidates = FindDoorCandidates();
        if (!ChooseConnectedRooms(candidates, Mathf.Clamp(availableRooms, 1, 22)))
        {
            lastSummary = "Cannot open the requested number of connected rooms.";
            Debug.LogError("PCG: " + lastSummary, this);
            return;
        }

        // Protect both cells around every doorway from furniture and monsters.
        foreach (DoorCandidate door in candidates)
        {
            if (openedWalls.Contains(door.wall))
            {
                reserved.Add(door.a);
                reserved.Add(door.b);
            }
        }

        // Inactive rooms are deliberately not included in this path check.
        if (!AllFreeCellsReachable())
        {
            lastSummary = "The selected rooms are not all reachable.";
            Debug.LogError("PCG: " + lastSummary, this);
            return;
        }

        generatedRoot = new GameObject(GeneratedName).transform;
        generatedRoot.SetParent(transform, false);
        chosenRoomIds = "Rooms: " + string.Join(", ", activeRoomIds);
        int doors = SpawnDoors(candidates);
        bool stairsOk = !placeStairs || PlaceStairs();
        // Heroes begin on the staircase; their starting room is already known.
        if (startRoomId > 0) RevealRoom(startRoomId);
        if (!stairsOk)
        {
            // Ne pas generer un donjon jouable sans salle de depart identifiee.
            ClearGenerated();
            lastSummary = "Impossible de placer l'escalier : génération annulée.";
            Debug.LogError("PCG: " + lastSummary, this);
            return;
        }
        bool objectiveOk = !placeObjectiveChest || PlaceObjectiveChest();
        if (!objectiveOk) Debug.LogWarning("PCG: objective chest couldn't be placed.", this);
        int furniture = PlaceRequested(ContentKind.Furniture, furnitureCount);
        int monsters = PlaceRequested(ContentKind.Monster, monsterCount);
        int traps = PlaceRequested(ContentKind.Trap, trapCount);
        int obstacles = PlaceRequested(ContentKind.Obstacle, blockedSquareCount);
        lastSummary = activeRoomIds.Count + " rooms, " + doors + " doors (" +
                      secretWalls.Count + " secret), " +
                      furniture + "/" + furnitureCount + " extra furniture, " +
                      monsters + "/" + monsterCount + " monsters, " +
                      traps + "/" + trapCount + " hidden traps, " +
                      obstacles + "/" + blockedSquareCount + " blocked pieces; " +
                      (placeObjectiveChest ? (objectiveOk ? "objective set" : "NO OBJECTIVE") : "no objective") +
                      " (seed " + seed + ")";
        Debug.Log("HeroQuest PCG: " + lastSummary, this);
    }

    // An exact request may be impossible when the chosen rooms are too small.
    // In that case, show the actual count rather than creating overlaps.
    private int PlaceRequested(ContentKind kind, int requested)
    {
        int placed = 0;
        for (int item = 0; item < Mathf.Clamp(requested, 0, 100); item++)
        {
            // Try the accessible rooms in a different random order for each object.
            var choices = new List<int>(activeRoomIds);
            if (kind == ContentKind.Trap || kind == ContentKind.Obstacle)
                choices.Add(0); // Traps / blocked square tiles may also be in corridors.
            // La salle de depart reste sans ennemis, pas seulement la case escalier.
            if (kind == ContentKind.Monster && startRoomId > 0)
                choices.Remove(startRoomId);
            Shuffle(choices);
            bool success = false;
            foreach (int room in choices)
            {
                if (TryPlace(room, kind)) { success = true; break; }
            }
            if (!success) break; // no more space for this category
            placed++;
        }
        return placed;
    }

    // Utilise directement Map.cs de Git pour lire board.txt.
    // L'axe Y de Map (haut vers bas) est inverse de l'axe Z dans la scene 3D.
    private bool ReadBoard()
    {
        if (boardFile == null)
        {
            Debug.LogError("PCG: assign Assets/Data/Board/board.txt (Git) in Board File.", this);
            return false;
        }

        Map map;
        try
        {
            map = new Map(boardFile.text);
        }
        catch (Exception ex)
        {
            Debug.LogError("PCG: invalid Git board.txt: " + ex.Message, this);
            return false;
        }
        if (map.width != Width || map.height != Height)
        {
            Debug.LogError("PCG: expected a 26 x 19 map from Git.", this);
            return false;
        }
        int[,] parsed = new int[Width, Height];
        var foundRooms = new HashSet<int>();
        roomCells = new Dictionary<int, List<Vector2Int>>();
        for (int i = 0; i <= 22; i++) roomCells[i] = new List<Vector2Int>();
        for (int row = 0; row < Height; row++)
        {
            for (int x = 0; x < Width; x++)
            {
                string token = map.Get(x, row).zone;
                int zone;
                if (token == ".") zone = 0;
                else if (!token.StartsWith("S") || !int.TryParse(token.Substring(1), out zone)
                        || zone < 1 || zone > 22)
                {
                    Debug.LogError("PCG: invalid zone " + token + " at (" + x + "," + row + ")", this);
                    return false;
                }
                int gridY = Height - row - 1;
                parsed[x, gridY] = zone;
                if (zone != 0) foundRooms.Add(zone);
                roomCells[zone].Add(new Vector2Int(x, gridY));
            }
        }
        if (foundRooms.Count != 22)
        {
            Debug.LogError("PCG: expected 22 rooms, found " + foundRooms.Count, this);
            return false;
        }
        cells = parsed;
        return true;
    }

    private bool FindWalls()
    {
        if (wallsRoot == null) wallsRoot = transform.Find("Walls");
        if (wallsRoot == null)
        {
            Debug.LogError("PCG: missing child 'Walls' on Plateau.", this);
            return false;
        }
        wallAt = new Dictionary<Vector2Int, Transform>();
        foreach (Transform child in wallsRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == wallsRoot || child.GetComponent<MeshRenderer>() == null) continue;
            Vector3 local = transform.InverseTransformPoint(child.position);
            Vector2Int key = new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.z));
            if (!wallAt.ContainsKey(key)) wallAt.Add(key, child);
        }
        if (wallAt.Count < 200)
        {
            Debug.LogError("PCG: too few wall pieces. Check Walls Root / Plateau transform.", this);
            return false;
        }
        return true;
    }

    private Vector2Int WallKey(Vector2Int a, Vector2Int b)
    {
        Vector3 mid = (CellLocal(a) + CellLocal(b)) * 0.5f;
        return new Vector2Int(Mathf.RoundToInt(mid.x), Mathf.RoundToInt(mid.z));
    }

    private List<DoorCandidate> FindDoorCandidates()
    {
        var result = new List<DoorCandidate>();
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            Vector2Int a = new Vector2Int(x, y);
            TryAdd(a, new Vector2Int(x + 1, y), result);
            TryAdd(a, new Vector2Int(x, y + 1), result);
        }
        return result;
    }

    private void TryAdd(Vector2Int a, Vector2Int b, List<DoorCandidate> result)
    {
        if (!Inside(b) || cells[a.x, a.y] == cells[b.x, b.y]) return;
        Transform wall;
        if (!wallAt.TryGetValue(WallKey(a, b), out wall)) return;
        result.Add(new DoorCandidate { a = a, b = b, wall = wall,
            zoneA = cells[a.x, a.y], zoneB = cells[b.x, b.y] });
    }

    // Begin with the corridor (zone 0), then add adjacent rooms one at a time.
    // This guarantees exactly N reachable rooms and leaves the others sealed.
    private bool ChooseConnectedRooms(List<DoorCandidate> candidates, int requestedRooms)
    {
        Shuffle(candidates);
        var zonePairsWithDoor = new HashSet<int>();

        while (activeRoomIds.Count < requestedRooms)
        {
            var frontier = new List<DoorCandidate>();
            foreach (DoorCandidate c in candidates)
            {
                bool fromA = activeZones.Contains(c.zoneA);
                bool fromB = activeZones.Contains(c.zoneB);
                if (fromA == fromB) continue; // both selected or neither
                frontier.Add(c);
            }

            if (frontier.Count == 0) return false;
            DoorCandidate selected = frontier[random.Next(frontier.Count)];
            int newRoom = activeZones.Contains(selected.zoneA) ? selected.zoneB : selected.zoneA;
            if (newRoom == 0) return false; // corridor was selected from the start
            activeZones.Add(newRoom);
            activeRoomIds.Add(newRoom);
            openedWalls.Add(selected.wall);
            zonePairsWithDoor.Add(PairKey(selected.zoneA, selected.zoneB));
        }

        // Optional shortcut doors ONLY between already active zones.
        foreach (DoorCandidate c in candidates)
        {
            if (!activeZones.Contains(c.zoneA) || !activeZones.Contains(c.zoneB)) continue;
            int pair = PairKey(c.zoneA, c.zoneB);
            if (zonePairsWithDoor.Contains(pair)) continue;
            if ((!respectPhysicalPieceLimits || openedWalls.Count < 21) &&
                random.NextDouble() < extraDoorChance)
            {
                openedWalls.Add(c.wall);
                // Secret doors provide a bonus shortcut, never a required connection.
                if (random.NextDouble() < secretDoorChance) secretWalls.Add(c.wall);
                zonePairsWithDoor.Add(pair);
            }
        }
        activeRoomIds.Sort(); // Stable ordering for reproducible content placement
        return true;
    }

    private static int PairKey(int a, int b) => Mathf.Min(a, b) * 23 + Mathf.Max(a, b);

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T temp = list[i]; list[i] = list[j]; list[j] = temp;
        }
    }

    private int SpawnDoors(List<DoorCandidate> candidates)
    {
        int count = 0;
        foreach (DoorCandidate c in candidates)
        {
            if (!openedWalls.Contains(c.wall)) continue;
            // Secret doors retain the original wall until a search discovers them.
            // Normal doors will be switched by HQGeneratedDoor.Initialize.
            // Les murs de HQPCGBoardBuilder sont des cubes redimensionnes,
            // pas pivotes : c.wall.rotation vaut souvent la meme rotation pour
            // tous les murs. La direction des DEUX cases determine l'axe de la porte.
            // - Cases voisines gauche/droite : separation suivant Z (Y = 90 deg)
            // - Cases voisines haut/bas     : separation suivant X (Y = 0 deg)
            bool neighborsAlongX = c.a.x != c.b.x;
            float yaw = neighborsAlongX ? 90f : 0f;
            Quaternion doorRotation = transform.rotation * Quaternion.Euler(0f, yaw, 0f);

            // Le decalage sert uniquement aux modeles importes si leur axe local
            // est different (par exemple prefab couche sur le sol).
            Quaternion prefabRotation = doorRotation * Quaternion.Euler(closedDoorPrefabRotationOffset);
            GameObject door = closedDoorPrefab != null
                ? Instantiate(closedDoorPrefab, c.wall.position, prefabRotation, generatedRoot)
                : DemoCube("Demo_Door", c.wall.position, doorRotation,
                           new Vector3(1.6f, 2f, 0.2f), new Color(0.6f, 0.36f, 0.15f));
            door.name = "Door_" + c.zoneA + "_" + c.zoneB;
            HQGeneratedDoor control = door.GetComponent<HQGeneratedDoor>();
            if (control == null) control = door.AddComponent<HQGeneratedDoor>();
            control.Initialize(this, c.wall.gameObject, c.zoneA, c.zoneB,
                               secretWalls.Contains(c.wall));
            count++;
        }
        return count;
    }

    // Stairs on the classic Quest Book maps occupy a 2 x 2 footprint.
    // Pick a starting room before allocating other furniture or monsters.
    private bool PlaceStairs()
    {
        var rooms = new List<int>(activeRoomIds);
        Shuffle(rooms);
        foreach (int room in rooms)
        {
            var cellsHere = new List<Vector2Int>(roomCells[room]);
            Shuffle(cellsHere);
            foreach (Vector2Int origin in cellsHere)
            {
                var size = new Vector2Int(2, 2);
                if (!Fits(room, origin, size, false)) continue;
                for (int dy = 0; dy < 2; dy++)
                for (int dx = 0; dx < 2; dx++) reserved.Add(origin + new Vector2Int(dx, dy));
                startRoomId = room;
                Vector3 pos = transform.TransformPoint(CellLocal(origin) +
                              new Vector3(cellSize * 0.5f, 0.18f, cellSize * 0.5f));
                if (stairsPrefab != null)
                    Instantiate(stairsPrefab, pos, transform.rotation, generatedRoot);
                else if (useDemoObjects)
                    DemoCube("Demo_Stairs_2x2", pos, transform.rotation,
                             new Vector3(3.5f, 0.2f, 3.5f), new Color(0.2f, 0.8f, 0.4f));
                return true;
            }
        }
        return false;
    }

    private bool PlaceObjectiveChest()
    {
        var rooms = new List<int>(activeRoomIds);
        // Prefer a different room from the stairs, when more than one is available.
        if (rooms.Count > 1) rooms.Remove(startRoomId);
        Shuffle(rooms);
        foreach (int room in rooms)
        {
            if (!TryPlace(room, ContentKind.Furniture, 13, true)) continue;
            objectiveRoomId = room;
            questGoal = "Find the chest in room S" + objectiveRoomId +
                        " and return to the stairs in S" + startRoomId +
                        ". Quest victory is not automated yet.";
            return true;
        }
        return false;
    }

    // When a hero opens a door, this room's monsters and furniture appear.
    // Hidden traps remain hidden, independently of these objects.
    public void RevealRoom(int roomId)
    {
        if (roomId < 1 || hiddenContents == null) return;
        revealedRooms.Add(roomId);
        List<GameObject> objects;
        if (!hiddenContents.TryGetValue(roomId, out objects)) return;
        foreach (GameObject obj in objects) if (obj != null) obj.SetActive(true);
    }

    public void RevealAllRoomsForTesting()
    {
        if (activeRoomIds == null) return;
        foreach (int roomId in activeRoomIds) RevealRoom(roomId);
    }

    public void RevealAllDoorsForTesting()
    {
        if (generatedRoot == null) return;
        foreach (HQGeneratedDoor door in generatedRoot.GetComponentsInChildren<HQGeneratedDoor>(true))
            if (!door.isSecret) door.RevealDoor();
    }

    public void DiscoverAllSecretDoorsForTesting()
    {
        if (generatedRoot == null) return;
        foreach (HQGeneratedDoor door in generatedRoot.GetComponentsInChildren<HQGeneratedDoor>(true))
            if (door.isSecret) door.DiscoverSecretDoor();
    }

    public void RevealAllTrapsForTesting()
    {
        if (generatedRoot == null) return;
        foreach (HQHiddenTrap trap in generatedRoot.GetComponentsInChildren<HQHiddenTrap>(true))
            trap.Reveal();
    }

    private string PieceKey(ContentKind kind, int id) => kind.ToString() + ":" + id;

    private bool CanUsePiece(Prop p)
    {
        if (p.weight <= 0 || p.sizeInCells.x < 1 || p.sizeInCells.y < 1) return false;
        if (!includeQuestBosses && p.kind == ContentKind.Monster && (p.id == 7 || p.id == 27))
            return false;
        if (!respectPhysicalPieceLimits) return true;
        int max = HQClassicSupplies.Maximum(p.kind, p.id);
        if (max == 0) return false;
        int used;
        piecesUsed.TryGetValue(PieceKey(p.kind, p.id), out used);
        return used < max;
    }

    private void RecordPiece(Prop p)
    {
        string key = PieceKey(p.kind, p.id);
        int used;
        piecesUsed.TryGetValue(key, out used);
        piecesUsed[key] = used + 1;
    }

    private void ConcealRoomObject(int room, GameObject instance)
    {
        if (room == 0 || !hideUnexploredRoomContents || revealedRooms.Contains(room)) return;
        // Corridor line-of-sight reveal is a later game-controller integration.
        List<GameObject> objects;
        if (!hiddenContents.TryGetValue(room, out objects))
        {
            objects = new List<GameObject>();
            hiddenContents[room] = objects;
        }
        objects.Add(instance);
        instance.SetActive(false);
    }

    private bool TryPlace(int area, ContentKind kind, int forcedId = -1, bool objective = false)
    {
        // Securite supplementaire si cette methode est invoquee directement.
        if (kind == ContentKind.Monster && startRoomId > 0 && area == startRoomId)
            return false;
        var pool = new List<Prop>();
        foreach (Prop p in catalog)
            if (p != null && p.kind == kind && p.prefab != null && p.weight > 0 &&
                (forcedId < 0 || p.id == forcedId)) pool.Add(p);

        if (pool.Count == 0 && (useDemoObjects || (objective && objectiveChestPrefab != null)))
        {
            foreach (Prop demo in HQClassicSupplies.Demo(kind, includeQuestBosses))
                if (forcedId < 0 || demo.id == forcedId) pool.Add(demo);
        }
        if (pool.Count == 0) return false;
        for (int attempt = 0; attempt < 180; attempt++)
        {
            var eligible = new List<Prop>();
            foreach (Prop p in pool) if (CanUsePiece(p)) eligible.Add(p);
            if (eligible.Count == 0) return false;
            Prop prop = WeightedPick(eligible);
            bool rotated = prop.canRotate && random.Next(2) == 1;
            Vector2Int size = rotated ? new Vector2Int(prop.sizeInCells.y, prop.sizeInCells.x) : prop.sizeInCells;
            if (size.x < 1 || size.y < 1) continue;
            // Choose an existing tile in this room, not an arbitrary board cell.
            List<Vector2Int> possibleAnchors = roomCells[area];
            if (possibleAnchors.Count == 0) return false;
            Vector2Int anchor = possibleAnchors[random.Next(possibleAnchors.Count)];
            if (!Fits(area, anchor, size, prop.againstWall)) continue;

            var occupiedCells = new List<Vector2Int>();
            for (int yy = 0; yy < size.y; yy++)
            for (int xx = 0; xx < size.x; xx++) occupiedCells.Add(anchor + new Vector2Int(xx, yy));

            if (kind == ContentKind.Furniture || kind == ContentKind.Obstacle)
            {
                foreach (Vector2Int c in occupiedCells) blockedByFurniture.Add(c);
                if (!AllFreeCellsReachable())
                {
                    foreach (Vector2Int c in occupiedCells) blockedByFurniture.Remove(c);
                    continue;
                }
            }
            foreach (Vector2Int c in occupiedCells) reserved.Add(c);

            Vector3 offset = new Vector3((size.x - 1) * cellSize * 0.5f, 0,
                                         (size.y - 1) * cellSize * 0.5f);
            Vector3 pos = transform.TransformPoint(CellLocal(anchor) + offset)
                        + transform.up * prop.heightOffset;
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, rotated ? 90 : 0, 0);
            GameObject obj;
            GameObject chosenPrefab = objective && objectiveChestPrefab != null
                ? objectiveChestPrefab : prop.prefab;
            if (chosenPrefab != null) obj = Instantiate(chosenPrefab, pos, rotation, generatedRoot);
            else
            {
                float h = kind == ContentKind.Trap ? 0.12f :
                          kind == ContentKind.Obstacle ? 0.4f : 1.1f;
                Color color = kind == ContentKind.Furniture ? new Color(0.85f, 0.6f, 0.16f)
                            : kind == ContentKind.Monster ? new Color(0.75f, 0.2f, 0.18f)
                            : kind == ContentKind.Obstacle ? new Color(0.35f, 0.38f, 0.44f)
                            : new Color(0.7f, 0.2f, 0.75f);
                // size represente DEJA l'empreinte apres rotation. Un cube sans
                // rotation evite d'inverser a nouveau largeur et profondeur.
                // Une case de 2 unites => un cube de 2 unites exactement.
                obj = DemoCube("Demo", pos + transform.up * (h * 0.5f),
                               transform.rotation,
                               new Vector3(size.x * cellSize - 0.12f, h,
                                           size.y * cellSize - 0.12f), color);
            }
            obj.name = (objective ? "QUEST_OBJECTIVE_" : "") + kind + "_ID" +
                       prop.id + "_" + anchor.x + "_" + anchor.y;
            RecordPiece(prop);
            HQPlacedPiece data = obj.GetComponent<HQPlacedPiece>();
            if (data == null) data = obj.AddComponent<HQPlacedPiece>();
            data.kind = kind;
            data.id = prop.id;
            data.roomId = area;
            data.anchor = anchor;
            data.footprint = size;
            data.isQuestObjective = objective;
            if (kind == ContentKind.Trap)
            {
                // Traps are NOT visible just because a room is revealed.
                HQHiddenTrap hiddenTrap = obj.GetComponent<HQHiddenTrap>();
                if (hiddenTrap == null) hiddenTrap = obj.AddComponent<HQHiddenTrap>();
                hiddenTrap.Initialize(prop.id, anchor, revealTrapsForTesting);
            }
            else ConcealRoomObject(area, obj);
            return true;
        }
        return false;
    }

    private Prop WeightedPick(List<Prop> pool)
    {
        int total = 0;
        foreach (Prop p in pool) total += p.weight;
        int roll = random.Next(total);
        foreach (Prop p in pool)
        {
            roll -= p.weight;
            if (roll < 0) return p;
        }
        return pool[pool.Count - 1];
    }

    private bool Fits(int area, Vector2Int origin, Vector2Int size, bool againstWall)
    {
        bool wallTouch = false;
        for (int dy = 0; dy < size.y; dy++)
        for (int dx = 0; dx < size.x; dx++)
        {
            Vector2Int c = origin + new Vector2Int(dx, dy);
            if (!Inside(c) || cells[c.x, c.y] != area || reserved.Contains(c)) return false;
            if (againstWall)
            {
                Vector2Int[] around = {Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right};
                foreach (Vector2Int direction in around)
                {
                    Transform wall;
                    if (wallAt.TryGetValue(WallKey(c, c + direction), out wall) && !openedWalls.Contains(wall))
                        wallTouch = true;
                }
            }
        }
        // Un meuble de plusieurs cases ne peut pas traverser un vrai mur.
        // Verifie chaque jonction INTERIEURE de son empreinte.
        for (int dy = 0; dy < size.y; dy++)
        for (int dx = 0; dx < size.x; dx++)
        {
            Vector2Int here = origin + new Vector2Int(dx, dy);
            if (dx + 1 < size.x && HasClosedWall(here, here + Vector2Int.right)) return false;
            if (dy + 1 < size.y && HasClosedWall(here, here + Vector2Int.up)) return false;
        }
        return !againstWall || wallTouch;
    }

    private bool HasClosedWall(Vector2Int a, Vector2Int b)
    {
        Transform wall;
        return wallAt != null && wallAt.TryGetValue(WallKey(a, b), out wall)
            && (openedWalls == null || !openedWalls.Contains(wall) ||
                (secretWalls != null && secretWalls.Contains(wall)));
    }

    private bool AllFreeCellsReachable()
    {
        Vector2Int start = new Vector2Int(-1, -1);
        int freeCount = 0;
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            Vector2Int c = new Vector2Int(x, y);
            if (!activeZones.Contains(cells[x,y]) || blockedByFurniture.Contains(c)) continue;
            freeCount++;
            if (start.x == -1 && cells[x,y] == 0) start = c;
        }
        if (start.x == -1 || blockedByFurniture.Contains(start)) return false;
        var seen = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        Vector2Int[] dirs = {Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right};
        while (queue.Count > 0)
        {
            Vector2Int a = queue.Dequeue();
            foreach (Vector2Int dir in dirs)
            {
                Vector2Int b = a + dir;
                if (!Inside(b) || seen.Contains(b) || blockedByFurniture.Contains(b) ||
                    !activeZones.Contains(cells[b.x,b.y])) continue;
                Transform wall;
                if (wallAt.TryGetValue(WallKey(a, b), out wall) &&
                    (!openedWalls.Contains(wall) || secretWalls.Contains(wall))) continue;
                seen.Add(b);
                queue.Enqueue(b);
            }
        }
        return seen.Count == freeCount;
    }

    private bool Inside(Vector2Int c) => c.x >= 0 && c.x < Width && c.y >= 0 && c.y < Height;

    private Vector3 CellLocal(Vector2Int c) => firstCellLocalCenter +
        new Vector3(c.x * cellSize, 0, c.y * cellSize);

    private GameObject DemoCube(string objectName, Vector3 position, Quaternion rotation,
                                Vector3 size, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;
        go.transform.SetParent(generatedRoot, true);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = size;
        Renderer renderer = go.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader != null)
        {
            Material material = new Material(shader);
            material.color = color;
            renderer.sharedMaterial = material;
        }
        return go;
    }

    [ContextMenu("Clear generated dungeon")]
    public void ClearGenerated()
    {
        // Find all roots so that clearing also works after a domain reload.
        var oldRoots = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == GeneratedName) oldRoots.Add(child.gameObject);
        }
        foreach (GameObject root in oldRoots)
        {
            foreach (HQGeneratedDoor door in root.GetComponentsInChildren<HQGeneratedDoor>(true))
                if (door.originalWall != null) door.originalWall.SetActive(true);
            root.SetActive(false);
            if (Application.isPlaying) Destroy(root);
            else DestroyImmediate(root);
        }
    }
}
