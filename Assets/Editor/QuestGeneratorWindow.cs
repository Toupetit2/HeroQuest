using System.IO;
using UnityEditor;
using UnityEngine;

// fenetre d'editeur : menu HeroQuest > Generateur de quetes
// ne contient que l'hote : les donnees sont dans QuestEditorModel, l'interface dans QuestEditorGUI,
// exactement les memes que dans le menu du jeu (QuestEditorMenu)
// a placer dans Assets/Editor/
public class QuestGeneratorWindow : EditorWindow
{
    [SerializeField] QuestEditorModel model = new QuestEditorModel();   // serialise : survit a une recompilation
    QuestEditorGUI gui;

    [MenuItem("HeroQuest/Generateur de quetes")]
    static void Open()
    {
        GetWindow<QuestGeneratorWindow>("Generateur de quetes");
    }

    void OnEnable()
    {
        minSize = new Vector2(1080, 700);
        wantsMouseMove = true;

        if (model == null) model = new QuestEditorModel();
        model.EnsureInit();
        LoadBoard();
        model.RefreshFileList();
        if (!model.started)
        {
            model.started = true;
            model.NewQuest(false);
        }

        gui = new QuestEditorGUI(model);
        gui.RequestRepaint = Repaint;
        gui.ReloadBoard = () => { LoadBoard(); model.RefreshFileList(); };
    }

    void LoadBoard()
    {
        string path = Path.Combine(Application.dataPath, "Data/Board/board.txt");
        model.LoadBoard(File.Exists(path) ? File.ReadAllText(path) : null,
            "Fichier introuvable : Assets/Data/Board/board.txt");
    }

    void OnGUI()
    {
        if (gui == null) OnEnable();
        if (Event.current.type == EventType.MouseMove) Repaint();

        gui.Draw(new Rect(0f, 0f, position.width, position.height));

        if (gui.NeedsRepaint) Repaint();
    }
}
