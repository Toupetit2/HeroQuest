using System;
using System.Collections.Generic;
using UnityEngine;

// Fonctionne dans le jeu compile, SANS UnityEditor / AssetDatabase.
// Appele directement par le GameManager de la branche main AVANT Generate().
public static class HeroQuestRuntimeTerrain
{
    private const string Folder = "HeroQuestTerrain/Prefabs/";
    private const string BoardFallback = "HeroQuestTerrain/board";
    private static readonly List<GameObject> Templates = new List<GameObject>();
    private static readonly Dictionary<Material, Material> Adapted = new Dictionary<Material, Material>();

    // Palette basee sur le plateau fourni (S1 a S22).
    private static readonly Color[] RoomColors =
    {
        Hex("8C766B"), Hex("BB706D"), Hex("659BA8"), Hex("C17E6B"),
        Hex("9FAFC2"), Hex("CEB36B"), Hex("696F77"), Hex("A2AF70"),
        Hex("D3AB57"), Hex("98B263"), Hex("9B9491"), Hex("D5B25A"),
        Hex("779EAB"), Hex("7EABB5"), Hex("9BAAB8"), Hex("D6AA59"),
        Hex("B97C67"), Hex("BA9C67"), Hex("C18972"), Hex("7D879A"),
        Hex("C48F60"), Hex("83A65E")
    };

    public static void Configure(BoardBuilder board, QuestView view)
    {
        if (board == null || view == null)
        {
            Debug.LogError("HeroQuest : BoardBuilder ou QuestView introuvable dans la scene de jeu.");
            return;
        }
        var floor = Load("RoomFloor");
        var corridor = Load("CorridorFloor");
        var wall = Load("Wall");
        var door = Load("Door");
        if (floor == null || corridor == null || wall == null || door == null)
        {
            Debug.LogWarning("HeroQuest : terrain 3D absent des Resources, " +
                             "conservation des prefabs deja affectes a la scene.");
            return;
        }

        if (board.boardFile == null)
            board.boardFile = Resources.Load<TextAsset>(BoardFallback);
        if (board.boardFile == null)
        {
            Debug.LogError("HeroQuest : donnees du plateau indisponibles (board.txt).");
            return;
        }

        board.floorPrefab = floor;
        board.wallPrefab = wall;
        Material baseFloor = GetFirstMaterial(floor);
        Material baseCorridor = GetFirstMaterial(corridor);
        board.roomMaterial = Tint(baseFloor, Hex("D6CEBF"));
        board.corridorMaterial = Tint(baseCorridor, Hex("D5CEC2"));
        List<ZoneStyle> styles = new List<ZoneStyle>();
        styles.Add(new ZoneStyle
        {
            zone = ".", floorPrefab = corridor, material = board.corridorMaterial
        });
        for (int i = 1; i <= 22; i++)
            styles.Add(new ZoneStyle
            {
                zone = "S" + i, floorPrefab = floor,
                material = Tint(baseFloor, RoomColors[i - 1])
            });
        board.zoneStyles = styles.ToArray();

        view.board = board;
        view.doorPrefab = door;
        view.secretDoorPrefab = door;
        view.stairsPrefab = Load("Stairs");
        view.blockedPrefab = Load("Blocked");
        var furniture = new List<FurniturePrefab>();
        if (view.furniturePrefabs != null)
            foreach (var item in view.furniturePrefabs)
                if (item != null) furniture.Add(item);
        SetFurniture(furniture, FurnitureType.EtabliAlchimiste, Load("Cauldron"));
        SetFurniture(furniture, FurnitureType.Tombeau, Load("Tomb"));
        SetFurniture(furniture, FurnitureType.TableSorcier, Load("Altar"));
        SetFurniture(furniture, FurnitureType.Cheminee, Load("Fire"));
        view.furniturePrefabs = furniture.ToArray();
        Debug.Log("HeroQuest : terrain KatsBits colore installe par GameManager a l'execution.");
    }

    private static void SetFurniture(List<FurniturePrefab> list, FurnitureType kind, GameObject prefab)
    {
        if (prefab == null) return;
        foreach (var f in list)
            if (f.type == kind) { f.prefab = prefab; return; }
        list.Add(new FurniturePrefab { type = kind, prefab = prefab });
    }

    // Un exemplaire hors du plateau est conserve comme modele. Les instances
    // generees par BoardBuilder / QuestView heritent de ses materiaux URP.
    private static GameObject Load(string name)
    {
        GameObject asset = Resources.Load<GameObject>(Folder + name);
        if (asset == null)
        {
            Debug.LogWarning("HeroQuest : modele manquant : " + Folder + name);
            return null;
        }
        var prototype = UnityEngine.Object.Instantiate(
            asset, new Vector3(10000f + Templates.Count * 15f, -10000f, 10000f),
            Quaternion.identity);
        prototype.name = "HQ_Runtime_Template_" + name;
        UnityEngine.Object.DontDestroyOnLoad(prototype);
        foreach (Renderer r in prototype.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = r.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = Adapt(materials[i]);
            r.sharedMaterials = materials;
        }
        Templates.Add(prototype);
        return prototype;
    }

    private static Material GetFirstMaterial(GameObject go)
    {
        if (go == null) return null;
        var renderer = go.GetComponentInChildren<Renderer>(true);
        return renderer != null ? renderer.sharedMaterial : null;
    }

    private static Material Adapt(Material original)
    {
        if (original == null) return null;
        Material cached;
        if (Adapted.TryGetValue(original, out cached) && cached != null) return cached;
        Material copy = new Material(original);
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit != null && copy.shader != lit)
        {
            Texture albedo = original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") :
                             original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : null;
            Texture normal = original.HasProperty("_BumpMap") ? original.GetTexture("_BumpMap") : null;
            Color color = original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white;
            copy.shader = lit;
            if (albedo != null) copy.SetTexture("_BaseMap", albedo);
            copy.SetColor("_BaseColor", color);
            if (normal != null)
            {
                copy.SetTexture("_BumpMap", normal);
                copy.EnableKeyword("_NORMALMAP");
            }
        }
        if (copy.HasProperty("_Smoothness")) copy.SetFloat("_Smoothness", .18f);
        Adapted.Add(original, copy);
        return copy;
    }

    private static Material Tint(Material original, Color tint)
    {
        Material basis = Adapt(original);
        if (basis == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            basis = new Material(shader);
        }
        Material copy = new Material(basis);
        if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", tint);
        else if (copy.HasProperty("_Color")) copy.SetColor("_Color", tint);
        return copy;
    }

    private static Color Hex(string value)
    {
        Color output;
        return ColorUtility.TryParseHtmlString("#" + value, out output) ? output : Color.white;
    }
}
