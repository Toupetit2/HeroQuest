using System;
using System.Collections.Generic;
using UnityEngine;

// interface de l'editeur de quetes, dessinee avec GUI / GUILayout uniquement (pas de UnityEditor)
// donc identique dans la fenetre d'editeur et dans le menu du jeu
// l'hote appelle Draw(zone) depuis son OnGUI
public class QuestEditorGUI
{
    const int CellSize = 24;

    static readonly string[] ToolNames =
        { "Porte", "P. secrete", "Bloquee", "Meuble", "Monstre", "Piege", "Escalier", "Note", "Gomme" };

    public QuestEditorModel model;
    public Action RequestRepaint;   // fourni par la fenetre d'editeur (en jeu OnGUI redessine en continu)
    public Action RequestClose;     // si renseigne (menu du jeu), affiche le bouton "Menu" et gere Echap
    public Action ReloadBoard;      // si renseigne, affiche le bouton "Recharger le plateau"

    // vrai tant qu'un message est affiche : la fenetre d'editeur doit continuer a se redessiner pour le faire disparaitre
    public bool NeedsRepaint { get { return notice != null; } }

    // ---------- etat de l'interface ----------
    Vector2Int hover = new Vector2Int(-1, -1);
    Vector2 scrollPos;
    List<Action> pending = new List<Action>();
    Dictionary<string, bool> folds = new Dictionary<string, bool>();
    Dictionary<string, string> intBuffers = new Dictionary<string, string>();
    int fieldCounter;
    string confirmText;
    Action confirmYes;
    string notice;
    float noticeUntil;

    GUIStyle centered, hint, bold, foldStyle, tag, tagWhite, zoneTag, errorBox, warningBox, okBox, bubbleStyle, tipStyle;

    // Theme sombre / parchemin assorti aux menus de campagne.
    static readonly Color DarkBrown = new Color(0.14f, 0.08f, 0.05f);
    static readonly Color Ivory = new Color(0.95f, 0.88f, 0.70f);
    static readonly Color Gold = new Color(1.00f, 0.82f, 0.30f);
    GUISkin themedSkin;
    readonly List<Texture2D> themeTextures = new List<Texture2D>();

    Texture2D ThemeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        themeTextures.Add(texture);
        return texture;
    }

    void SetThemeStyle(GUIStyle style, Color baseColor, Color hoverColor)
    {
        style.normal.background = ThemeTexture(baseColor);
        style.hover.background = ThemeTexture(hoverColor);
        style.active.background = style.hover.background;
        style.focused.background = style.normal.background;
        style.normal.textColor = Ivory;
        style.hover.textColor = Ivory;
        style.active.textColor = Ivory;
        style.focused.textColor = Ivory;
        style.alignment = TextAnchor.MiddleCenter;
    }

    void InitTheme()
    {
        if (themedSkin != null) return;
        themedSkin = UnityEngine.Object.Instantiate(GUI.skin);
        themedSkin.hideFlags = HideFlags.HideAndDontSave;
        themedSkin.label.normal.textColor = Ivory;
        themedSkin.label.fontSize = 13;
        themedSkin.button.fontSize = 13;
        SetThemeStyle(themedSkin.button, new Color(0.25f, 0.14f, 0.08f), new Color(0.42f, 0.24f, 0.10f));
        themedSkin.box.fontSize = 12;
        SetThemeStyle(themedSkin.box, new Color(0.20f, 0.12f, 0.07f), new Color(0.28f, 0.17f, 0.09f));
        themedSkin.textField.fontSize = 13;
        SetThemeStyle(themedSkin.textField, new Color(0.18f, 0.11f, 0.07f), new Color(0.27f, 0.16f, 0.09f));
        themedSkin.textArea.fontSize = 13;
        SetThemeStyle(themedSkin.textArea, new Color(0.18f, 0.11f, 0.07f), new Color(0.27f, 0.16f, 0.09f));
        themedSkin.textField.alignment = TextAnchor.MiddleLeft;
        themedSkin.textArea.alignment = TextAnchor.UpperLeft;
        themedSkin.toggle.normal.textColor = Ivory;
        themedSkin.toggle.hover.textColor = Ivory;
    }

    public QuestEditorGUI(QuestEditorModel model)
    {
        this.model = model;
    }

    // =====================================================================
    //  boucle principale
    // =====================================================================

    public void Draw(Rect area)
    {
        GUISkin previousSkin = GUI.skin;
        InitTheme();
        GUI.skin = themedSkin;
        try
        {
        InitStyles();
        fieldCounter = 0;
        Event e = Event.current;

        // les modifications de listes se font au debut d'un evenement Layout,
        // sinon l'IMGUI plante quand le nombre d'elements change en plein milieu du dessin
        if (e.type == EventType.Layout)
        {
            if (pending.Count > 0)
            {
                List<Action> todo = new List<Action>(pending);
                pending.Clear();
                foreach (Action a in todo) a();
            }
            model.Validate();
        }

        string msg = model.TakeMessage();
        if (msg != null)
        {
            notice = msg;
            noticeUntil = Time.realtimeSinceStartup + 3f;
        }

        if (notice != null && Time.realtimeSinceStartup >= noticeUntil) notice = null;

        if (e.type == EventType.Repaint) Fill(area, DarkBrown);

        bool locked = confirmYes != null;

        // retour au menu : bouton en haut a gauche ou touche Echap (demande confirmation si la quete a ete modifiee)
        if (RequestClose != null && !locked && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            Defer(() => AskDiscard(RequestClose));
            e.Use();
        }

        GUILayout.BeginArea(area);
        GUI.enabled = !locked;

        DrawToolbar();

        if (model.map == null)
        {
            GUILayout.Label(model.boardError, hint);
        }
        else
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(model.map.width * CellSize + 10));
            DrawToolPalette();
            DrawGrid();
            DrawValidation();
            GUILayout.EndVertical();

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true));
            DrawSettings();
            DrawLists();
            GUILayout.EndScrollView();

            GUILayout.EndHorizontal();
        }

        GUI.enabled = true;
        GUILayout.EndArea();

        if (locked) DrawConfirm(area);
        else DrawTooltip(area);
        }
        finally { GUI.skin = previousSkin; }
    }

    void Defer(Action action)
    {
        pending.Add(action);
        if (RequestRepaint != null) RequestRepaint();
    }

    void Ask(string text, Action yes)
    {
        confirmText = text;
        confirmYes = yes;
    }

    void AskDiscard(Action then)
    {
        if (!model.IsDirty()) then();
        else Ask("La quete en cours a ete modifiee. Continuer sans l'enregistrer ?", then);
    }

    void Save()
    {
        model.Validate();
        if (model.errors.Count > 0)
            Ask(model.errors.Count + " erreur(s) detectee(s) : le jeu risque de ne pas pouvoir charger cette quete.\nEnregistrer quand meme ?", SaveCheckOverwrite);
        else SaveCheckOverwrite();
    }

    void SaveCheckOverwrite()
    {
        if (model.NeedsOverwriteConfirm())
            Ask(model.FileNameFor(model.number) + " existe deja. Le remplacer ?", model.Save);
        else model.Save();
    }

    // =====================================================================
    //  styles et petits composants
    // =====================================================================

    void InitStyles()
    {
        if (centered != null) return;

        centered = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        hint = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 };
        bold = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        foldStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };

        tag = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 9, fontStyle = FontStyle.Bold };
        tag.normal.textColor = Color.black;
        tagWhite = new GUIStyle(tag);
        tagWhite.normal.textColor = Color.white;
        zoneTag = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = 8 };
        zoneTag.normal.textColor = new Color(0f, 0f, 0f, 0.55f);

        errorBox = new GUIStyle(GUI.skin.box) { wordWrap = true, alignment = TextAnchor.UpperLeft };
        errorBox.normal.textColor = new Color(1.00f, 0.60f, 0.52f);
        warningBox = new GUIStyle(errorBox);
        warningBox.normal.textColor = Gold;
        okBox = new GUIStyle(errorBox);
        okBox.normal.textColor = new Color(0.62f, 0.91f, 0.63f);

        // bulle d'aide : juste un "?" jaune, sans fond
        bubbleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 };
        bubbleStyle.padding = new RectOffset(0, 0, 0, 0);
        bubbleStyle.margin = new RectOffset(4, 4, 2, 2);
        bubbleStyle.normal.background = null;
        bubbleStyle.hover.background = null;
        bubbleStyle.normal.textColor = Gold;
        bubbleStyle.hover.textColor = new Color(1f, 0.95f, 0.65f);

        tipStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.UpperLeft, fontSize = 12 };
        tipStyle.padding = new RectOffset(8, 8, 6, 6);
        tipStyle.normal.textColor = Ivory;
    }


    static void Fill(Rect r, Color c)
    {
        Color previous = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    static void Label(string text, float width)
    {
        GUILayout.Label(text, GUILayout.Width(width));
    }

    // champ entier : le texte en cours de saisie est garde tant que le champ a le focus
    // (sinon impossible de taper "-" ou d'effacer le chiffre pour en ecrire un autre)
    int IntField(int value, float width)
    {
        string name = "quest_int_" + (fieldCounter++);
        GUI.SetNextControlName(name);
        bool focused = GUI.GetNameOfFocusedControl() == name;

        string shown = value.ToString();
        string buffered;
        if (focused && intBuffers.TryGetValue(name, out buffered)) shown = buffered;

        string edited = GUILayout.TextField(shown, GUILayout.Width(width));
        if (edited != shown)
        {
            intBuffers[name] = edited;
            int parsed;
            if (int.TryParse(edited, out parsed)) value = parsed;
        }
        else if (!focused) intBuffers.Remove(name);

        return value;
    }

    // ---------- stats de base des monstres ----------
    // lues dans les MonsterData (Resources/Monsters), les memes que celles utilisees par le jeu
    Dictionary<MonsterClass, MonsterData> monsterBase;

    MonsterData BaseStats(MonsterClass monsterClass)
    {
        if (monsterBase == null)
        {
            monsterBase = new Dictionary<MonsterClass, MonsterData>();
            foreach (MonsterData d in Resources.LoadAll<MonsterData>("Monsters"))
                if (d != null) monsterBase[d.monsterClass] = d;
        }
        MonsterData data;
        monsterBase.TryGetValue(monsterClass, out data);
        return data;
    }

    // champ de stat : affiche la stat de base tant qu'elle n'est pas modifiee
    // "stored" vaut 0 dans la quete quand la stat est normale (c'est ce que lit le QuestLoader) :
    // on n'enregistre donc une valeur que si elle est differente de la base, et 0 remet la base
    int StatField(int stored, int baseValue, float width)
    {
        int shown = stored > 0 ? stored : baseValue;
        int edited = IntField(shown, width);
        if (edited <= 0 || edited == baseValue) return 0;
        return edited;
    }

    // petite bulle "?" : l'explication s'affiche quand on passe la souris dessus (sans cliquer)
    void HelpBubble(string tip)
    {
        GUILayout.Label(new GUIContent("?", tip), bubbleStyle, GUILayout.Width(18), GUILayout.Height(20));
    }

    // titre de section suivi de sa bulle d'aide
    void TitleHelp(string title, string tip)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(title, bold, GUILayout.ExpandWidth(false));
        HelpBubble(tip);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    // liste repliable avec sa bulle d'aide a droite du titre
    bool FoldHelp(string key, string title, bool defaultOpen, string tip)
    {
        GUILayout.BeginHorizontal();
        bool open = Fold(key, title, defaultOpen);
        HelpBubble(tip);
        GUILayout.EndHorizontal();
        return open;
    }

    // dessine l'info-bulle de la bulle survolee, au-dessus de tout le reste
    void DrawTooltip(Rect area)
    {
        Event e = Event.current;
        if (e.type != EventType.Repaint || string.IsNullOrEmpty(GUI.tooltip)) return;

        GUIContent content = new GUIContent(GUI.tooltip);
        float width = Mathf.Min(360f, area.width - 16f);
        float height = tipStyle.CalcHeight(content, width);

        float x = Mathf.Clamp(e.mousePosition.x + 14f, area.x + 4f, area.xMax - width - 4f);
        float y = e.mousePosition.y + 20f;
        if (y + height > area.yMax - 4f) y = e.mousePosition.y - height - 8f;
        Rect box = new Rect(x, Mathf.Max(area.y + 4f, y), width, height);

        Fill(new Rect(box.x - 2f, box.y - 2f, box.width + 4f, box.height + 4f), Gold);
        Fill(box, new Color(0.17f, 0.10f, 0.06f));
        GUI.Label(box, content.text, tipStyle);
    }

    // width <= 0 : le champ prend la place disponible
    string StringField(string text, float width)
    {
        if (width > 0f) return GUILayout.TextField(text ?? "", GUILayout.Width(width));
        return GUILayout.TextField(text ?? "");
    }

    // choix dans un enum : "<  valeur  >" (pas de menu deroulant en IMGUI de jeu)
    // l'ecriture est differee pour ne pas changer le nombre de controles en plein dessin
    T EnumCycle<T>(T value, Action<T> set, float width) where T : struct
    {
        Array values = Enum.GetValues(typeof(T));
        int index = Mathf.Max(0, Array.IndexOf(values, value));

        GUILayout.BeginHorizontal(GUILayout.Width(width));
        if (GUILayout.Button("<", GUILayout.Width(20)))
        {
            T previous = (T)values.GetValue((index - 1 + values.Length) % values.Length);
            Defer(() => set(previous));
        }
        GUILayout.Label(value.ToString(), centered, GUILayout.ExpandWidth(true));
        if (GUILayout.Button(">", GUILayout.Width(20)))
        {
            T next = (T)values.GetValue((index + 1) % values.Length);
            Defer(() => set(next));
        }
        GUILayout.EndHorizontal();
        return value;
    }

    // meme principe pour un choix dans une liste de textes
    void ListCycle(List<string> items, string current, Action<string> set, float width)
    {
        int index = items.IndexOf(current);

        GUILayout.BeginHorizontal(GUILayout.Width(width));
        if (GUILayout.Button("<", GUILayout.Width(20)) && items.Count > 0)
        {
            string previous = items[(index - 1 + items.Count) % items.Count];
            Defer(() => set(previous));
        }
        GUILayout.Label(index >= 0 ? current : "(choisir)", centered, GUILayout.ExpandWidth(true));
        if (GUILayout.Button(">", GUILayout.Width(20)) && items.Count > 0)
        {
            string next = items[(index + 1) % items.Count];
            Defer(() => set(next));
        }
        GUILayout.EndHorizontal();
    }

    bool Fold(string key, string title, bool defaultOpen)
    {
        bool open;
        if (!folds.TryGetValue(key, out open))
        {
            open = defaultOpen;
            folds[key] = open;
        }
        if (GUILayout.Button((open ? "v " : "> ") + title, foldStyle))
        {
            bool newState = !open;
            Defer(() => folds[key] = newState);
        }
        return open;
    }

    // =====================================================================
    //  barre du haut, fenetre de confirmation
    // =====================================================================

    void DrawToolbar()
    {
        GUILayout.BeginHorizontal();

        HelpBubble("COMMENT FONCTIONNE L'EDITEUR\n\n"
            + "1. Choisissez un outil (Porte, Meuble, Monstre...) dans la barre au-dessus du plateau, puis cliquez sur une case. Le clic droit efface, quel que soit l'outil.\n"
            + "2. Reglez la quete dans le panneau de droite : numero, titre, escalier, depart, victoire, parchemin.\n"
            + "3. Les listes (Notes, Monstres, Meubles...) montrent tout ce qui est pose : on peut y corriger les valeurs ou supprimer une ligne avec x.\n"
            + "4. Le cadre sous le plateau signale les erreurs (rouge : la quete risque de ne pas se charger) et les avertissements (jaune).\n"
            + "5. Enregistrer ecrit quest_NN.json. Charger rouvre une quete. Nouvelle quete basee sur celle-ci copie la quete en cours sous le prochain numero.\n\n"
            + "Coordonnees : (0, 0) = case en haut a gauche ; x vers la droite, y vers le bas. Les boutons < > passent a la valeur precedente / suivante.");

        if (RequestClose != null)
        {
            if (GUILayout.Button("< Menu", GUILayout.Width(70)))
                Defer(() => AskDiscard(RequestClose));
            GUILayout.Space(10);
        }

        if (model.fileNames.Count > 0)
        {
            int index = Mathf.Clamp(model.selectedFile, 0, model.fileNames.Count - 1);
            if (GUILayout.Button("<", GUILayout.Width(22)))
            {
                int previous = (index - 1 + model.fileNames.Count) % model.fileNames.Count;
                Defer(() => model.selectedFile = previous);
            }
            GUILayout.Label(model.fileNames[index], centered, GUILayout.Width(130));
            if (GUILayout.Button(">", GUILayout.Width(22)))
            {
                int next = (index + 1) % model.fileNames.Count;
                Defer(() => model.selectedFile = next);
            }
            if (GUILayout.Button("Charger", GUILayout.Width(70)))
            {
                string name = model.fileNames[index];
                Defer(() => AskDiscard(() => model.LoadQuest(name)));
            }
        }
        else GUILayout.Label("Aucune quete enregistree");

        GUILayout.Space(10);
        if (GUILayout.Button("Nouvelle quete (n" + (char)176 + model.NextNumber() + ")"))
            Defer(() => AskDiscard(() => model.NewQuest(false)));
        if (GUILayout.Button("Nouvelle quete basee sur celle-ci"))
            Defer(() => model.NewQuest(true));

        GUILayout.FlexibleSpace();
        GUILayout.Label(notice ?? " ");

        if (ReloadBoard != null && GUILayout.Button("Recharger le plateau"))
            Defer(ReloadBoard);
        if (GUILayout.Button("Enregistrer", GUILayout.Width(90)))
            Defer(Save);

        GUILayout.EndHorizontal();
    }

    void DrawConfirm(Rect area)
    {
        Rect box = new Rect(area.x + area.width / 2f - 230f, area.y + area.height / 2f - 75f, 460f, 150f);
        if (Event.current.type == EventType.Repaint)
        {
            Fill(area, new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(box.x - 2f, box.y - 2f, box.width + 4f, box.height + 4f), Gold);
            Fill(box, new Color(0.17f, 0.10f, 0.06f));
        }

        GUILayout.BeginArea(new Rect(box.x + 10f, box.y + 10f, box.width - 20f, box.height - 20f));
        GUILayout.Label(confirmText, hint);
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Oui"))
        {
            Action yes = confirmYes;
            Defer(() => { confirmYes = null; yes(); });
        }
        if (GUILayout.Button("Annuler"))
            Defer(() => confirmYes = null);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    // =====================================================================
    //  palette d'outils
    // =====================================================================

    void DrawToolPalette()
    {
        int toolIndex = (int)model.tool;
        int picked = GUILayout.Toolbar(toolIndex, ToolNames);
        if (picked != toolIndex) Defer(() => model.tool = (QuestPaintTool)picked);

        switch (model.tool)
        {
            case QuestPaintTool.Porte:
            case QuestPaintTool.PorteSecrete:
                GUILayout.Label(model.tool == QuestPaintTool.PorteSecrete
                    ? "Cliquez pres du bord d'une case pour poser (ou retirer) une porte secrete : elle reste invisible tant que les heros ne l'ont pas trouvee en fouillant. Elle doit etre sur un mur entre deux zones."
                    : "Cliquez pres du bord d'une case pour poser (ou retirer) une porte sur ce bord. La porte doit etre sur un mur entre deux zones.", hint);
                break;

            case QuestPaintTool.CaseBloquee:
                GUILayout.Label("Clic = bloquer / debloquer la case. Une case bloquee est infranchissable (eboulis, rocher...).", hint);
                break;

            case QuestPaintTool.Meuble:
                GUILayout.BeginHorizontal();
                EnumCycle(model.furnitureType, v => { model.furnitureType = v; model.ApplyFurnitureDefault(); }, 190);
                Label("larg", 30);
                model.furnitureW = Mathf.Max(1, IntField(model.furnitureW, 28));
                Label("haut", 30);
                model.furnitureH = Mathf.Max(1, IntField(model.furnitureH, 28));
                if (GUILayout.Button("<", GUILayout.Width(24))) Defer(() => model.RotateFurnitureTool(-90));
                GUILayout.Label(model.furnitureRot + " deg", GUILayout.Width(50));
                if (GUILayout.Button(">", GUILayout.Width(24))) Defer(() => model.RotateFurnitureTool(90));
                GUILayout.EndHorizontal();
                GUILayout.Label("Clic = coin haut-gauche du meuble.", hint);
                break;

            case QuestPaintTool.Monstre:
                GUILayout.BeginHorizontal();
                EnumCycle(model.monsterType, v => model.monsterType = v, 190);
                Label("id", 16);
                model.monsterId = StringField(model.monsterId, 120);
                GUILayout.EndHorizontal();
                GUILayout.Label("L'id (optionnel) sert aux objectifs \"Tuer\". Un id ne sert qu'a un seul monstre.", hint);
                MonsterData baseData = BaseStats(model.monsterType);
                if (baseData != null)
                    GUILayout.Label("Stats de base du " + model.monsterType + " : deplacement " + baseData.movementCases
                        + ", attaque " + baseData.attackDices + ", defense " + baseData.defenseDices
                        + ", corps " + baseData.health + ", esprit " + baseData.spirit
                        + ". Modifiables dans la liste Monstres a droite (pour un boss, par exemple).", hint);
                break;

            case QuestPaintTool.Piege:
                GUILayout.BeginHorizontal();
                EnumCycle(model.trapType, v => model.trapType = v, 190);
                GUILayout.EndHorizontal();
                GUILayout.Label("Clic = pose un piege sur la case. Il reste cache jusqu'a ce qu'un heros le declenche.", hint);
                break;

            case QuestPaintTool.Escalier:
                GUILayout.Label("Clic = coin haut-gauche de l'escalier (taille reglable a droite).", hint);
                break;

            case QuestPaintTool.Note:
                GUILayout.BeginHorizontal();
                Label("lettre", 40);
                model.noteLetter = StringField(model.noteLetter, 30);
                GUILayout.Label("vide = prochaine lettre libre", hint, GUILayout.Width(200));
                GUILayout.EndHorizontal();
                GUILayout.Label("Clic = pose la lettre ; le texte s'ecrit dans la liste Notes a droite.", hint);
                break;

            case QuestPaintTool.Gomme:
                GUILayout.Label("Clic = efface (bord de case : porte, sinon monstre, piege, note, meuble, case bloquee). Le clic droit efface avec n'importe quel outil.", hint);
                break;
        }
    }

    // =====================================================================
    //  plateau
    // =====================================================================

    void DrawGrid()
    {
        float w = model.map.width * CellSize;
        float h = model.map.height * CellSize;
        Rect r = GUILayoutUtility.GetRect(w, h, GUILayout.Width(w), GUILayout.Height(h));
        Event e = Event.current;

        if (e.type != EventType.Layout && r.Contains(e.mousePosition))
        {
            hover = new Vector2Int(
                Mathf.Clamp((int)((e.mousePosition.x - r.x) / CellSize), 0, model.map.width - 1),
                Mathf.Clamp((int)((e.mousePosition.y - r.y) / CellSize), 0, model.map.height - 1));
        }

        if (e.type == EventType.MouseDown && GUI.enabled && (e.button == 0 || e.button == 1) && r.Contains(e.mousePosition))
        {
            Vector2 local = e.mousePosition - r.position;
            int cx = Mathf.Clamp((int)(local.x / CellSize), 0, model.map.width - 1);
            int cy = Mathf.Clamp((int)(local.y / CellSize), 0, model.map.height - 1);
            float fx = local.x / CellSize - cx;
            float fy = local.y / CellSize - cy;
            bool erase = e.button == 1 || model.tool == QuestPaintTool.Gomme;
            QuestPaintTool t = model.tool;
            Defer(() => model.ClickCell(cx, cy, fx, fy, erase, t));
            e.Use();
        }

        if (e.type == EventType.Repaint) PaintGrid(r);

        bool inside = model.map.InBounds(hover.x, hover.y);
        string zone = inside ? model.map.Get(hover.x, hover.y).zone : "";
        GUILayout.Label(inside
            ? "Case (" + hover.x + ", " + hover.y + ")   zone " + (zone == "." ? "couloir" : zone)
            : " ");
    }

    static Rect CellRect(Rect r, int x, int y)
    {
        return new Rect(r.x + x * CellSize, r.y + y * CellSize, CellSize, CellSize);
    }

    static Rect SpanRect(Rect r, int x, int y, int w, int h, float pad)
    {
        return new Rect(r.x + x * CellSize + pad, r.y + y * CellSize + pad, w * CellSize - 2f * pad, h * CellSize - 2f * pad);
    }

    static string Short(string text, int length)
    {
        if (string.IsNullOrEmpty(text)) return "?";
        return text.Length <= length ? text : text.Substring(0, length);
    }

    void PaintGrid(Rect r)
    {
        Map map = model.map;
        Fill(r, new Color(0.12f, 0.12f, 0.12f));

        // fond : une couleur par zone
        for (int y = 0; y < map.height; y++)
        {
            for (int x = 0; x < map.width; x++)
            {
                Rect c = CellRect(r, x, y);
                Fill(new Rect(c.x + 0.5f, c.y + 0.5f, c.width - 1f, c.height - 1f), model.ZoneColor(map.Get(x, y).zone));
            }
        }

        foreach (KeyValuePair<string, Vector2Int> kv in model.zoneFirst)
        {
            if (kv.Key != ".") GUI.Label(CellRect(r, kv.Value.x, kv.Value.y), kv.Key, zoneTag);
        }

        // murs = changement de zone
        for (int y = 0; y < map.height; y++)
        {
            for (int x = 0; x < map.width; x++)
            {
                Rect c = CellRect(r, x, y);
                if (x + 1 < map.width && map.HasWall(x, y, x + 1, y))
                    Fill(new Rect(c.xMax - 1.5f, c.y, 3f, c.height), Color.black);
                if (y + 1 < map.height && map.HasWall(x, y, x, y + 1))
                    Fill(new Rect(c.x, c.yMax - 1.5f, c.width, 3f), Color.black);
            }
        }

        // escalier
        Rect sr = SpanRect(r, model.stairs.x, model.stairs.y, model.stairs.width, model.stairs.height, 2f);
        Fill(sr, new Color(0.3f, 0.75f, 0.35f));
        GUI.Label(sr, "Esc", tag);

        // cases bloquees
        foreach (CellEntry b in model.blocked)
        {
            Rect br = SpanRect(r, b.x, b.y, 1, 1, 2f);
            Fill(br, new Color(0.25f, 0.25f, 0.25f));
            GUI.Label(br, "X", tagWhite);
        }

        // meubles
        foreach (FurnitureEntry f in model.furniture)
        {
            Rect fr = SpanRect(r, f.x, f.y, f.width, f.height, 2f);
            Fill(fr, new Color(0.7f, 0.5f, 0.3f));
            GUI.Label(fr, Short(f.type, 4) + (f.rotation != 0 ? "\n" + f.rotation : ""), tag);

            // barre sombre sur la "face avant" : sud a 0 deg, puis dans le sens des aiguilles d'une montre
            int rot = ((f.rotation % 360) + 360) % 360;
            Rect bar;
            if (rot == 90) bar = new Rect(fr.x, fr.y, 4f, fr.height);
            else if (rot == 180) bar = new Rect(fr.x, fr.y, fr.width, 4f);
            else if (rot == 270) bar = new Rect(fr.xMax - 4f, fr.y, 4f, fr.height);
            else bar = new Rect(fr.x, fr.yMax - 4f, fr.width, 4f);
            Fill(bar, new Color(0.35f, 0.2f, 0.05f));
        }

        // pieges
        foreach (TrapEntry t in model.traps)
        {
            Rect tr = SpanRect(r, t.x, t.y, 1, 1, 4f);
            Fill(tr, new Color(0.9f, 0.7f, 0.1f));
            GUI.Label(tr, Short(t.type, 1), tag);
        }

        // monstres (* = a un id, donc un boss ou une cible)
        foreach (MonsterEntry m in model.monsters)
        {
            Rect mr = SpanRect(r, m.x, m.y, 1, 1, 3f);
            bool named = !string.IsNullOrEmpty(m.id);
            Fill(mr, named ? new Color(0.55f, 0.15f, 0.55f) : new Color(0.8f, 0.25f, 0.25f));
            GUI.Label(mr, Short(m.type, 2) + (named ? "*" : ""), tagWhite);
        }

        // notes : pastille bleue avec la lettre, en haut a droite de la case
        foreach (NoteEntry note in model.notes)
        {
            if (string.IsNullOrEmpty(note.letter) || !map.InBounds(note.x, note.y)) continue;
            Rect cell = CellRect(r, note.x, note.y);
            Rect badge = new Rect(cell.xMax - 12f, cell.y + 1f, 11f, 11f);
            Fill(badge, new Color(0.1f, 0.25f, 0.6f));
            GUI.Label(badge, note.letter, tagWhite);
        }

        // portes : trait epais sur le bord de la case
        foreach (DoorEntry d in model.doors) PaintDoor(r, d, new Color(0.45f, 0.25f, 0.05f));
        foreach (DoorEntry d in model.secretDoors) PaintDoor(r, d, new Color(0.6f, 0.1f, 0.9f));
    }

    void PaintDoor(Rect r, DoorEntry d, Color color)
    {
        Rect c = CellRect(r, d.x, d.y);
        Side side = QuestEditorModel.ParseEnum(d.side, Side.North);
        Rect line;
        if (side == Side.North) line = new Rect(c.x + 2, c.y - 2, c.width - 4, 5);
        else if (side == Side.South) line = new Rect(c.x + 2, c.yMax - 3, c.width - 4, 5);
        else if (side == Side.West) line = new Rect(c.x - 2, c.y + 2, 5, c.height - 4);
        else line = new Rect(c.xMax - 3, c.y + 2, 5, c.height - 4);
        Fill(line, color);
    }

    // =====================================================================
    //  validation
    // =====================================================================

    // les resultats sont calcules une fois par evenement Layout (voir Draw) puis relus tels quels
    void DrawValidation()
    {
        List<string> errors = model.errors;
        List<string> warnings = model.warnings;

        if (errors.Count == 0 && warnings.Count == 0)
        {
            GUILayout.Box("Aucun probleme detecte.", okBox);
            return;
        }

        if (errors.Count > 0)
            GUILayout.Box(string.Join("\n", errors.GetRange(0, Mathf.Min(errors.Count, 8)).ToArray())
                + (errors.Count > 8 ? "\n... (" + (errors.Count - 8) + " autres)" : ""), errorBox);

        if (warnings.Count > 0)
            GUILayout.Box(string.Join("\n", warnings.GetRange(0, Mathf.Min(warnings.Count, 5)).ToArray())
                + (warnings.Count > 5 ? "\n... (" + (warnings.Count - 5) + " autres)" : ""), warningBox);
    }

    // =====================================================================
    //  panneau de droite : reglages
    // =====================================================================

    void DrawSettings()
    {
        TitleHelp("Quete", "Numero : donne le nom du fichier (quest_NN.json). Titre : nom de la quete. Les autres reglages sont expliques a cote de chaque section.");
        GUILayout.BeginHorizontal();
        Label("Numero", 110);
        model.number = IntField(model.number, 60);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        Label("Titre", 110);
        model.title = StringField(model.title, 0);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        Label("Fichier", 110);
        GUILayout.Label("quest_" + model.number.ToString("00") + ".json");
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        TitleHelp("Escalier", "Position (coin haut-gauche) et taille de l'escalier, en cases. On peut aussi le poser avec l'outil Escalier. Le bouton rot tourne l'image de l'escalier de 90 degres a chaque clic (sens des aiguilles d'une montre). C'est la sortie des objectifs Escalier, Escorter et Rapporter.");
        GUILayout.BeginHorizontal();
        Label("x", 12);
        model.stairs.x = IntField(model.stairs.x, 34);
        Label("y", 12);
        model.stairs.y = IntField(model.stairs.y, 34);
        Label("larg", 30);
        model.stairs.width = Mathf.Max(1, IntField(model.stairs.width, 34));
        Label("haut", 30);
        model.stairs.height = Mathf.Max(1, IntField(model.stairs.height, 34));
        int stairsRot = ((model.stairs.rotation % 360) + 360) % 360;
        if (GUILayout.Button("rot " + stairsRot, GUILayout.Width(60)))
            Defer(() => model.stairs.rotation = (stairsRot + 90) % 360);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        Label("Depart", 110);
        int startIndex = model.startType == "room" ? 1 : 0;
        int pickedStart = GUILayout.Toolbar(startIndex, new[] { "Escalier", "Une salle" }, GUILayout.Width(200));
        if (pickedStart != startIndex) Defer(() => model.startType = pickedStart == 1 ? "room" : "stairs");
        HelpBubble("Escalier = les heros commencent sur l'escalier. Une salle = ils commencent dans la salle choisie.");
        GUILayout.EndHorizontal();

        if (model.startType == "room")
        {
            GUILayout.BeginHorizontal();
            Label("Salle de depart", 110);
            ListCycle(model.zones.FindAll(z => z != "."), model.startRoom, v => model.startRoom = v, 200);
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        Label("Monstre errant", 110);
        EnumCycle(model.wanderingMonster, v => model.wanderingMonster = v, 200);
        HelpBubble("Type de monstre qui peut apparaitre pendant la partie.");
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        TitleHelp("Victoire", "Mode : Tous = tous les objectifs doivent etre remplis ; AuMoinsUn = un seul suffit. Ajoutez les objectifs avec + Objectif.\n\n"
            + "Tuer : cible = id du monstre (voir l'outil Monstre).\n"
            + "TuerTous : tous les monstres de la carte.\n"
            + "TrouverObjet : cible = objet, salle = zone (ex : S3).\n"
            + "Escorter : cible = id du personnage a amener a l'escalier.\n"
            + "Rapporter : cible = objet, nb = quantite a rapporter.\n"
            + "Escalier : revenir a l'escalier.");
        GUILayout.BeginHorizontal();
        Label("Mode", 110);
        EnumCycle(model.victoryMode, v => model.victoryMode = v, 200);
        GUILayout.EndHorizontal();

        for (int i = 0; i < model.objectives.Count; i++)
        {
            ObjectiveEntry o = model.objectives[i];
            ObjectiveType ot = QuestEditorModel.ParseEnum(o.type, ObjectiveType.Escalier);

            GUILayout.BeginHorizontal();
            EnumCycle(ot, v => o.type = v.ToString(), 170);

            if (ot == ObjectiveType.Tuer || ot == ObjectiveType.Escorter
                || ot == ObjectiveType.TrouverObjet || ot == ObjectiveType.Rapporter)
            {
                Label("cible", 32);
                o.target = StringField(o.target, 0);
            }
            if (ot == ObjectiveType.TrouverObjet)
            {
                Label("salle", 32);
                o.room = StringField(o.room, 40);
            }
            if (ot == ObjectiveType.Rapporter)
            {
                Label("nb", 18);
                o.count = IntField(o.count, 30);
            }

            int idx = i;
            if (GUILayout.Button("x", GUILayout.Width(22))) Defer(() => model.objectives.RemoveAt(idx));
            GUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Objectif", GUILayout.Width(100)))
            Defer(() => model.objectives.Add(new ObjectiveEntry { type = ObjectiveType.Tuer.ToString() }));

        GUILayout.Space(8);
        TitleHelp("Parchemin (lu a voix haute aux joueurs)", "Le texte d'introduction de la quete, affiche aux joueurs. La note secrete en dessous est reservee a Zargon.");
        model.parchment = GUILayout.TextArea(model.parchment ?? "", GUILayout.MinHeight(70));
        GUILayout.Label("Note secrete pour Zargon", bold);
        model.zargonNote = GUILayout.TextArea(model.zargonNote ?? "", GUILayout.MinHeight(40));
        GUILayout.Space(8);
    }

    // =====================================================================
    //  panneau de droite : listes
    // =====================================================================

    void DrawLists()
    {
        if (FoldHelp("notes", "Notes A, B, C... (" + model.notes.Count + ")", true, "Une lettre posee sur la carte renvoie a un texte reserve a Zargon. Une meme lettre peut etre posee plusieurs fois ; son texte n'est ecrit qu'une fois. x = y = -1 : la note n'est pas sur la carte."))
        {
            for (int i = 0; i < model.notes.Count; i++)
            {
                NoteEntry note = model.notes[i];
                GUILayout.BeginHorizontal();
                Label("lettre", 36);
                note.letter = StringField(note.letter, 30);
                Label("x", 10);
                note.x = IntField(note.x, 30);
                Label("y", 10);
                note.y = IntField(note.y, 30);
                GUILayout.Label("(-1 = pas sur la carte)", hint, GUILayout.Width(130));
                int idx = i;
                if (GUILayout.Button("x", GUILayout.Width(22))) Defer(() => model.notes.RemoveAt(idx));
                GUILayout.EndHorizontal();
                note.text = GUILayout.TextArea(note.text ?? "", GUILayout.MinHeight(38));
            }
            if (GUILayout.Button("+ Note", GUILayout.Width(100)))
                Defer(() => model.notes.Add(new NoteEntry { letter = model.NextNoteLetter(), text = "", x = -1, y = -1 }));
            GUILayout.Space(6);
        }

        if (FoldHelp("monsters", "Monstres (" + model.monsters.Count + ")", false, "Chaque ligne : type, case (x, y) et id optionnel. Dessous, les stats : dep = deplacement, att / def = des d'attaque / de defense, corps = points de corps, esp = esprit. Les valeurs affichees sont celles du monstre de base ; changez-en une pour un boss. Mettre 0 (ou remettre la valeur de base) revient a la stat normale."))
        {
            for (int i = 0; i < model.monsters.Count; i++)
            {
                MonsterEntry m = model.monsters[i];
                GUILayout.BeginHorizontal();
                EnumCycle(QuestEditorModel.ParseEnum(m.type, MonsterClass.Gobelin), v => m.type = v.ToString(), 170);
                Label("x", 10);
                m.x = IntField(m.x, 30);
                Label("y", 10);
                m.y = IntField(m.y, 30);
                Label("id", 14);
                m.id = StringField(m.id, 0);
                int idx = i;
                if (GUILayout.Button("x", GUILayout.Width(22))) Defer(() => model.monsters.RemoveAt(idx));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(16);
                MonsterData baseStats = BaseStats(QuestEditorModel.ParseEnum(m.type, MonsterClass.Gobelin));
                bool custom = m.movement > 0 || m.attack > 0 || m.defense > 0 || m.body > 0 || m.mind > 0;
                GUILayout.Label(custom ? "stats (modifiees)" : "stats (de base)", hint, GUILayout.Width(150));
                Label("dep", 24);
                m.movement = StatField(m.movement, baseStats != null ? baseStats.movementCases : 0, 26);
                Label("att", 22);
                m.attack = StatField(m.attack, baseStats != null ? baseStats.attackDices : 0, 26);
                Label("def", 22);
                m.defense = StatField(m.defense, baseStats != null ? baseStats.defenseDices : 0, 26);
                Label("corps", 34);
                m.body = StatField(m.body, baseStats != null ? baseStats.health : 0, 26);
                Label("esp", 24);
                m.mind = StatField(m.mind, baseStats != null ? baseStats.spirit : 0, 26);
                GUILayout.EndHorizontal();
            }
        }

        if (FoldHelp("furniture", "Meubles (" + model.furniture.Count + ")", false, "Type, case (x, y), largeur et hauteur de chaque meuble. Le bouton d'angle tourne le meuble de 90 degres (largeur et hauteur s'echangent)."))
        {
            for (int i = 0; i < model.furniture.Count; i++)
            {
                FurnitureEntry f = model.furniture[i];
                GUILayout.BeginHorizontal();
                EnumCycle(QuestEditorModel.ParseEnum(f.type, FurnitureType.Table), v => f.type = v.ToString(), 190);
                f.x = IntField(f.x, 28);
                f.y = IntField(f.y, 28);
                f.width = Mathf.Max(1, IntField(f.width, 28));
                f.height = Mathf.Max(1, IntField(f.height, 28));

                int rotIndex = Mathf.Max(0, Array.IndexOf(QuestEditorModel.RotationValues, ((f.rotation % 360) + 360) % 360));
                if (GUILayout.Button(QuestEditorModel.RotationValues[rotIndex] + " deg", GUILayout.Width(56)))
                {
                    // un quart de tour echange largeur et hauteur
                    Defer(() =>
                    {
                        int tmp = f.width;
                        f.width = f.height;
                        f.height = tmp;
                        f.rotation = (f.rotation + 90) % 360;
                    });
                }
                int idx = i;
                if (GUILayout.Button("x", GUILayout.Width(22))) Defer(() => model.furniture.RemoveAt(idx));
                GUILayout.EndHorizontal();
            }
            if (model.furniture.Count > 0)
                GUILayout.Label("colonnes : x, y, largeur, hauteur ; le bouton d'angle tourne de 90 deg (largeur et hauteur s'echangent)", hint);
        }

        if (FoldHelp("traps", "Pieges (" + model.traps.Count + ")", false, "Type et case (x, y) de chaque piege."))
        {
            for (int i = 0; i < model.traps.Count; i++)
            {
                TrapEntry t = model.traps[i];
                GUILayout.BeginHorizontal();
                EnumCycle(QuestEditorModel.ParseEnum(t.type, TrapType.Fosse), v => t.type = v.ToString(), 170);
                t.x = IntField(t.x, 30);
                t.y = IntField(t.y, 30);
                int idx = i;
                if (GUILayout.Button("x", GUILayout.Width(22))) Defer(() => model.traps.RemoveAt(idx));
                GUILayout.EndHorizontal();
            }
        }

        if (FoldHelp("doors", "Portes (" + model.doors.Count + ") et portes secretes (" + model.secretDoors.Count + ")", false, "Une porte est posee sur le bord d'une case : x, y de la case et cote (North, South, East, West). Il doit y avoir un mur a cet endroit."))
        {
            DrawDoorList(model.doors, "Portes");
            DrawDoorList(model.secretDoors, "Portes secretes");
        }

        if (FoldHelp("blocked", "Cases bloquees (" + model.blocked.Count + ")", false, "Cases infranchissables : x, y de chaque case."))
        {
            for (int i = 0; i < model.blocked.Count; i++)
            {
                CellEntry c = model.blocked[i];
                GUILayout.BeginHorizontal();
                c.x = IntField(c.x, 30);
                c.y = IntField(c.y, 30);
                int idx = i;
                if (GUILayout.Button("x", GUILayout.Width(22))) Defer(() => model.blocked.RemoveAt(idx));
                GUILayout.EndHorizontal();
            }
        }
    }

    void DrawDoorList(List<DoorEntry> list, string label)
    {
        GUILayout.Label(label, bold);
        for (int i = 0; i < list.Count; i++)
        {
            DoorEntry d = list[i];
            GUILayout.BeginHorizontal();
            d.x = IntField(d.x, 30);
            d.y = IntField(d.y, 30);
            EnumCycle(QuestEditorModel.ParseEnum(d.side, Side.North), v => d.side = v.ToString(), 110);
            int idx = i;
            List<DoorEntry> l = list;
            if (GUILayout.Button("x", GUILayout.Width(22))) Defer(() => l.RemoveAt(idx));
            GUILayout.EndHorizontal();
        }
    }
}
