using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Remplace les cubes du plateau par les modeles KatsBits pendant le jeu.
// Aucun AssetDatabase ni UnityEditor : fonctionne dans un .exe.
// Les cubes d'origine restent en place (colliders et logique des portes),
// mais leur MeshRenderer est masque et un modele 3D devient leur enfant.
public static class HeroQuestKatsBitsVisuals
{
    private const string Folder = "HeroQuestTerrain/Prefabs/";
    private const float Epsilon = 0.00001f;
    private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
    private static readonly Dictionary<string, GameObject> PreparedPrefabs = new Dictionary<string, GameObject>();
    private static readonly Dictionary<Material, Material> ShaderCache = new Dictionary<Material, Material>();
    private static readonly Color[] RoomColors = {
        Hex("8C766B"), Hex("BB706D"), Hex("659BA8"), Hex("C17E6B"),
        Hex("9FAFC2"), Hex("CEB36B"), Hex("696F77"), Hex("A2AF70"),
        Hex("D3AB57"), Hex("98B263"), Hex("9B9491"), Hex("D5B25A"),
        Hex("779EAB"), Hex("7EABB5"), Hex("9BAAB8"), Hex("D6AA59"),
        Hex("B97C67"), Hex("BA9C67"), Hex("C18972"), Hex("7D879A"),
        Hex("C48F60"), Hex("83A65E")
    };
    private static readonly Color Stone = Hex("D2CABD");
    private static readonly Color WallStone = Hex("C6C0B7");

    // Exécute automatiquement la configuration avant le Start du GameManager
    // dans TestGameplayScene. Ne change pas les menus.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Appele pour la scene initiale et lors du passage MainMenu -> partie.
        // sceneLoaded a lieu avant les Start des composants de la scene.
        ConfigureMainBoard();
    }

    private static GameObject Get(string name)
    {
        GameObject prepared;
        if (PreparedPrefabs.TryGetValue(name, out prepared) && prepared != null)
            return prepared;
        GameObject source = Resources.Load<GameObject>(Folder + name);
        if (source == null) return null;
        // Les originaux du pack utilisent parfois le shader Standard ; une
        // copie runtime permet la conversion URP sans modifier les assets.
        prepared = UnityEngine.Object.Instantiate(source);
        prepared.name = "HQ_KatsBits_Template_" + name;
        prepared.transform.position = new Vector3(10000f + PreparedPrefabs.Count * 50f, -10000f, 10000f);
        UnityEngine.Object.DontDestroyOnLoad(prepared);
        foreach (Renderer r in prepared.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = AdaptShader(mats[i]);
            r.sharedMaterials = mats;
        }
        PreparedPrefabs[name] = prepared;
        return prepared;
    }

    private static Material AdaptShader(Material original)
    {
        if (original == null) return null;
        Material cached;
        if (ShaderCache.TryGetValue(original, out cached) && cached != null) return cached;
        Material m = new Material(original);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null && m.shader != shader)
        {
            Texture albedo = original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") :
                             original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : null;
            Texture normal = original.HasProperty("_BumpMap") ? original.GetTexture("_BumpMap") : null;
            Color tint = original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white;
            m.shader = shader;
            if (albedo != null) m.SetTexture("_BaseMap", albedo);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            m.SetColor("_BaseColor", tint);
        }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.17f);
        ShaderCache[original] = m;
        return m;
    }

    private static void ConfigureMainBoard()
    {
        BoardBuilder board = UnityEngine.Object.FindFirstObjectByType<BoardBuilder>();
        QuestView view = UnityEngine.Object.FindFirstObjectByType<QuestView>();
        if (board == null || view == null) return;
        GameObject floor = Get("RoomFloor");
        GameObject corridor = Get("CorridorFloor");
        GameObject wall = Get("Wall");
        GameObject door = Get("Door");
        if (floor == null || corridor == null || wall == null || door == null)
        {
            Debug.LogError("HeroQuest/KatsBits: prefabs introuvables sous Assets/Resources/HQSelectedTerrain/Prefabs.");
            return;
        }
        board.floorPrefab = floor;
        board.wallPrefab = wall;
        board.roomMaterial = null;
        board.corridorMaterial = null;
        var styles = new List<ZoneStyle>();
        styles.Add(new ZoneStyle { zone = ".", floorPrefab = corridor,
                                   material = Tinted(GetMaterial(corridor), Stone) });
        for (int room = 1; room <= 22; room++)
            styles.Add(new ZoneStyle { zone = "S" + room, floorPrefab = floor,
                        material = Tinted(GetMaterial(floor), RoomColors[room - 1]) });
        board.zoneStyles = styles.ToArray();

        view.board = board;
        view.doorPrefab = door;
        view.secretDoorPrefab = door;
        view.stairsPrefab = Get("Stairs");
        view.blockedPrefab = Get("Blocked");
        var furnishing = new List<FurniturePrefab>();
        if (view.furniturePrefabs != null) furnishing.AddRange(view.furniturePrefabs);
        SetFurniture(furnishing, FurnitureType.EtabliAlchimiste, Get("Cauldron"));
        SetFurniture(furnishing, FurnitureType.Tombeau, Get("Tomb"));
        SetFurniture(furnishing, FurnitureType.TableSorcier, Get("Altar"));
        SetFurniture(furnishing, FurnitureType.Cheminee, Get("Fire"));
        view.furniturePrefabs = furnishing.ToArray();
        Debug.Log("HeroQuest/KatsBits: BoardBuilder configure avec les modeles KatsBits avant Generate().");
    }

    private static void SetFurniture(List<FurniturePrefab> list, FurnitureType type, GameObject prefab)
    {
        if (prefab == null) return;
        foreach (FurniturePrefab entry in list)
            if (entry != null && entry.type == type) { entry.prefab = prefab; return; }
        list.Add(new FurniturePrefab { type = type, prefab = prefab });
    }

    // HQGameModeRuntime charge un autre plateau (Resources/HeroQuest/Plateau),
    // dont les 494 cubes conservent la gestion des collisions et des murs.
    // On change uniquement leur rendu, pas la generation des quetes.
    public static void ReplaceLegacyPlateau(GameObject plateau, string mapText)
    {
        if (plateau == null || string.IsNullOrEmpty(mapText)) return;
        Transform ground = plateau.transform.Find("Ground");
        Transform walls = plateau.transform.Find("Walls");
        if (ground == null || walls == null) return;
        GameObject floor = Get("RoomFloor");
        GameObject corridor = Get("CorridorFloor");
        GameObject wall = Get("Wall");
        if (floor == null || corridor == null || wall == null)
        {
            Debug.LogError("HeroQuest/KatsBits: paquet Resources/HQSelectedTerrain manquant.");
            return;
        }
        Map map;
        try { map = new Map(mapText); }
        catch (Exception ex)
        {
            Debug.LogError("HeroQuest/KatsBits: carte illisible : " + ex.Message);
            return;
        }

        int floors = 0, wallCount = 0;
        foreach (MeshRenderer renderer in ground.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.gameObject.name.StartsWith("ground(Clone)", StringComparison.Ordinal)) continue;
            Vector3 local = plateau.transform.InverseTransformPoint(renderer.bounds.center);
            int x = Mathf.RoundToInt((local.x + 25f) / 2f);
            int bottomY = Mathf.RoundToInt((local.z + 18f) / 2f);
            int topY = map.height - 1 - bottomY;
            if (x < 0 || x >= map.width || topY < 0 || topY >= map.height) continue;
            string zone = map.Get(x, topY).zone;
            int room = 0;
            bool inRoom = zone.StartsWith("S") && int.TryParse(zone.Substring(1), out room) && room > 0 && room <= 22;
            Color tint = inRoom ? RoomColors[room - 1] : Stone;
            GameObject model = inRoom ? floor : corridor;
            if (AttachVisual(renderer, model, tint, false)) floors++;
        }
        foreach (MeshRenderer renderer in walls.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.gameObject.name.StartsWith("ground(Clone)", StringComparison.Ordinal)) continue;
            if (AttachVisual(renderer, wall, WallStone, true)) wallCount++;
        }
        Debug.Log("HeroQuest/KatsBits: " + floors + " sols et " + wallCount +
                  " murs habilles avec les modeles KatsBits sur le plateau du GameManager.");
    }

    // Les prefabs de contenu servent tant a la PCG qu'aux quetes officielles.
    public static void ConfigureGenerator(HeroQuestSimplePCG generator)
    {
        if (generator == null) return;
        generator.closedDoorPrefab = Get("Door");
        generator.stairsPrefab = Get("Stairs");
        generator.objectiveChestPrefab = Get("Plinth"); // socle de substitution, pas un coffre KatsBits

        if (generator.catalog == null) generator.catalog = new List<HeroQuestSimplePCG.Prop>();
        AddProp(generator.catalog, HeroQuestSimplePCG.ContentKind.Furniture, 14, "Tombeau", "Tomb", 1, 2, 1);
        AddProp(generator.catalog, HeroQuestSimplePCG.ContentKind.Furniture, 19, "Table du sorcier", "Altar", 1, 2, 1);
        AddProp(generator.catalog, HeroQuestSimplePCG.ContentKind.Furniture, 20, "Alchimiste", "Cauldron", 1, 2, 1);
        AddProp(generator.catalog, HeroQuestSimplePCG.ContentKind.Furniture, 22, "Cheminee", "Fire", 1, 2, 1);
        AddProp(generator.catalog, HeroQuestSimplePCG.ContentKind.Furniture, 13, "Objectif (socle)", "Plinth", 5, 1, 1);
        AddProp(generator.catalog, HeroQuestSimplePCG.ContentKind.Obstacle, 15, "Bloc de pierre", "Blocked", 5, 1, 1);
        AddProp(generator.catalog, HeroQuestSimplePCG.ContentKind.Obstacle, 18, "Grand bloc de pierre", "Blocked", 2, 2, 1);
        // Le pack KatsBits ne contient pas de figurines de monstres ni de pieges.
        // On garde les representations de demonstration pour ces deux categories.
        generator.useDemoObjects = true;
    }

    private static void AddProp(List<HeroQuestSimplePCG.Prop> list, HeroQuestSimplePCG.ContentKind kind,
                               int id, string description, string modelName,
                               int weight, int width, int height)
    {
        GameObject model = Get(modelName);
        if (model == null) return;
        foreach (var p in list)
            if (p != null && p.kind == kind && p.id == id)
            { p.prefab = model; return; }
        list.Add(new HeroQuestSimplePCG.Prop {
            kind = kind, id = id, description = description, prefab = model,
            weight = weight, sizeInCells = new Vector2Int(width, height), canRotate = true
        });
    }

    private static bool AttachVisual(MeshRenderer original, GameObject prefab, Color tint, bool isWall)
    {
        if (original == null || prefab == null || original.transform.Find("HQ_KatsBits_Visual") != null)
            return false;
        Bounds destination = original.bounds;
        GameObject visual = UnityEngine.Object.Instantiate(prefab, original.transform);
        visual.name = "HQ_KatsBits_Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = isWall && destination.size.z > destination.size.x
            ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        // Prefabs imports : la mise a l'echelle est calculee sur les bounds reels.
        Bounds from = RenderBounds(visual);
        // Les dalles peuvent etre plates (bounds Y = 0) : il faut quand meme
        // les garder visibles et ne pas revenir aux cubes d'origine.
        if (from.size.x > Epsilon && from.size.z > Epsilon)
        {
            Vector3 factor = new Vector3(destination.size.x / from.size.x,
                from.size.y > Epsilon ? destination.size.y / from.size.y : 1f,
                destination.size.z / from.size.z);
            visual.transform.localScale = Vector3.Scale(visual.transform.localScale, factor);
            Bounds updated = RenderBounds(visual);
            visual.transform.position += destination.center - updated.center;
            // Lorsqu'on decouvre une salle ou qu'une porte s'ouvre, le parent
            // est masque/affiche par le generateur : l'enfant suit automatiquement.
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null) mats[i] = Tinted(mats[i], tint);
                r.sharedMaterials = mats;
            }
            original.enabled = false;
            return true;
        }
        UnityEngine.Object.Destroy(visual);
        return false;
    }

    private static Bounds RenderBounds(GameObject root)
    {
        var rs = root.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds();
        Bounds total = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) total.Encapsulate(rs[i].bounds);
        return total;
    }

    private static Material GetMaterial(GameObject asset)
    {
        if (asset == null) return null;
        Renderer renderer = asset.GetComponentInChildren<Renderer>(true);
        return renderer == null ? null : renderer.sharedMaterial;
    }

    private static Material Tinted(Material original, Color tint)
    {
        if (original == null) return null;
        string key = original.GetInstanceID().ToString() + ":" + ColorUtility.ToHtmlStringRGBA(tint);
        Material cached;
        if (MaterialCache.TryGetValue(key, out cached) && cached != null) return cached;
        Material m = new Material(original);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null && m.shader != shader)
        {
            Texture tex = original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") :
                original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : null;
            Texture normal = original.HasProperty("_BumpMap") ? original.GetTexture("_BumpMap") : null;
            m.shader = shader;
            if (tex != null) m.SetTexture("_BaseMap", tex);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        else if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.17f);
        MaterialCache[key] = m;
        return m;
    }

    private static Color Hex(string value)
    {
        Color c;
        return ColorUtility.TryParseHtmlString("#" + value, out c) ? c : Color.white;
    }
}
