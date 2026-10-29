using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Deux parcours independants : campagne lineaire, puis rejeu libre apres sa fin ;
// quetes personnelles toujours librement accessibles (QuestStorage).
// Palette HeroQuest : brun sombre, boutons bois et accents dores.
// La progression et le rejeu des chapitres ne sont pas modifies.
public class QuestPickerMenu : MonoBehaviour
{
    public string gameScene = "TestGameplayScene";

    class QuestInfo
    {
        public string file;
        public int number;
        public string title;
    }

    static readonly Color DarkBrown = new Color(0.14f, 0.08f, 0.05f);
    static readonly Color Ivory = new Color(0.95f, 0.88f, 0.70f);
    static readonly Color Gold = new Color(1.00f, 0.82f, 0.30f);
    static readonly Color SoftBrown = new Color(0.30f, 0.18f, 0.09f);

    bool isOpen;
    bool campaign = true;
    bool editorWasOpen;
    bool confirmReset;
    int selected = -1;
    int selectedReplay = -1;
    Vector2 scroll;
    string status = "";
    Canvas menuCanvas;
    readonly List<QuestInfo> quests = new List<QuestInfo>();
    readonly List<Action> pending = new List<Action>();
    readonly List<Texture2D> textures = new List<Texture2D>();

    GUIStyle titleStyle, sectionStyle, infoStyle, smallStyle;
    GUIStyle buttonStyle, listStyle, selectedStyle, dangerStyle;

    public bool IsOpen { get { return isOpen; } }

    public void OpenCampaign() { Open(true); }
    public void OpenPersonalQuests() { Open(false); }

    // Conserve l'ancienne signature utilisee par d'autres scripts ou scenes Unity.
    public void Open(bool campaignQuests)
    {
        campaign = campaignQuests;
        confirmReset = false;
        status = "";
        Refresh();
        isOpen = true;
    }

    public void Close()
    {
        isOpen = false;
        confirmReset = false;
    }

    void Start()
    {
        menuCanvas = FindFirstObjectByType<Canvas>();
    }

    void Update()
    {
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
        foreach (Texture2D texture in textures)
            if (texture != null) Destroy(texture);
    }

    void Refresh()
    {
        quests.Clear();
        selected = -1;
        selectedReplay = campaign && CampaignProgress.IsFinished ? 0 : -1;
        scroll = Vector2.zero;

        if (!campaign)
        {
            foreach (string file in QuestStorage.ListFiles())
            {
                string json = null;
                try { json = QuestStorage.Read(file); }
                catch (Exception) { }
                quests.Add(MakeInfo(file, json));
            }
        }
        quests.Sort((a, b) =>
        {
            int na = a.number > 0 ? a.number : int.MaxValue;
            int nb = b.number > 0 ? b.number : int.MaxValue;
            int comparison = na.CompareTo(nb);
            return comparison != 0 ? comparison : string.CompareOrdinal(a.file, b.file);
        });
    }

    QuestInfo MakeInfo(string file, string json)
    {
        QuestInfo info = new QuestInfo { file = file, number = 0, title = "" };
        try
        {
            QuestDefinition quest = JsonUtility.FromJson<QuestDefinition>(json);
            if (quest != null)
            {
                info.number = quest.number;
                info.title = quest.title;
            }
        }
        catch (Exception) { info.title = "(fichier illisible)"; }
        return info;
    }

    // Les changements structurels s'executent sur Layout pour eviter les erreurs IMGUI.
    void Defer(Action action) { pending.Add(action); }

    void OnGUI()
    {
        if (!isOpen || QuestEditorMenu.IsOpen) return;
        GUI.depth = -50;
        Event current = Event.current;
        if (current.type == EventType.Layout && pending.Count > 0)
        {
            List<Action> todo = new List<Action>(pending);
            pending.Clear();
            foreach (Action action in todo) action();
        }
        if (!isOpen) return;

        InitStyles();
        float scale = Mathf.Max(0.72f, Screen.height / 720f);
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        Rect screen = new Rect(0f, 0f, Screen.width / scale, Screen.height / scale);

        if (current.type == EventType.Repaint) DrawBackground(screen);
        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
        {
            Defer(Close);
            current.Use();
        }

        float width = Mathf.Min(765f, screen.width - 30f);
        float height = Mathf.Min(682f, screen.height - 28f);
        Rect panel = new Rect((screen.width - width) / 2f, (screen.height - height) / 2f, width, height);
        if (current.type == EventType.Repaint) DrawPanel(panel);
        GUILayout.BeginArea(new Rect(panel.x + 25f, panel.y + 20f, panel.width - 50f, panel.height - 38f));
        DrawContent();
        GUILayout.EndArea();

        GUI.matrix = oldMatrix;
    }

    void DrawBackground(Rect area)
    {
        // Degrade discret identique au rendu d'origine des sous-menus.
        Color bottom = new Color(0.03f, 0.02f, 0.02f);
        const int bands = 48;
        for (int i = 0; i < bands; i++)
            Fill(new Rect(0f, area.height * i / bands, area.width, area.height / bands + 1f),
                 Color.Lerp(DarkBrown, bottom, i / (float)(bands - 1)));
        Fill(new Rect(0f, 0f, area.width, 5f), Gold);
        Fill(new Rect(0f, area.height - 5f, area.width, 5f), SoftBrown);
    }

    void DrawPanel(Rect panel)
    {
        Fill(new Rect(panel.x + 5f, panel.y + 7f, panel.width, panel.height), new Color(0f, 0f, 0f, 0.50f));
        Fill(panel, new Color(0.17f, 0.10f, 0.06f));
        Fill(new Rect(panel.x, panel.y, panel.width, 5f), Gold);
        Fill(new Rect(panel.x + 24f, panel.y + 89f, panel.width - 48f, 2f), SoftBrown);
    }

    static void Fill(Rect rect, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    void DrawContent()
    {
        GUILayout.Label(campaign ? "Campagne" : "Mes quêtes", titleStyle, GUILayout.Height(65f));
        GUILayout.Space(10f);

        if (campaign) DrawCampaign();
        else DrawPersonalQuests();

        if (!string.IsNullOrEmpty(status))
        {
            GUILayout.Space(4f);
            GUILayout.Label(status, smallStyle, GUILayout.Height(28f));
        }
    }

    void DrawProgressBar(int completed, int total)
    {
        Rect bar = GUILayoutUtility.GetRect(10f, 14f, GUILayout.ExpandWidth(true));
        if (Event.current.type != EventType.Repaint) return;
        Fill(bar, SoftBrown);
        if (total > 0)
            Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(completed / (float)total), bar.height), Gold);
    }

    bool Button(string text, bool enabled, GUIStyle style = null)
    {
        bool oldEnabled = GUI.enabled;
        GUI.enabled = oldEnabled && enabled;
        bool clicked = GUILayout.Button(text, style ?? buttonStyle, GUILayout.Height(49f));
        GUI.enabled = oldEnabled;
        GUILayout.Space(7f);
        return clicked && enabled;
    }

    void DrawCampaign()
    {
        List<CampaignProgress.Chapter> chapters = CampaignProgress.GetChapters();
        int completed = CampaignProgress.CompletedCount;
        bool finished = CampaignProgress.IsFinished;

        GUILayout.Label("Progression de la campagne  •  " + completed + " / " + chapters.Count, infoStyle);
        GUILayout.Space(8f);
        DrawProgressBar(completed, chapters.Count);
        GUILayout.Space(13f);

        if (confirmReset)
        {
            GUILayout.Space(26f);
            GUILayout.Label("Recommencer la campagne ?", sectionStyle);
            GUILayout.Space(8f);
            GUILayout.Label("Ta progression sera remise à zéro. Les quêtes personnalisées ne seront pas supprimées.", infoStyle);
            GUILayout.FlexibleSpace();
            if (Button("Oui, recommencer depuis la quête 1", true, dangerStyle))
                Defer(() => { CampaignProgress.Reset(); QuestSelection.Clear(); confirmReset = false; Refresh(); });
            if (Button("Annuler", true)) Defer(() => confirmReset = false);
        }
        else if (chapters.Count == 0)
        {
            GUILayout.Label("Aucune quête officielle trouvée dans Resources/Quests.", infoStyle);
            GUILayout.FlexibleSpace();
        }
        else if (finished)
        {
            GUILayout.Label("Campagne terminée ! Choisis un niveau à rejouer :", infoStyle);
            GUILayout.Space(6f);
            float availableHeight = Mathf.Clamp((Screen.height / Mathf.Max(0.72f, Screen.height / 720f)) * 0.31f, 145f, 225f);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(availableHeight));
            for (int i = 0; i < chapters.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                ReplayItem(chapters, i);
                if (i + 1 < chapters.Count) ReplayItem(chapters, i + 1);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.Space(10f);
            if (selectedReplay >= 0 && selectedReplay < chapters.Count)
            {
                CampaignProgress.Chapter chapter = chapters[selectedReplay];
                GUILayout.Label("Quête " + chapter.number + "  —  " + chapter.title, smallStyle);
            }
            if (Button("Rejouer la quête sélectionnée", selectedReplay >= 0 && selectedReplay < chapters.Count))
                Defer(() => PlayReplay(chapters));
            GUILayout.FlexibleSpace();
        }
        else
        {
            CampaignProgress.Chapter next;
            if (CampaignProgress.TryGetNext(out next))
            {
                GUILayout.Space(38f);
                GUILayout.Label(completed == 0 ? "Le début de l'aventure" : "Prochain chapitre débloqué", infoStyle);
                GUILayout.Space(13f);
                GUILayout.Label("Quête " + next.number + "  —  " + next.title, sectionStyle);
                GUILayout.Space(22f);
                if (Button(completed == 0 ? "Commencer la campagne" : "Continuer la campagne", true))
                    Defer(PlayCampaign);
            }
            GUILayout.FlexibleSpace();
        }

        if (!confirmReset && completed > 0 && Button("Recommencer la campagne", true, dangerStyle))
            Defer(() => confirmReset = true);
        if (Button("Retour au menu principal", true)) Defer(Close);
    }

    void ReplayItem(List<CampaignProgress.Chapter> chapters, int index)
    {
        CampaignProgress.Chapter chapter = chapters[index];
        string title = string.IsNullOrEmpty(chapter.title) ? "Sans titre" : chapter.title;
        if (title.Length > 29) title = title.Substring(0, 26) + "...";
        string caption = chapter.number.ToString("00") + "  " + title;
        if (GUILayout.Button(caption, index == selectedReplay ? selectedStyle : listStyle,
            GUILayout.Height(45f), GUILayout.ExpandWidth(true)))
        {
            int chosen = index;
            Defer(() => selectedReplay = chosen);
        }
    }

    void DrawPersonalQuests()
    {
        GUILayout.Label("Les quêtes créées par les joueurs sont indépendantes de la campagne.", infoStyle);
        GUILayout.Space(14f);
        if (quests.Count == 0)
        {
            GUILayout.Label("Aucune quête personnelle enregistrée. Crée-en une dans l'éditeur de quêtes.", sectionStyle);
            GUILayout.FlexibleSpace();
        }
        else
        {
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(285f));
            for (int i = 0; i < quests.Count; i++)
            {
                QuestInfo quest = quests[i];
                string label = (quest.number > 0 ? "Quête " + quest.number + "  —  " : "") +
                    (string.IsNullOrEmpty(quest.title) ? quest.file : quest.title);
                if (GUILayout.Button(label, i == selected ? selectedStyle : listStyle, GUILayout.Height(43f)))
                {
                    int chosen = i;
                    Defer(() => selected = chosen);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
        }

        if (Button("Lancer la quête sélectionnée", selected >= 0 && selected < quests.Count))
            Defer(PlayPersonalQuest);
        if (Button("Retour au menu principal", true)) Defer(Close);
    }

    bool CanLoadScene()
    {
        if (!string.IsNullOrEmpty(gameScene) && Application.CanStreamedLevelBeLoaded(gameScene)) return true;
        status = "Scène « " + gameScene + " » absente des Build Settings.";
        Debug.LogWarning("QuestPickerMenu : " + status);
        return false;
    }

    void PlayCampaign()
    {
        if (!CanLoadScene()) return;
        if (!QuestSelection.TrySelectNextCampaign())
        {
            status = "Aucun chapitre disponible.";
            return;
        }
        SceneManager.LoadScene(gameScene);
    }

    void PlayReplay(List<CampaignProgress.Chapter> chapters)
    {
        if (!CampaignProgress.IsFinished || selectedReplay < 0 || selectedReplay >= chapters.Count || !CanLoadScene()) return;
        if (!QuestSelection.TrySelectReplayCampaign(chapters[selectedReplay].fileName))
        {
            status = "Impossible de sélectionner ce chapitre.";
            return;
        }
        SceneManager.LoadScene(gameScene);
    }

    void PlayPersonalQuest()
    {
        if (selected < 0 || selected >= quests.Count || !CanLoadScene()) return;
        QuestInfo quest = quests[selected];
        QuestSelection.Set(quest.file, quest.number, quest.title, false);
        SceneManager.LoadScene(gameScene);
    }

    Texture2D Texture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        textures.Add(texture);
        return texture;
    }

    void SetBackgrounds(GUIStyle style, Color normal, Color hover, Color active)
    {
        style.normal.background = Texture(normal);
        style.hover.background = Texture(hover);
        style.active.background = Texture(active);
        style.focused.background = style.normal.background;
        style.onNormal.background = style.normal.background;
        style.normal.textColor = Ivory;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.focused.textColor = Ivory;
        style.onNormal.textColor = Ivory;
    }

    void InitStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 43, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = Gold;
        sectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        sectionStyle.normal.textColor = Gold;
        infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        infoStyle.normal.textColor = Ivory;
        smallStyle = new GUIStyle(infoStyle) { fontSize = 16 };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter, fontSize = 21, fontStyle = FontStyle.Bold,
            padding = new RectOffset(12, 12, 4, 4), margin = new RectOffset(2, 2, 2, 2)
        };
        SetBackgrounds(buttonStyle, new Color(0.25f, 0.14f, 0.08f), new Color(0.42f, 0.24f, 0.10f), new Color(0.60f, 0.35f, 0.12f));

        listStyle = new GUIStyle(buttonStyle)
        {
            fontSize = 15, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(13, 6, 3, 3), margin = new RectOffset(2, 4, 2, 2)
        };
        SetBackgrounds(listStyle, new Color(0.20f, 0.12f, 0.07f), new Color(0.35f, 0.21f, 0.09f), new Color(0.55f, 0.32f, 0.10f));
        selectedStyle = new GUIStyle(listStyle) { fontStyle = FontStyle.Bold };
        SetBackgrounds(selectedStyle, new Color(0.55f, 0.32f, 0.10f), new Color(0.62f, 0.37f, 0.12f), new Color(0.62f, 0.37f, 0.12f));
        dangerStyle = new GUIStyle(buttonStyle) { fontSize = 18 };
        SetBackgrounds(dangerStyle, new Color(0.32f, 0.12f, 0.09f), new Color(0.48f, 0.19f, 0.11f), new Color(0.60f, 0.25f, 0.13f));
    }
}
