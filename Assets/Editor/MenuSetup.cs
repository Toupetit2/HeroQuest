using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// menu Unity : HeroQuest > Installer le menu et le plateau
// - MainMenu : ajoute un objet "MenusQuetes" (QuestEditorMenu + QuestPickerMenu) et les relie au script MainMenu
//   les scenes existantes ne sont PAS remplacees : seuls des objets sont ajoutes
// - TestGameplayScene : ajoute un objet "Board" (BoardBuilder + QuestView), le relie au GameManager,
//   et pose un CameraController sur la camera s'il n'y en a pas
// - Build Settings : ajoute les deux scenes si elles manquent (sans changer l'ordre existant)
// peut etre relance sans danger : ce qui existe deja est reutilise
// a placer dans Assets/Editor/
public static class MenuSetup
{
    const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Scenes/TestGameplayScene.unity";
    const string BoardPath = "Assets/Data/Board/board.txt";

    [MenuItem("HeroQuest/Installer le menu et le plateau")]
    static void Install()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        TextAsset boardFile = AssetDatabase.LoadAssetAtPath<TextAsset>(BoardPath);
        if (boardFile == null) Debug.LogWarning("Plateau introuvable : " + BoardPath + " (a assigner a la main dans BoardBuilder et QuestEditorMenu)");

        SetupMenuScene(boardFile);
        SetupGameScene(boardFile);
        AddToBuildSettings();

        // on termine sur le menu : c'est de la qu'on lance le jeu
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        Debug.Log("Installation terminee. Ouvrez MainMenu et lancez Play.");
    }

    static void SetupMenuScene(TextAsset boardFile)
    {
        if (!System.IO.File.Exists(MenuScenePath))
        {
            Debug.LogWarning("Scene introuvable : " + MenuScenePath);
            return;
        }
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

        GameObject host = FindOrCreate("MenusQuetes");

        QuestEditorMenu editor = host.GetComponent<QuestEditorMenu>();
        if (editor == null) editor = host.AddComponent<QuestEditorMenu>();
        if (editor.boardFile == null) editor.boardFile = boardFile;

        QuestPickerMenu picker = host.GetComponent<QuestPickerMenu>();
        if (picker == null) picker = host.AddComponent<QuestPickerMenu>();
        picker.gameScene = "TestGameplayScene";

        MainMenu menu = Object.FindFirstObjectByType<MainMenu>();
        if (menu != null)
        {
            menu.questPicker = picker;
            menu.questEditor = editor;
            EditorUtility.SetDirty(menu);
        }
        else Debug.LogWarning("Aucun script MainMenu dans la scene du menu : les boutons ne seront pas relies.");

        Save();
    }

    static void SetupGameScene(TextAsset boardFile)
    {
        if (!System.IO.File.Exists(GameScenePath))
        {
            Debug.LogWarning("Scene introuvable : " + GameScenePath);
            return;
        }
        EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        BoardBuilder board = Object.FindFirstObjectByType<BoardBuilder>();
        if (board == null)
        {
            GameObject go = new GameObject("Board");
            board = go.AddComponent<BoardBuilder>();
        }
        if (board.boardFile == null) board.boardFile = boardFile;

        QuestView view = board.GetComponent<QuestView>();
        if (view == null) view = board.gameObject.AddComponent<QuestView>();
        view.board = board;

        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        if (manager != null)
        {
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("boardBuilder").objectReferenceValue = board;
            so.FindProperty("questView").objectReferenceValue = view;
            so.ApplyModifiedProperties();
        }
        else Debug.LogWarning("Aucun GameManager dans la scene de jeu.");

        // la camera doit connaitre le plateau
        CameraController cam = Object.FindFirstObjectByType<CameraController>();
        if (cam == null && Camera.main != null) cam = Camera.main.gameObject.AddComponent<CameraController>();
        if (cam != null)
        {
            cam.board = board;
            EditorUtility.SetDirty(cam);
        }

        Save();
    }

    static GameObject FindOrCreate(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go : new GameObject(name);
    }

    static void Save()
    {
        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // les scenes qui manquent sont ajoutees a la fin : l'ordre existant ne change pas
    // (PlayCampaign charge l'index 1 quand il n'y a pas de selecteur)
    static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (string path in new[] { MenuScenePath, GameScenePath })
        {
            if (!System.IO.File.Exists(path)) continue;
            if (!scenes.Exists(x => x.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
