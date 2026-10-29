using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Applique au premier pack 3D (KatsBits / Material.zip) une palette proche
// du plateau de reference fourni par l'utilisateur : bordures/couloirs pierre
// claire et salles bleu, rouge, vert, jaune, orange et gris.
public static class HeroQuestReferencePaletteInstaller
{
    private const string Pack = "Assets/KatsBits-fantasy-base/";
    private const string ScenePath = "Assets/Scenes/TestGameplayScene.unity";
    private const string BoardPath = "Assets/Data/Board/board.txt";
    private const string PaletteRoot = "Assets/HeroQuestReferencePalette";
    private const string PaletteMaterials = PaletteRoot + "/Materials";

    [MenuItem("HeroQuest/Appliquer la palette coloree du plateau de reference")]
    public static void Install()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError("HeroQuest : scene introuvable : " + ScenePath);
            return;
        }
        if (!AssetDatabase.IsValidFolder(Pack.TrimEnd('/')))
        {
            Debug.LogError("HeroQuest : le premier pack 3D n'est pas installe. " +
                           "Installez d'abord le pack de Material.zip dans Assets/KatsBits-fantasy-base/.");
            return;
        }

        GameObject floor = Prefab("Prefabs/Ground/Paving.Slabs [1x1].prefab");
        GameObject corridor = Prefab("Prefabs/Ground/Paving.Stone.A [1x1].prefab");
        GameObject wall = Prefab("Prefabs/Walls_plain/Wall.2U.prefab");
        if (wall == null) wall = Prefab("Prefabs/Walls_grey/Wall.2U.prefab");
        GameObject door = Prefab("Prefabs/Doors/Prop.Door.Sgl.prefab");
        GameObject stairs = Prefab("Prefabs/Ground/Steps [1x1].prefab");
        GameObject blocked = Prefab("Prefabs/Walls/Wall.Blk.prefab");

        if (floor == null || corridor == null || wall == null || door == null)
        {
            Debug.LogError("HeroQuest : certains prefabs KatsBits sont introuvables. " +
                           "Verifiez le contenu de Assets/KatsBits-fantasy-base/.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ConvertPackMaterialsToURP();
        EnsureFolders();

        Material border = CloneAndTint("Border_Stone", Pack + "Materials/paving.mat", Hex("DCD5C9"), 0.10f);
        Material corridorMat = CloneAndTint("Corridor_Stone", Pack + "Materials/wall_grey.mat", Hex("CFCBC2"), 0.12f);
        Material deepGrey = CloneAndTint("Room_Grey", Pack + "Materials/wall_grey.mat", Hex("6B6F76"), 0.12f);
        Material mossGreen = CloneAndTint("Room_Green", Pack + "Materials/wall_green.mat", Hex("9BA35B"), 0.12f);
        Material tealBlue = CloneAndTint("Room_Teal", Pack + "Materials/wall_default.mat", Hex("6DA9B0"), 0.12f);
        Material paleBlue = CloneAndTint("Room_Blue", Pack + "Materials/wall_grey.mat", Hex("A7B8C8"), 0.12f);
        Material dustyRed = CloneAndTint("Room_Red", Pack + "Materials/wall_red.mat", Hex("B36A66"), 0.12f);
        Material mustard = CloneAndTint("Room_Mustard", Pack + "Materials/wall_yellow.mat", Hex("C3AA57"), 0.12f);
        Material ochre = CloneAndTint("Room_Ochre", Pack + "Materials/wall_yellow.mat", Hex("D0B262"), 0.12f);
        Material terracotta = CloneAndTint("Room_Orange", Pack + "Materials/wall_orange.mat", Hex("C98B59"), 0.12f);
        Material salmon = CloneAndTint("Room_Salmon", Pack + "Materials/wall_red.mat", Hex("C59A8D"), 0.12f);
        Material slate = CloneAndTint("Room_Slate", Pack + "Materials/wall_grey.mat", Hex("8D97A7"), 0.12f);

        Material[] cycle = new Material[]
        {
            deepGrey, mossGreen, dustyRed, tealBlue, paleBlue,
            mustard, ochre, terracotta, salmon, slate
        };

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BoardBuilder board = UnityEngine.Object.FindFirstObjectByType<BoardBuilder>();
        if (board == null)
        {
            GameObject host = new GameObject("Board");
            board = host.AddComponent<BoardBuilder>();
        }
        if (board.boardFile == null)
            board.boardFile = AssetDatabase.LoadAssetAtPath<TextAsset>(BoardPath);

        board.floorPrefab = floor;
        board.wallPrefab = wall;
        board.roomMaterial = border;
        board.corridorMaterial = corridorMat;

        List<ZoneStyle> zones = new List<ZoneStyle>();
        zones.Add(new ZoneStyle { zone = ".", floorPrefab = corridor, material = corridorMat });
        for (int i = 1; i <= 22; i++)
        {
            string zoneName = "S" + i;
            Material mat = cycle[(i - 1) % cycle.Length];
            zones.Add(new ZoneStyle { zone = zoneName, floorPrefab = floor, material = mat });
        }
        board.zoneStyles = zones.ToArray();
        EditorUtility.SetDirty(board);

        QuestView view = board.GetComponent<QuestView>();
        if (view == null) view = board.gameObject.AddComponent<QuestView>();
        view.board = board;
        view.doorPrefab = door;
        view.secretDoorPrefab = door;
        view.stairsPrefab = stairs;
        view.blockedPrefab = blocked;
        EnsureFurniture(view);
        EditorUtility.SetDirty(view);

        GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (manager != null)
        {
            SerializedObject so = new SerializedObject(manager);
            SerializedProperty p = so.FindProperty("boardBuilder");
            if (p != null) p.objectReferenceValue = board;
            p = so.FindProperty("questView");
            if (p != null) p.objectReferenceValue = view;
            p = so.FindProperty("loadQuestContent");
            if (p != null) p.boolValue = true;
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("HeroQuest : palette coloree appliquee. Le premier pack 3D garde ses modeles " +
                  "mais ses couleurs sont maintenant proches du plateau de reference.");
    }

    private static void EnsureFurniture(QuestView view)
    {
        List<FurniturePrefab> furniture = new List<FurniturePrefab>();
        if (view.furniturePrefabs != null) furniture.AddRange(view.furniturePrefabs);
        SetFurniture(furniture, FurnitureType.EtabliAlchimiste, Prefab("Prefabs/Props/Prop.Cauldron.prefab"));
        SetFurniture(furniture, FurnitureType.Tombeau, Prefab("Prefabs/Props/Prop.Plinth.Lrg.prefab"));
        SetFurniture(furniture, FurnitureType.TableSorcier, Prefab("Prefabs/Props/Prop.Altar.Lrg.A.prefab"));
        SetFurniture(furniture, FurnitureType.Cheminee, Prefab("Prefabs/Props/Prop.Fire.Med.prefab"));
        view.furniturePrefabs = furniture.ToArray();
    }

    private static void SetFurniture(List<FurniturePrefab> furniture, FurnitureType type, GameObject prefab)
    {
        if (prefab == null) return;
        foreach (FurniturePrefab f in furniture)
        {
            if (f != null && f.type == type)
            {
                f.prefab = prefab;
                return;
            }
        }
        furniture.Add(new FurniturePrefab { type = type, prefab = prefab });
    }

    private static GameObject Prefab(string relative)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(Pack + relative);
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(PaletteRoot))
            AssetDatabase.CreateFolder("Assets", "HeroQuestReferencePalette");
        if (!AssetDatabase.IsValidFolder(PaletteMaterials))
            AssetDatabase.CreateFolder(PaletteRoot, "Materials");
    }

    private static Material CloneAndTint(string name, string sourcePath, Color color, float smoothness)
    {
        Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
        if (source == null)
        {
            Debug.LogError("HeroQuest : materiau source introuvable : " + sourcePath);
            return null;
        }

        string assetPath = PaletteMaterials + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            material = new Material(source);
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else
        {
            EditorUtility.CopySerialized(source, material);
        }

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit != null) material.shader = lit;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Color Hex(string hex)
    {
        Color color;
        if (!ColorUtility.TryParseHtmlString("#" + hex, out color)) color = Color.white;
        return color;
    }

    private static void ConvertPackMaterialsToURP()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            Debug.LogWarning("HeroQuest : shader URP/Lit introuvable. Si les objets sont roses, verifiez le package Universal RP.");
            return;
        }
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { Pack + "Materials" });
        foreach (string guid in guids)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material == null || material.shader == lit) continue;
            Texture albedo = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            Texture normal = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            float normalStrength = material.HasProperty("_BumpScale") ? material.GetFloat("_BumpScale") : 1f;
            material.shader = lit;
            if (albedo != null) material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", color);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", normalStrength);
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetFloat("_Smoothness", 0.20f);
            EditorUtility.SetDirty(material);
        }
    }
}
