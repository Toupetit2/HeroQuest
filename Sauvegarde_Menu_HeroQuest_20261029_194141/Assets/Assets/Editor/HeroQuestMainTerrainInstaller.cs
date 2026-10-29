using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Integre le decor KatsBits + palette coloree a la scene de jeu de la branche main.
// Ne touche PAS au MainMenu.unity, MainMenu.cs, GameManager.cs ni UIManager.cs de main.
public static class HeroQuestMainTerrainInstaller
{
    const string MenuScene = "Assets/Scenes/MainMenu.unity";
    const string PlayScene = "Assets/Scenes/TestGameplayScene.unity";
    const string Pack = "Assets/KatsBits-fantasy-base/";
    const string PaletteFolder = "Assets/HeroQuest_Main_Terrain";
    const string PaletteMaterials = PaletteFolder + "/Materials";

    // Permet a tous les collaborateurs de lancer Play sur le vrai menu de main.
    [InitializeOnLoadMethod]
    private static void ConfigureStartupAfterImport()
    {
        EditorApplication.delayCall += SetPlayStartScene;
    }

    [MenuItem("HeroQuest/Integration main/1 - Demarrer sur le menu de main")]
    public static void SetPlayStartScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene);
        var gameplay = AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayScene);
        if (menu == null || gameplay == null)
        {
            Debug.LogWarning("HeroQuest : les scenes de main sont absentes (MainMenu / TestGameplayScene). " +
                "Ouvrez le projet sur la branche main avant l'installation.");
            return;
        }
        if (EditorSceneManager.playModeStartScene != menu)
            EditorSceneManager.playModeStartScene = menu;

        // Le MainMenu.cs de main fait SceneManager.LoadSceneAsync(1).
        // Garantir que l'index 1 correspond a TestGameplayScene, sans perdre les autres scenes.
        EditorBuildSettingsScene[] before = EditorBuildSettings.scenes;
        List<EditorBuildSettingsScene> ordered = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScene, true),
            new EditorBuildSettingsScene(PlayScene, true)
        };
        foreach (var scene in before)
            if (!string.Equals(scene.path, MenuScene, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(scene.path, PlayScene, StringComparison.OrdinalIgnoreCase))
                ordered.Add(scene);

        bool same = ordered.Count == before.Length;
        if (same)
            for (int i = 0; i < ordered.Count; i++)
                if (ordered[i].path != before[i].path || ordered[i].enabled != before[i].enabled)
                { same = false; break; }
        if (!same) EditorBuildSettings.scenes = ordered.ToArray();
    }

    [MenuItem("HeroQuest/Integration main/2 - Installer le terrain 3D colore")]
    public static void InstallTerrain()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("HeroQuest : arretez le mode Play avant d'installer le terrain.");
            return;
        }
        if (!File.Exists(PlayScene))
        {
            Debug.LogError("HeroQuest : scene de jeu introuvable : " + PlayScene);
            return;
        }
        GameObject floor = LoadPrefab("Prefabs/Ground/Paving.Slabs [1x1].prefab");
        GameObject corridor = LoadPrefab("Prefabs/Ground/Paving.Stone.A [1x1].prefab");
        GameObject wall = LoadPrefab("Prefabs/Walls_plain/Wall.2U.prefab");
        if (wall == null) wall = LoadPrefab("Prefabs/Walls/Wall.2U.prefab");
        GameObject door = LoadPrefab("Prefabs/Doors/Prop.Door.Sgl.prefab");
        GameObject stairs = LoadPrefab("Prefabs/Ground/Steps [1x1].prefab");
        GameObject blocked = LoadPrefab("Prefabs/Walls/Wall.Blk.prefab");
        if (floor == null || corridor == null || wall == null || door == null)
        {
            Debug.LogError("HeroQuest : prefabs du 1er pack manquants dans " + Pack);
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ConvertMaterialsToURP();
        EnsureFolders();
        Material corridorMaterial = Recolor("Couloir_Pierre_Claire", "paving", "C8C4BB");
        Material defaultFloor = Recolor("Sol_Pierre", "paving", "D8D2C7");
        string[] sourceMaterials = { "wall_grey", "wall_green", "wall_red", "wall_default", "wall_grey", "wall_yellow", "wall_yellow", "wall_orange", "wall_red", "wall_grey" };
        string[] colorHex = { "73767D", "9AA35A", "B77472", "6AA5AF", "A6B7C7", "C0A153", "D2B45C", "CE915F", "C09B93", "929EAA" };
        string[] materialNames = { "Gris", "Vert", "Rouge", "Bleu_Vert", "Bleu_Clair", "Moutarde", "Jaune", "Orange", "Rose", "Bleu_Gris" };
        Material[] shades = new Material[colorHex.Length];
        for (int i = 0; i < shades.Length; i++)
            shades[i] = Recolor("Salle_" + materialNames[i], sourceMaterials[i], colorHex[i]);
        if (corridorMaterial == null || defaultFloor == null)
        {
            Debug.LogError("HeroQuest : impossible de creer les materiaux de sol.");
            return;
        }
        foreach (Material m in shades) if (m == null) { Debug.LogError("Materiau de salle introuvable."); return; }

        Scene scene = EditorSceneManager.OpenScene(PlayScene, OpenSceneMode.Single);
        // Dans main, ces composants sont deja presents sur deux GameObjects distincts.
        BoardBuilder board = UnityEngine.Object.FindFirstObjectByType<BoardBuilder>();
        QuestView questView = UnityEngine.Object.FindFirstObjectByType<QuestView>();
        GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (board == null || questView == null || manager == null)
        {
            Debug.LogError("HeroQuest : scene de main incomplete : GameManager, BoardBuilder ou QuestView introuvable. " +
                "Aucune modification de scene sauvegardee.");
            return;
        }
        if (board.boardFile == null)
            board.boardFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/Board/board.txt");
        if (board.boardFile == null)
        {
            Debug.LogError("HeroQuest : fichier de plateau manquant. Aucune modification de scene sauvegardee.");
            return;
        }

        board.floorPrefab = floor;
        board.wallPrefab = wall;
        board.roomMaterial = defaultFloor;
        board.corridorMaterial = corridorMaterial;
        List<ZoneStyle> styles = new List<ZoneStyle> {
            new ZoneStyle { zone = ".", floorPrefab = corridor, material = corridorMaterial }
        };
        for (int i = 1; i <= 22; i++)
            styles.Add(new ZoneStyle { zone = "S" + i, floorPrefab = floor, material = shades[(i - 1) % shades.Length] });
        board.zoneStyles = styles.ToArray();
        EditorUtility.SetDirty(board);

        questView.board = board;
        questView.doorPrefab = door;
        questView.secretDoorPrefab = door;
        questView.stairsPrefab = stairs;
        questView.blockedPrefab = blocked;
        SetFurniture(questView);
        EditorUtility.SetDirty(questView);

        // Important : le GameManager de main expose deja ces deux champs serialises.
        // On lui affecte le plateau existant sans modifier le script ni son UIManager.
        SerializedObject gm = new SerializedObject(manager);
        SerializedProperty boardProperty = gm.FindProperty("boardBuilder");
        SerializedProperty questProperty = gm.FindProperty("questView");
        if (boardProperty == null || questProperty == null)
        {
            Debug.LogError("HeroQuest : GameManager different de celui attendu dans main. " +
                "Aucune modification de scene sauvegardee.");
            return;
        }
        boardProperty.objectReferenceValue = board;
        questProperty.objectReferenceValue = questView;
        gm.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        SetPlayStartScene();
        Debug.Log("HeroQuest : terrain du premier pack applique avec la palette coloree. " +
                  "GameManager et MainMenu de main conserves. Cliquez Play pour tester.");
    }

    private static GameObject LoadPrefab(string relative)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(Pack + relative);
    }

    private static void SetFurniture(QuestView view)
    {
        List<FurniturePrefab> list = new List<FurniturePrefab>();
        if (view.furniturePrefabs != null) list.AddRange(view.furniturePrefabs);
        Assign(list, FurnitureType.EtabliAlchimiste, LoadPrefab("Prefabs/Props/Prop.Cauldron.prefab"));
        Assign(list, FurnitureType.Tombeau, LoadPrefab("Prefabs/Props/Prop.Plinth.Lrg.prefab"));
        Assign(list, FurnitureType.TableSorcier, LoadPrefab("Prefabs/Props/Prop.Altar.Lrg.A.prefab"));
        Assign(list, FurnitureType.Cheminee, LoadPrefab("Prefabs/Props/Prop.Fire.Med.prefab"));
        view.furniturePrefabs = list.ToArray();
    }

    private static void Assign(List<FurniturePrefab> list, FurnitureType type, GameObject prefab)
    {
        if (prefab == null) return;
        foreach (FurniturePrefab f in list)
            if (f != null && f.type == type) { f.prefab = prefab; return; }
        list.Add(new FurniturePrefab { type = type, prefab = prefab });
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(PaletteFolder))
            AssetDatabase.CreateFolder("Assets", "HeroQuest_Main_Terrain");
        if (!AssetDatabase.IsValidFolder(PaletteMaterials))
            AssetDatabase.CreateFolder(PaletteFolder, "Materials");
    }

    private static Material Recolor(string name, string sourceName, string hex)
    {
        Material original = AssetDatabase.LoadAssetAtPath<Material>(Pack + "Materials/" + sourceName + ".mat");
        if (original == null) return null;
        string path = PaletteMaterials + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(original);
            AssetDatabase.CreateAsset(material, path);
        }
        else EditorUtility.CopySerialized(original, material);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null) material.shader = shader;
        Color color;
        if (!ColorUtility.TryParseHtmlString("#" + hex, out color)) color = Color.white;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .18f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConvertMaterialsToURP()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            Debug.LogWarning("HeroQuest : shader URP/Lit indisponible. Verifiez le pipeline de rendu.");
            return;
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Pack + "Materials" }))
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || m.shader == lit) continue;
            Texture diffuse = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Texture normal = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
            Color tint = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            m.shader = lit;
            if (diffuse != null) m.SetTexture("_BaseMap", diffuse);
            m.SetColor("_BaseColor", tint);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Smoothness", .18f);
            EditorUtility.SetDirty(m);
        }
    }
}
