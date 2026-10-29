using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// selecteur de quetes (IMGUI, aucun prefab) affiche par-dessus le MainMenu existant
//   Open(true)  : quetes de la campagne (Resources/Quests, fournies avec le jeu)
//   Open(false) : quetes creees avec l'editeur (QuestStorage)
// "Lancer la quete" memorise le choix dans QuestSelection puis charge la scene de jeu
// pendant que le selecteur ou l'editeur est ouvert, le Canvas du menu est masque
// (sinon un clic sur l'IMGUI traverserait jusqu'aux boutons du Canvas)
public class QuestPickerMenu : MonoBehaviour
{
    public string gameScene = "TestGameplayScene";    // doit etre dans les Build Settings

    class QuestInfo
    {
        public string file;
        public int number;
        public string title;
        public bool campaign;
    }

    bool isOpen;
    bool campaign = true;
    bool editorWasOpen;
    List<QuestInfo> quests = new List<QuestInfo>();
    int selected = -1;
    Vector2 scroll;
    string status = "";
    Canvas menuCanvas;
    List<Action> pending = new List<Action>();

    GUIStyle sectionStyle, infoStyle, buttonStyle, tabStyle, tabOnStyle, listStyle, selectedStyle;
    List<Texture2D> textures = new List<Texture2D>();

    public bool IsOpen { get { return isOpen; } }

    public void Open(bool campaignQuests)
    {
        campaign = campaignQuests;
        status = "";
        Refresh();
        isOpen = true;
    }

    public void Close()
    {
        isOpen = false;
    }

    void Start()
    {
        menuCanvas = FindFirstObjectByType<Canvas>();
    }

    void Update()
    {
        // l'editeur vient d'etre ferme : une quete a pu etre creee ou modifiee
        bool editorOpen = QuestEditorMenu.IsOpen;
        if (editorWasOpen && !editorOpen && isOpen) Refresh();
        editorWasOpen = editorOpen;

        if (menuCanvas != null) menuCanvas.enabled = !(isOpen || editorOpen);
    }

    void OnDisable()
    {
        if (menuCanvas != null) menuCanvas.enabled = true;
    }

    void OnDestroy()
    {
        foreach (Texture2D t in textures)
            if (t != null) Destroy(t);
    }

    void Refresh()
    {
        quests.Clear();
        selected = -1;
        scroll = Vector2.zero;

        if (campaign)
        {
            foreach (TextAsset asset in Resources.LoadAll<TextAsset>("Quests"))
                quests.Add(MakeInfo(asset.name + ".json", asset.text, true));
        }
        else
        {
            foreach (string file in QuestStorage.ListFiles())
            {
                string json = null;
                try { json = QuestStorage.Read(file); }
                catch (Exception) { }
                quests.Add(MakeInfo(file, json, false));
            }
        }

        // par numero (sans numero a la fin), puis par nom de fichier
        quests.Sort((a, b) =>
        {
            int na = a.number > 0 ? a.number : int.MaxValue;
            int nb = b.number > 0 ? b.number : int.MaxValue;
            int c = na.CompareTo(nb);
            return c != 0 ? c : string.CompareOrdinal(a.file, b.file);
        });
    }

    QuestInfo MakeInfo(string file, string json, bool isCampaign)
    {
        QuestInfo info = new QuestInfo { file = file, number = 0, title = "", campaign = isCampaign };
        try
        {
            QuestDefinition q = JsonUtility.FromJson<QuestDefinition>(json);
            if (q != null)
            {
                info.number = q.number;
                info.title = q.title;
            }
        }
        catch (Exception)
        {
            info.title = "(fichier illisible)";
        }
        return info;
    }

    // =====================================================================
    //  dessin
    // =====================================================================

    // les changements d'etat se font au debut d'un evenement Layout,
    // sinon l'IMGUI plante quand le nombre de controles change en plein dessin
    void Defer(Action action)
    {
        pending.Add(action);
    }

    void OnGUI()
    {
        if (!isOpen || QuestEditorMenu.IsOpen) return;   // l'editeur prend toute la place

        GUI.depth = -50;   // sous l'editeur (-100)

        Event e = Event.current;
        if (e.type == EventType.Layout && pending.Count > 0)
        {
            List<Action> todo = new List<Action>(pending);
            pending.Clear();
            foreach (Action a in todo) a();
        }
        if (!isOpen) return;   // une action vient de fermer le selecteur

        InitStyles();

        float scale = Mathf.Max(0.75f, Screen.height / 720f);
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        Rect area = new Rect(0f, 0f, Screen.width / scale, Screen.height / scale);

        if (e.type == EventType.Repaint) DrawBackground(area);

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            Defer(Close);
            e.Use();
        }

        float w = 460f;
        GUILayout.BeginArea(new Rect((area.width - w) / 2f, area.height * 0.10f, w, area.height * 0.84f));
        DrawQuests();
        GUILayout.EndArea();

        if (!string.IsNullOrEmpty(status)) GUI.Label(new Rect(0f, area.height - 46f, area.width, 30f), status, infoStyle);

        GUI.matrix = previousMatrix;
    }

    void DrawBackground(Rect area)
    {
        Color top = new Color(0.14f, 0.08f, 0.05f);
        Color bottom = new Color(0.03f, 0.02f, 0.02f);
        int bands = 48;
        for (int i = 0; i < bands; i++)
        {
            Fill(new Rect(0f, area.height * i / bands, area.width, area.height / bands + 1f),
                Color.Lerp(top, bottom, i / (float)(bands - 1)));
        }
    }

    static void Fill(Rect r, Color c)
    {
        Color previous = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    // bouton du menu ; renvoie false s'il est grise
    bool Btn(string text, bool enabled)
    {
        GUI.enabled = enabled;
        bool clicked = GUILayout.Button(text, buttonStyle, GUILayout.Height(48f));
        GUI.enabled = true;
        GUILayout.Space(10f);
        return clicked && enabled;
    }

    void DrawQuests()
    {
        GUILayout.Label("Choisir une quete", sectionStyle);
        GUILayout.Space(8f);

        // onglets Campagne / Mes quetes
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Campagne", campaign ? tabOnStyle : tabStyle, GUILayout.Height(36f)) && !campaign)
            Defer(() => { campaign = true; Refresh(); });
        if (GUILayout.Button("Mes quetes", !campaign ? tabOnStyle : tabStyle, GUILayout.Height(36f)) && campaign)
            Defer(() => { campaign = false; Refresh(); });
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        if (quests.Count == 0)
        {
            GUILayout.Label(campaign
                ? "Aucune quete de campagne trouvee\n(Resources/Quests)."
                : "Aucune quete personnalisee.\nCreez-en une avec l'editeur de quetes.", infoStyle);
        }
        else
        {
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(300f));
            for (int i = 0; i < quests.Count; i++)
            {
                QuestInfo info = quests[i];
                string label = (info.number > 0 ? "Quete " + info.number + " - " : "")
                    + (string.IsNullOrEmpty(info.title) ? info.file : info.title);
                if (GUILayout.Button(label, i == selected ? selectedStyle : listStyle, GUILayout.Height(38f)))
                {
                    int index = i;
                    Defer(() => selected = index);
                }
            }
            GUILayout.EndScrollView();
        }

        GUILayout.Space(12f);
        if (Btn("Lancer la quete", selected >= 0)) Defer(Play);
        if (Btn("Retour", true)) Defer(Close);
    }

    // =====================================================================
    //  actions
    // =====================================================================

    void Play()
    {
        if (selected < 0 || selected >= quests.Count) return;
        QuestInfo info = quests[selected];
        QuestSelection.Set(info.file, info.number, info.title, info.campaign);

        if (string.IsNullOrEmpty(gameScene) || !Application.CanStreamedLevelBeLoaded(gameScene))
        {
            status = "Scene \"" + gameScene + "\" introuvable dans les Build Settings (quete choisie : " + info.file + ")";
            Debug.LogWarning("QuestPickerMenu : " + status);
            return;
        }
        SceneManager.LoadScene(gameScene);
    }

    // =====================================================================
    //  styles
    // =====================================================================

    Texture2D Tex(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        textures.Add(t);
        return t;
    }

    void Backgrounds(GUIStyle s, Color normal, Color hover, Color active)
    {
        s.border = new RectOffset(0, 0, 0, 0);
        s.normal.background = Tex(normal);
        s.focused.background = s.normal.background;
        s.hover.background = Tex(hover);
        s.active.background = Tex(active);
        Color text = new Color(0.95f, 0.88f, 0.7f);
        s.normal.textColor = text;
        s.focused.textColor = text;
        s.hover.textColor = new Color(1f, 0.96f, 0.82f);
        s.active.textColor = Color.white;
    }

    void InitStyles()
    {
        if (sectionStyle != null) return;

        Color gold = new Color(1f, 0.82f, 0.3f);
        Color light = new Color(0.9f, 0.84f, 0.7f);

        sectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        sectionStyle.normal.textColor = gold;

        infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, alignment = TextAnchor.MiddleCenter };
        infoStyle.normal.textColor = light;

        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        Backgrounds(buttonStyle, new Color(0.25f, 0.14f, 0.08f), new Color(0.42f, 0.24f, 0.10f), new Color(0.6f, 0.35f, 0.12f));

        tabStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        Backgrounds(tabStyle, new Color(0.18f, 0.11f, 0.07f), new Color(0.30f, 0.18f, 0.09f), new Color(0.45f, 0.27f, 0.10f));

        tabOnStyle = new GUIStyle(tabStyle);
        Backgrounds(tabOnStyle, new Color(0.55f, 0.32f, 0.10f), new Color(0.62f, 0.37f, 0.12f), new Color(0.62f, 0.37f, 0.12f));

        listStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(16, 8, 4, 4) };
        Backgrounds(listStyle, new Color(0.18f, 0.11f, 0.07f), new Color(0.30f, 0.18f, 0.09f), new Color(0.45f, 0.27f, 0.10f));

        selectedStyle = new GUIStyle(listStyle);
        Backgrounds(selectedStyle, new Color(0.55f, 0.32f, 0.10f), new Color(0.62f, 0.37f, 0.12f), new Color(0.62f, 0.37f, 0.12f));
    }
}
