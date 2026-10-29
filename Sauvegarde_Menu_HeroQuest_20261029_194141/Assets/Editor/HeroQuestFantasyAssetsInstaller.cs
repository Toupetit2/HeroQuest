using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Integration du pack 3D KatsBits au plateau HeroQuest.
// Ne remplace aucun script de gameplay et n'ecrase pas les scenes de menu.
public static class HeroQuestFantasyAssetsInstaller
{
    private const string Pack = "Assets/KatsBits-fantasy-base/";
    private const string GameScene = "Assets/Scenes/TestGameplayScene.unity";
    private const string BoardText = "Assets/Data/Board/board.txt";

    [MenuItem("HeroQuest/Installer les nouveaux decors 3D")]
    public static void Install()
    {
        if (!File.Exists(GameScene))
        {
            Debug.LogError("HeroQuest : scene introuvable : " + GameScene);
            return;
        }

        GameObject floor = Prefab("Prefabs/Ground/Paving.Slabs [1x1].prefab");
        GameObject corridor = Prefab("Prefabs/Ground/Paving.Stone.A [1x1].prefab");
        GameObject wall = Prefab("Prefabs/Walls/Wall.2U.prefab");
        GameObject door = Prefab("Prefabs/Doors/Prop.Door.Sgl.prefab");
        if (floor == null || corridor == null || wall == null || door == null)
        {
            Debug.LogError("HeroQuest : le pack KatsBits est incomplet. Copiez le dossier " + Pack + " avec ses .meta.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Le projet utilise URP ; les anciens materiaux Standard du pack
        // doivent etre convertis pour eviter des objets roses.
        ConvertPackMaterialsToURP();

        Scene scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        BoardBuilder board = UnityEngine.Object.FindFirstObjectByType<BoardBuilder>();
        if (board == null)
        {
            GameObject host = new GameObject("Board");
            board = host.AddComponent<BoardBuilder>();
            board.boardFile = AssetDatabase.LoadAssetAtPath<TextAsset>(BoardText);
        }
        if (board.boardFile == null)
            board.boardFile = AssetDatabase.LoadAssetAtPath<TextAsset>(BoardText);

        board.floorPrefab = floor;
        board.wallPrefab = wall;
        // Si un ancien materiau de salle est encore affecte, il ecrase
        // toutes les textures du nouveau prefab : on le vide.
        board.roomMaterial = null;
        board.corridorMaterial = null;

        List<ZoneStyle> styles = new List<ZoneStyle>();
        if (board.zoneStyles != null) styles.AddRange(board.zoneStyles);
        SetZone(styles, ".", corridor);
        // Quelques salles avec une variante subtile du dallage.
        GameObject otherFloor = Prefab("Prefabs/Ground/Paving.Stone.B [1x1].prefab");
        if (otherFloor != null)
        {
            foreach (string name in new[] { "S3", "S7", "S12", "S16", "S21" })
                SetZone(styles, name, otherFloor);
        }
        board.zoneStyles = styles.ToArray();
        EditorUtility.SetDirty(board);

        QuestView view = board.GetComponent<QuestView>();
        if (view == null) view = board.gameObject.AddComponent<QuestView>();
        view.board = board;
        view.doorPrefab = door;
        // Une porte secrete revelee doit rester visuellement une porte.
        view.secretDoorPrefab = door;
        view.stairsPrefab = Prefab("Prefabs/Ground/Steps [1x1].prefab");
        view.blockedPrefab = Prefab("Prefabs/Walls/Wall.Blk.prefab");

        List<FurniturePrefab> furniture = new List<FurniturePrefab>();
        if (view.furniturePrefabs != null) furniture.AddRange(view.furniturePrefabs);
        SetFurniture(furniture, FurnitureType.EtabliAlchimiste, Prefab("Prefabs/Props/Prop.Cauldron.prefab"));
        SetFurniture(furniture, FurnitureType.Tombeau, Prefab("Prefabs/Props/Prop.Plinth.Lrg.prefab"));
        SetFurniture(furniture, FurnitureType.TableSorcier, Prefab("Prefabs/Props/Prop.Altar.Lrg.A.prefab"));
        SetFurniture(furniture, FurnitureType.Cheminee, Prefab("Prefabs/Props/Prop.Fire.Med.prefab"));
        view.furniturePrefabs = furniture.ToArray();
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
        Debug.Log("HeroQuest : nouveaux decors 3D installes dans TestGameplayScene. " +
                  "Les sols, murs, portes, escaliers et plusieurs meubles utilisent le pack KatsBits. " +
                  "Cliquez Play pour verifier le plateau.");
    }

    private static GameObject Prefab(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(Pack + path);
    }

    private static void SetZone(List<ZoneStyle> styles, string zone, GameObject prefab)
    {
        if (prefab == null) return;
        foreach (ZoneStyle s in styles)
        {
            if (s != null && s.zone == zone)
            {
                s.floorPrefab = prefab;
                s.material = null;
                return;
            }
        }
        styles.Add(new ZoneStyle { zone = zone, floorPrefab = prefab });
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

    private static void ConvertPackMaterialsToURP()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            Debug.LogWarning("HeroQuest : shader URP/Lit introuvable. " +
                             "Si les objets sont roses, verifiez le package Universal RP.");
            return;
        }
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { Pack + "Materials" });
        int changed = 0;
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
            material.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(material);
            changed++;
        }
        if (changed > 0) Debug.Log("HeroQuest : " + changed + " materiaux adaptes a URP.");
    }
}
