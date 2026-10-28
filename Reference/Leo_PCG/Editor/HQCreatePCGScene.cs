#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Crée une scène PCG à partir du véritable Plateau.prefab fourni par l'équipe.
// Ne reconstruit PAS les murs/sols en cubes et ne modifie PAS Git/board.txt.
public static class HQCreatePCGScene
{
    private const string MenuPath = "HeroQuest/Creer plateau";
    private const string BoardAssetPath = "Assets/Prefabs/HeroQuest/Plateau.prefab";
    private const string MapAssetPath = "Assets/Data/PCG/board_prefab.txt";
    private const string StoneAssetPath = "Assets/Prefabs/HeroQuest/HQStoneFallback.mat";

    [MenuItem(MenuPath)]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Arrête le mode Play avant de créer une scène PCG.");
            return;
        }
        GameObject originalBoard = AssetDatabase.LoadAssetAtPath<GameObject>(BoardAssetPath);
        TextAsset boardMap = AssetDatabase.LoadAssetAtPath<TextAsset>(MapAssetPath);
        if (originalBoard == null || boardMap == null)
        {
            Debug.LogError("Prefab ou carte manquante : " + BoardAssetPath + " / " + MapAssetPath);
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject board = (GameObject)PrefabUtility.InstantiatePrefab(originalBoard, scene);
        board.name = "Plateau";
        board.transform.SetPositionAndRotation(
            Vector3.zero,
            Quaternion.Euler(0f, 180f, 0f)
        );
        board.transform.localScale = Vector3.one;

        // L'archive originale contient une référence de matériau manquante.
        // Cette étape crée un matériau de secours sans modifier l'asset Plateau.prefab.
        RepairMissingMaterials(board);

        Transform walls = board.transform.Find("Walls");
        if (walls == null)
        {
            Debug.LogError("Le prefab Plateau ne contient pas d'enfant 'Walls'.", board);
            return;
        }

        HeroQuestSimplePCG pcg = board.GetComponent<HeroQuestSimplePCG>();
        if (pcg == null) pcg = board.AddComponent<HeroQuestSimplePCG>();
        pcg.boardFile = boardMap;
        pcg.wallsRoot = walls;
        pcg.firstCellLocalCenter = new Vector3(-25f, 0f, -18f);
        pcg.cellSize = 2f;
        pcg.generateOnPlay = false;

        // Pas de HQPCGBoardBuilder : le vrai plateau existe déjà.
        HQPCGBoardBuilder oldBuilder = board.GetComponent<HQPCGBoardBuilder>();
        if (oldBuilder != null) Object.DestroyImmediate(oldBuilder);

        HeroQuestPCGMenu menu = board.GetComponent<HeroQuestPCGMenu>();
        if (menu == null) menu = board.AddComponent<HeroQuestPCGMenu>();
        menu.generator = pcg;
        HQCampaignQuestLoader campaign = board.GetComponent<HQCampaignQuestLoader>();
        if (campaign == null) campaign = board.AddComponent<HQCampaignQuestLoader>();
        campaign.board = pcg;
        menu.campaign = campaign;

        GameObject cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        Camera camera = cameraGO.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 31f;
        cameraGO.transform.SetPositionAndRotation(new Vector3(0f, 65f, 0f),
                                                  Quaternion.Euler(90f, 0f, 0f));
        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        lightGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        const string scenePath = "Assets/Scenes/PCG_HeroQuest_Prefab.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Selection.activeGameObject = board;
        Debug.Log("PCG: vrai Plateau.prefab instancié, murs préservés. " +
                  "Play > Generate quest. Scène: " + scenePath, board);

        TextAsset gitMap = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/Board/board.txt");
        if (gitMap != null && gitMap.text.Trim() != boardMap.text.Trim())
            Debug.LogWarning("Git/board.txt et la carte du prefab diffèrent. " +
                "La PCG utilise Assets/Data/PCG/board_prefab.txt pour correspondre aux murs originaux. " +
                "Les données du jeu Git n'ont pas été modifiées.", board);
    }

    [MenuItem(MenuPath, true)]
    private static bool CanCreateScene() =>
        !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;

    private static void RepairMissingMaterials(GameObject board)
    {
        // Murs plus sombres pour qu'ils ressortent en vue de dessus
        Transform wallsRoot = board.transform.Find("Walls");
        if (wallsRoot != null)
        {
            const string wallMatPath = "Assets/Prefabs/HeroQuest/HQWallDark.mat";
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>(wallMatPath);
            if (wallMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                wallMat = new Material(shader) { name = "HQWallDark" };
                wallMat.color = new Color(0.23f, 0.25f, 0.30f);
                AssetDatabase.CreateAsset(wallMat, wallMatPath);
            }
            foreach (Renderer r in wallsRoot.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = wallMat;
        }
        Material fallback = AssetDatabase.LoadAssetAtPath<Material>(StoneAssetPath);
        if (fallback == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogWarning("Aucun shader pour le matériau de secours. Vérifiez vos packages URP.");
                return;
            }
            fallback = new Material(shader);
            fallback.name = "HQStoneFallback";
            fallback.color = new Color(0.55f, 0.53f, 0.49f);
            AssetDatabase.CreateAsset(fallback, StoneAssetPath);
        }

        int repaired = 0;
        foreach (Renderer renderer in board.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            bool dirty = false;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null) continue;
                materials[i] = fallback;
                dirty = true;
            }
            if (dirty)
            {
                renderer.sharedMaterials = materials;
                repaired++;
            }
        }
        if (repaired > 0)
            Debug.Log("Matériau de secours affecté à " + repaired + " éléments du prefab.");
    }
}
#endif
