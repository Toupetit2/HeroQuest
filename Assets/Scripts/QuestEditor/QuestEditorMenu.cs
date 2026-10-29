using UnityEngine;

// menu du jeu : ouvre l'editeur de quetes par-dessus la scene (IMGUI, aucune scene ni prefab a creer)
// 1. poser ce script sur un GameObject, 2. assigner Board File (Data/Board/board.txt)
// le futur menu principal n'a qu'a appeler Open() depuis son bouton :
//     FindFirstObjectByType<QuestEditorMenu>().Open();
// en attendant, la touche F2 ouvre / ferme l'editeur
// QuestEditorMenu.IsOpen permet au jeu d'ignorer ses propres touches pendant l'edition
public class QuestEditorMenu : MonoBehaviour
{
    public TextAsset boardFile;                 // le meme board.txt que celui utilise par le jeu
    public TextAsset[] builtInQuests;           // quetes fournies avec le jeu : copiees dans le dossier des quetes si absentes
    public KeyCode toggleKey = KeyCode.F2;      // touche de test, KeyCode.None pour la desactiver

    public static bool IsOpen { get; private set; }

    QuestEditorModel model = new QuestEditorModel();
    QuestEditorGUI gui;
    bool open;

    public void Open()
    {
        Init();
        open = true;
        IsOpen = true;
    }

    public void Close()
    {
        open = false;
        IsOpen = false;
    }

    public void Toggle()
    {
        if (open) Close();
        else Open();
    }

    void OnDisable()
    {
        if (open) Close();
    }

    void Init()
    {
        if (gui != null) return;

        SeedBuiltInQuests();
        model.EnsureInit();
        model.LoadBoard(boardFile != null ? boardFile.text : null,
            "Aucun plateau : assignez Data/Board/board.txt au champ Board File de QuestEditorMenu.");
        model.RefreshFileList();
        if (!model.started)
        {
            model.started = true;
            model.NewQuest(false);
        }

        gui = new QuestEditorGUI(model) { RequestClose = Close };
    }

    // dans un jeu compile, le dossier des quetes est vide au depart : on y copie les quetes du jeu
    public void SeedBuiltInQuests()
    {
        if (builtInQuests == null) return;

        foreach (TextAsset quest in builtInQuests)
        {
            if (quest == null) continue;
            string fileName = quest.name + ".json";
            try
            {
                if (!QuestStorage.Exists(fileName)) QuestStorage.Write(fileName, quest.text);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Quete " + fileName + " non copiee : " + e.Message);
            }
        }
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (toggleKey != KeyCode.None && e.type == EventType.KeyDown && e.keyCode == toggleKey)
        {
            Toggle();
            e.Use();
        }
        if (!open || gui == null) return;

        GUI.depth = -100;   // par-dessus les autres OnGUI

        // l'IMGUI est minuscule sur un grand ecran : on agrandit, sans descendre sous la taille minimale de l'editeur
        float scale = Mathf.Max(1f, Mathf.Min(Screen.height / 800f, Screen.width / 1100f));
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        gui.Draw(new Rect(0f, 0f, Screen.width / scale, Screen.height / scale));
        GUI.matrix = previous;
    }
}
