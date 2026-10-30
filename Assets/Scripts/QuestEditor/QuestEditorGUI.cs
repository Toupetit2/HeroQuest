using System;
using System.Collections.Generic;
using UnityEngine;

// interface de l'editeur de quetes, dessinee avec GUI / GUILayout uniquement (pas de UnityEditor)
// donc identique dans la fenetre d'editeur et dans le menu du jeu
// l'hote appelle Draw(zone) depuis son OnGUI
// tous les textes affiches sont lus par les joueurs : francais, accents, aucun nom de code
public class QuestEditorGUI
{
    const int CellSize = 24;

    // meme ordre que QuestPaintTool
    static readonly string[] ToolNames =
        { "Sélection", "Porte", "P. secrète", "Fausse p.", "Bloquée", "Meuble", "Monstre", "Piège", "Trésor", "Allié", "Repère", "Escalier", "Héros", "Note", "Gomme" };

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

    GUIStyle centered, hint, bold, foldStyle, tag, tagWhite, zoneTag, errorBox, warningBox, okBox, bubbleStyle, tipStyle, toggleButton;

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
        themedSkin.toggle.onNormal.textColor = Gold;
        themedSkin.toggle.onHover.textColor = Gold;
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
            model.TrackChanges();
            model.Validate();
        }

        string msg = model.TakeMessage();
        if (msg != null)
        {
            notice = msg;
            noticeUntil = Time.realtimeSinceStartup + 4f;
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

        // Ctrl+Z (Cmd+Z sur Mac) : annuler la derniere modification
        if (!locked && e.type == EventType.KeyDown && e.keyCode == KeyCode.Z && (e.control || e.command))
        {
            Defer(UndoLast);
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
            DrawSelection();
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

    // annule la derniere modification ; le champ de texte actif perd le focus pour afficher la valeur restauree
    void UndoLast()
    {
        GUIUtility.keyboardControl = 0;
        model.Undo();
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
        else Ask("La quête en cours a été modifiée. Continuer sans l'enregistrer ?", then);
    }

    void Save()
    {
        model.Validate();
        if (model.errors.Count > 0)
            Ask(model.errors.Count + " problème(s) à corriger : la quête risque de ne pas pouvoir être jouée.\nEnregistrer quand même ?", SaveCheckOverwrite);
        else SaveCheckOverwrite();
    }

    void SaveCheckOverwrite()
    {
        if (model.NeedsOverwriteConfirm())
            Ask("Une quête perso n°" + model.number + " existe déjà. La remplacer ?", model.Save);
        else model.Save();
    }

    // nom de fichier affiche au joueur : "Livret 03 - titre" ou "Perso 01 - titre"
    static string FileLabel(string fileName)
    {
        string name = fileName.Replace(".json", "");
        if (name.StartsWith(QuestStorage.OfficialPrefix)) return "Livret " + name.Substring(QuestStorage.OfficialPrefix.Length);
        if (name.StartsWith(QuestStorage.CustomPrefix)) return "Perso " + name.Substring(QuestStorage.CustomPrefix.Length);
        return name;
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

        // bouton qui reste enfonce (zones de roche, regles)
        toggleButton = new GUIStyle(GUI.skin.button) { fontSize = 11 };
        toggleButton.onNormal.background = ThemeTexture(new Color(0.55f, 0.38f, 0.10f));
        toggleButton.onHover.background = toggleButton.onNormal.background;
        toggleButton.onNormal.textColor = Color.white;
        toggleButton.onHover.textColor = Color.white;
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

    // case a cocher ; le changement est differe car il peut afficher ou cacher d'autres champs
    void ToggleField(bool value, string label, Action<bool> set, float width = 0f)
    {
        bool edited = width > 0f ? GUILayout.Toggle(value, label, GUILayout.Width(width)) : GUILayout.Toggle(value, label);
        if (edited != value) Defer(() => set(edited));
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
        float width = Mathf.Min(380f, area.width - 16f);
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
    // name = texte affiche au joueur ; l'ecriture est differee pour ne pas changer le nombre de controles en plein dessin
    void EnumCycle<T>(T value, Action<T> set, float width, Func<T, string> name) where T : struct
    {
        Array values = Enum.GetValues(typeof(T));
        int index = Mathf.Max(0, Array.IndexOf(values, value));

        GUILayout.BeginHorizontal(GUILayout.Width(width));
        if (GUILayout.Button("<", GUILayout.Width(20)))
        {
            T previous = (T)values.GetValue((index - 1 + values.Length) % values.Length);
            Defer(() => set(previous));
        }
        GUILayout.Label(name(value), centered, GUILayout.ExpandWidth(true));
        if (GUILayout.Button(">", GUILayout.Width(20)))
        {
            T next = (T)values.GetValue((index + 1) % values.Length);
            Defer(() => set(next));
        }
        GUILayout.EndHorizontal();
    }

    // meme principe pour un choix dans une liste de textes ; name = texte affiche (null = le texte tel quel)
    void ListCycle(List<string> items, string current, Action<string> set, float width, Func<string, string> name = null)
    {
        int index = items.IndexOf(current);

        GUILayout.BeginHorizontal(GUILayout.Width(width));
        if (GUILayout.Button("<", GUILayout.Width(20)) && items.Count > 0)
        {
            string previous = items[(index - 1 + items.Count) % items.Count];
            Defer(() => set(previous));
        }
        string shown = index < 0 ? (items.Count == 0 ? "(aucun choix)" : "(choisir)") : (name != null ? name(current) : current);
        GUILayout.Label(shown, centered, GUILayout.ExpandWidth(true));
        if (GUILayout.Button(">", GUILayout.Width(20)) && items.Count > 0)
        {
            string next = items[(index + 1) % items.Count];
            Defer(() => set(next));
        }
        GUILayout.EndHorizontal();
    }

    static string RoomName(string zone)
    {
        if (zone == ".") return "Couloirs";
        return zone.StartsWith("S") ? "Salle " + zone.Substring(1) : zone;
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

    // case de la grille : "col" et "lig" puis les deux champs
    void CellFields(ref int x, ref int y)
    {
        Label("col.", 26);
        x = IntField(x, 30);
        Label("lig.", 24);
        y = IntField(y, 30);
    }

    // =====================================================================
    //  barre du haut, fenetre de confirmation
    // =====================================================================

    void DrawToolbar()
    {
        GUILayout.BeginHorizontal();

        HelpBubble("COMMENT CRÉER UNE QUÊTE\n\n"
            + "1. Choisissez un outil (Porte, Meuble, Monstre…) dans la barre au-dessus du plateau, puis cliquez sur une case. Le clic droit efface, quel que soit l'outil.\n"
            + "2. Réglez la quête dans le panneau de droite : titre, escalier, départ, victoire, parchemin, règles, récompense.\n"
            + "3. Les listes (Monstres, Trésors, Meubles…) montrent tout ce qui est posé : on peut y corriger chaque élément ou le supprimer avec ×.\n"
            + "4. Sous le plateau, les problèmes en rouge empêchent de jouer la quête ; les remarques en jaune sont des conseils.\n"
            + "5. « Enregistrer » range la quête dans Mes quêtes. « Ouvrir » recharge une quête, y compris celles du livret pour s'en inspirer : elles ne sont jamais modifiées, vos changements deviennent une nouvelle quête.\n\n"
            + "Les cases sont repérées par leur colonne (de gauche à droite, à partir de 0) et leur ligne (de haut en bas, à partir de 0). Passez la souris sur le plateau pour les voir. Les boutons < > font défiler les choix.");

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
            GUILayout.Label(FileLabel(model.fileNames[index]), centered, GUILayout.Width(100));
            if (GUILayout.Button(">", GUILayout.Width(22)))
            {
                int next = (index + 1) % model.fileNames.Count;
                Defer(() => model.selectedFile = next);
            }
            if (GUILayout.Button("Ouvrir", GUILayout.Width(70)))
            {
                string name = model.fileNames[index];
                Defer(() => AskDiscard(() => model.LoadQuest(name)));
            }
        }
        else GUILayout.Label("Aucune quête enregistrée");

        GUILayout.Space(10);
        bool wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && model.CanUndo;
        if (GUILayout.Button(new GUIContent("Annuler", "Revenir en arrière : annule la dernière modification (raccourci Ctrl+Z)."), GUILayout.Width(80)))
            Defer(UndoLast);
        GUI.enabled = wasEnabled;

        GUILayout.Space(10);
        if (GUILayout.Button("Nouvelle quête"))
            Defer(() => AskDiscard(() => model.NewQuest(false)));
        if (GUILayout.Button("Copier cette quête"))
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
        // deux lignes d'outils : une seule ligne serait plus large que le plateau et ecraserait le panneau de droite
        int toolIndex = (int)model.tool;
        int picked = GUILayout.SelectionGrid(toolIndex, ToolNames, 8, GUILayout.Width(model.map.width * CellSize));
        if (picked != toolIndex) Defer(() => model.tool = (QuestPaintTool)picked);

        switch (model.tool)
        {
            case QuestPaintTool.Selection:
                GUILayout.Label("Cliquez sur un élément du plateau pour le choisir : ses réglages s'affichent en haut à droite. Près d'un bord de case, c'est la porte qui est choisie.", hint);
                break;

            case QuestPaintTool.Porte:
                GUILayout.Label("Cliquez près du bord d'une case pour poser (ou retirer) une porte. Une porte se place toujours sur un mur, entre deux pièces.", hint);
                break;

            case QuestPaintTool.PorteSecrete:
                GUILayout.Label("Cliquez près du bord d'une case pour poser (ou retirer) une porte secrète. Les héros ne la voient qu'après l'avoir trouvée en cherchant les portes secrètes.", hint);
                break;

            case QuestPaintTool.FaussePorte:
                GUILayout.Label("Cliquez près du bord d'une case pour poser (ou retirer) une fausse porte : les héros la voient, mais elle ne s'ouvre jamais.", hint);
                break;

            case QuestPaintTool.CaseBloquee:
                GUILayout.Label("Cliquez sur une case pour la bloquer ou la débloquer. Personne ne peut traverser une case bloquée (éboulis, rocher…).", hint);
                break;

            case QuestPaintTool.Meuble:
                GUILayout.BeginHorizontal();
                EnumCycle(model.furnitureType, v => { model.furnitureType = v; model.ApplyFurnitureDefault(); }, 190, QuestEditorNames.Of);
                Label("larg.", 34);
                model.furnitureW = Mathf.Max(1, IntField(model.furnitureW, 28));
                Label("haut.", 34);
                model.furnitureH = Mathf.Max(1, IntField(model.furnitureH, 28));
                if (GUILayout.Button("<", GUILayout.Width(24))) Defer(() => model.RotateFurnitureTool(-90));
                GUILayout.Label(model.furnitureRot + "°", GUILayout.Width(36));
                if (GUILayout.Button(">", GUILayout.Width(24))) Defer(() => model.RotateFurnitureTool(90));
                GUILayout.EndHorizontal();
                GUILayout.Label("Cliquez sur la case du coin en haut à gauche du meuble. Les flèches le tournent d'un quart de tour.", hint);
                break;

            case QuestPaintTool.Monstre:
                GUILayout.BeginHorizontal();
                EnumCycle(model.monsterType, v => model.monsterType = v, 190, QuestEditorNames.Of);
                Label("nom", 32);
                model.monsterId = StringField(model.monsterId, 120);
                GUILayout.EndHorizontal();
                GUILayout.Label("Le nom est facultatif : donnez-en un aux monstres importants (un chef à vaincre, un monstre qui garde un trésor). Chaque nom ne sert qu'une fois.", hint);
                MonsterData baseData = BaseStats(model.monsterType);
                if (baseData != null)
                    GUILayout.Label(QuestEditorNames.Of(model.monsterType) + " : déplacement " + baseData.movementCases
                        + ", attaque " + baseData.attackDices + ", défense " + baseData.defenseDices
                        + ", corps " + baseData.health + ", esprit " + baseData.spirit
                        + ". Pour un chef plus fort, changez ses valeurs dans la liste Monstres à droite.", hint);
                break;

            case QuestPaintTool.Piege:
                GUILayout.BeginHorizontal();
                EnumCycle(model.trapType, v => model.trapType = v, 190, QuestEditorNames.Of);
                GUILayout.EndHorizontal();
                GUILayout.Label("Cliquez sur une case pour y cacher un piège. Il reste invisible jusqu'à ce qu'un héros le trouve ou tombe dedans.", hint);
                break;

            case QuestPaintTool.Tresor:
                GUILayout.Label("Cliquez sur une case d'une salle pour y cacher un trésor spécial (or, objet, coffre piégé…). Il est trouvé par le premier héros qui fouille la salle. Réglez son contenu dans la liste Trésors à droite.", hint);
                break;

            case QuestPaintTool.Allie:
                GUILayout.BeginHorizontal();
                Label("nom", 32);
                model.allyId = StringField(model.allyId, 120);
                GUILayout.EndHorizontal();
                GUILayout.Label("Cliquez sur une case pour y placer un allié à secourir ou à escorter (comme un prisonnier). Ses caractéristiques se règlent dans la liste Alliés.", hint);
                break;

            case QuestPaintTool.Repere:
                GUILayout.BeginHorizontal();
                Label("étiquette", 64);
                model.markerLabel = StringField(model.markerLabel, 50);
                GUILayout.EndHorizontal();
                GUILayout.Label("Cliquez sur une case pour y poser un repère (X, XX, un numéro…) : une case dont parlent les règles de la quête, comme le point d'arrivée d'une téléportation.", hint);
                break;

            case QuestPaintTool.Escalier:
                GUILayout.Label("Cliquez sur la case du coin en haut à gauche de l'escalier (sa taille se règle à droite).", hint);
                break;

            case QuestPaintTool.DepartHeros:
                GUILayout.BeginHorizontal();
                int slotIndex = Mathf.Clamp(model.heroSlot, 1, HeroStartLayout.Count) - 1;
                int pickedSlot = GUILayout.Toolbar(slotIndex, new[] { "H1", "H2", "H3", "H4" }, GUILayout.Width(200));
                if (pickedSlot != slotIndex) Defer(() => model.heroSlot = pickedSlot + 1);
                if (GUILayout.Button("Positions par défaut", GUILayout.Width(150))) Defer(() => model.heroStarts = model.DefaultHeroStarts());
                GUILayout.EndHorizontal();
                GUILayout.Label("Choisissez un héros (H1 joue en premier), puis cliquez sur sa case de départ : l'outil passe ensuite au héros suivant. Par défaut, les héros sont sur l'escalier et tournent avec lui (H1 en bas à droite, H2 en bas à gauche, H3 en haut à droite, H4 en haut à gauche).", hint);
                break;

            case QuestPaintTool.Note:
                GUILayout.BeginHorizontal();
                Label("lettre", 40);
                model.noteLetter = StringField(model.noteLetter, 30);
                GUILayout.Label("vide = prochaine lettre libre", hint, GUILayout.Width(200));
                GUILayout.EndHorizontal();
                GUILayout.Label("Cliquez sur une case pour y poser une lettre (A, B…). Son texte, réservé à Zargon, s'écrit dans la liste Notes.", hint);
                break;

            case QuestPaintTool.Gomme:
                GUILayout.Label("Cliquez pour effacer : près d'un bord, la porte ; sinon ce qui est sur la case. Le clic droit efface aussi, avec n'importe quel outil.", hint);
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
            if (t == QuestPaintTool.Selection && !erase) Defer(() => SelectAt(cx, cy, fx, fy));
            else Defer(() => model.ClickCell(cx, cy, fx, fy, erase, t));
            e.Use();
        }

        if (e.type == EventType.Repaint) PaintGrid(r);

        bool inside = model.map.InBounds(hover.x, hover.y);
        string zone = inside ? model.map.Get(hover.x, hover.y).zone : "";
        GUILayout.BeginHorizontal();
        GUILayout.Label(inside
            ? "Colonne " + hover.x + ", ligne " + hover.y + "   –   " + (zone == "." ? "couloir" : RoomName(zone).ToLower())
            : " ");
        GUILayout.FlexibleSpace();
        GUILayout.Label("Clic gauche : poser     Clic droit : effacer     Ctrl+Z : annuler", hint);
        GUILayout.EndHorizontal();
    }

    // image de l'escalier du jeu (la meme que HQStairsVisual), null si absente
    Texture2D stairsTexture;
    bool stairsTextureLoaded;

    Texture2D StairsTexture()
    {
        if (!stairsTextureLoaded)
        {
            stairsTextureLoaded = true;
            stairsTexture = Resources.Load<Texture2D>(HQStairsVisual.TexturePath);
        }
        return stairsTexture;
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

        // fond : une couleur par zone ; seules les cases que les heros ne peuvent pas atteindre sont assombries
        bool[,] reachable = model.ReachableCells();
        for (int y = 0; y < map.height; y++)
        {
            for (int x = 0; x < map.width; x++)
            {
                Rect c = CellRect(r, x, y);
                string z = map.Get(x, y).zone;
                Color color = model.ZoneColor(z);
                if (!reachable[x, y]) color = Color.Lerp(color, new Color(0.3f, 0.2f, 0.2f), 0.7f);
                Fill(new Rect(c.x + 0.5f, c.y + 0.5f, c.width - 1f, c.height - 1f), color);
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
        // l'image est tournee comme dans le jeu : vue de dessus, sens des aiguilles d'une montre
        Rect sr = SpanRect(r, model.stairs.x, model.stairs.y, model.stairs.width, model.stairs.height, 2f);
        Fill(sr, new Color(0.3f, 0.75f, 0.35f));
        int stairsRot = ((model.stairs.rotation % 360) + 360) % 360;
        Texture2D stairsImage = StairsTexture();
        if (stairsImage != null)
        {
            Matrix4x4 previous = GUI.matrix;
            GUIUtility.RotateAroundPivot(stairsRot, sr.center);
            bool turned = stairsRot == 90 || stairsRot == 270;
            Rect img = turned ? new Rect(sr.center.x - sr.height / 2f, sr.center.y - sr.width / 2f, sr.height, sr.width) : sr;
            GUI.DrawTexture(img, stairsImage, ScaleMode.StretchToFill);
            GUI.matrix = previous;
        }
        else GUI.Label(sr, "Esc\n" + stairsRot + "°", tag);

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
            string name = QuestEditorNames.Of(QuestEditorModel.ParseEnum(f.type, FurnitureType.Table));
            GUI.Label(fr, Short(name, 4) + (f.rotation != 0 ? "\n" + f.rotation + "°" : ""), tag);

            // barre sombre sur la "face avant" : sud a 0 deg, puis dans le sens des aiguilles d'une montre
            int rot = ((f.rotation % 360) + 360) % 360;
            Rect bar;
            if (rot == 90) bar = new Rect(fr.x, fr.y, 4f, fr.height);
            else if (rot == 180) bar = new Rect(fr.x, fr.y, fr.width, 4f);
            else if (rot == 270) bar = new Rect(fr.xMax - 4f, fr.y, 4f, fr.height);
            else bar = new Rect(fr.x, fr.yMax - 4f, fr.width, 4f);
            Fill(bar, new Color(0.35f, 0.2f, 0.05f));
        }

        // tresors : pastille doree "T" en bas a gauche (souvent sur un coffre)
        foreach (TreasureEntry tr in model.treasures)
        {
            if (!map.InBounds(tr.x, tr.y)) continue;
            Rect cell = CellRect(r, tr.x, tr.y);
            Rect badge = new Rect(cell.x + 1f, cell.yMax - 12f, 11f, 11f);
            Fill(badge, tr.trapped ? new Color(0.85f, 0.35f, 0.1f) : Gold);
            GUI.Label(badge, "T", tag);
        }

        // pieges
        foreach (TrapEntry t in model.traps)
        {
            Rect tr = SpanRect(r, t.x, t.y, 1, 1, 4f);
            Fill(tr, new Color(0.9f, 0.7f, 0.1f));
            GUI.Label(tr, Short(QuestEditorNames.Of(QuestEditorModel.ParseEnum(t.type, TrapType.Fosse)), 1), tag);
        }

        // monstres (* = a un nom, donc un chef ou une cible)
        foreach (MonsterEntry m in model.monsters)
        {
            Rect mr = SpanRect(r, m.x, m.y, 1, 1, 3f);
            bool named = !string.IsNullOrEmpty(m.id);
            Color color = named ? new Color(0.55f, 0.15f, 0.55f) : new Color(0.8f, 0.25f, 0.25f);
            if (m.hidden || m.statue) color = Color.Lerp(color, Color.gray, 0.5f);
            Fill(mr, color);
            GUI.Label(mr, Short(m.type, 2) + (named ? "*" : ""), tagWhite);
        }

        // allies
        foreach (AllyEntry a in model.allies)
        {
            Rect ar = SpanRect(r, a.x, a.y, 1, 1, 3f);
            Fill(ar, new Color(0.2f, 0.45f, 0.85f));
            GUI.Label(ar, "Al", tagWhite);
        }

        // reperes : etiquette noire en bas a droite
        foreach (MarkerEntry mk in model.markers)
        {
            if (!map.InBounds(mk.x, mk.y)) continue;
            Rect cell = CellRect(r, mk.x, mk.y);
            Rect badge = new Rect(cell.xMax - 14f, cell.yMax - 12f, 13f, 11f);
            Fill(badge, Color.black);
            GUI.Label(badge, Short(mk.label, 2), tagWhite);
        }

        // depart des heros : pastille turquoise "H1" a "H4", au centre de la case
        foreach (HeroStartEntry h in model.heroStarts)
        {
            if (!map.InBounds(h.x, h.y)) continue;
            Rect hr = SpanRect(r, h.x, h.y, 1, 1, 4f);
            Fill(hr, new Color(0.1f, 0.6f, 0.6f));
            GUI.Label(hr, "H" + h.slot, tagWhite);
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
        foreach (DoorEntry d in model.falseDoors) PaintDoor(r, d, new Color(0.55f, 0.55f, 0.55f));

        PaintSelection(r);
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
            GUILayout.Box("Aucun problème : la quête est jouable.", okBox);
            return;
        }

        if (errors.Count > 0)
            GUILayout.Box(string.Join("\n", errors.GetRange(0, Mathf.Min(errors.Count, 8)).ToArray())
                + (errors.Count > 8 ? "\n… et " + (errors.Count - 8) + " autre(s)" : ""), errorBox);

        if (warnings.Count > 0)
            GUILayout.Box(string.Join("\n", warnings.GetRange(0, Mathf.Min(warnings.Count, 5)).ToArray())
                + (warnings.Count > 5 ? "\n… et " + (warnings.Count - 5) + " autre(s)" : ""), warningBox);
    }

    // =====================================================================
    //  panneau de droite : reglages
    // =====================================================================

    void DrawSettings()
    {
        TitleHelp("Quête", "Le numéro range la quête dans Mes quêtes (Perso 01, Perso 02…). Le titre est le nom affiché aux joueurs.");
        GUILayout.BeginHorizontal();
        Label("Numéro", 110);
        model.number = IntField(model.number, 60);
        GUILayout.Label("→ " + FileLabel(model.FileNameFor(model.number)), hint);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        Label("Titre", 110);
        model.title = StringField(model.title, 0);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        TitleHelp("Escalier", "L'escalier est l'entrée et la sortie du donjon. Indiquez la case de son coin en haut à gauche (ou posez-le avec l'outil Escalier) et sa taille en cases. Le bouton « tourner » fait pivoter son dessin d'un quart de tour. Si les héros partent de l'escalier, leurs cases de départ le suivent quand on le déplace.");
        StairsFields();

        GUILayout.BeginHorizontal();
        Label("Départ", 110);
        int startIndex = model.startType == "room" ? 1 : 0;
        int pickedStart = GUILayout.Toolbar(startIndex, new[] { "Sur l'escalier", "Dans une salle" }, GUILayout.Width(220));
        if (pickedStart != startIndex)
        {
            Defer(() =>
            {
                model.startType = pickedStart == 1 ? "room" : "stairs";
                if (model.startType == "stairs") model.heroStarts = model.DefaultHeroStarts();
            });
        }
        HelpBubble("Où les héros commencent la partie : sur l'escalier, ou dans une salle (par exemple une cellule de prison). Pour une salle, placez ensuite les 4 héros avec l'outil Départ.");
        GUILayout.EndHorizontal();

        if (model.startType == "room")
        {
            GUILayout.BeginHorizontal();
            Label("Salle de départ", 110);
            ListCycle(model.Rooms(), model.startRoom, v => model.startRoom = v, 220, RoomName);
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        Label("Monstre errant", 110);
        if (!model.noWanderingMonster)
            EnumCycle(model.wanderingMonster, v => model.wanderingMonster = v, 200, QuestEditorNames.Of);
        ToggleField(model.noWanderingMonster, "aucun", v => model.noWanderingMonster = v, 70);
        HelpBubble("Le monstre qui surgit quand un héros pioche la carte « monstre errant » en fouillant une salle. Cochez « aucun » si cette quête n'en a pas.");
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        TitleHelp("Victoire", "Ce que les héros doivent accomplir pour gagner.\n\n"
            + "« Tous les objectifs » : il faut tous les remplir. « Un seul objectif suffit » : le premier rempli fait gagner.\n\n"
            + "Vaincre un monstre : choisissez avec < > l'un des monstres posés sur le plateau.\n"
            + "Vaincre tous les monstres : tous les monstres de la carte.\n"
            + "Trouver un objet : l'objet et la salle où il est caché. Le bouton « trésor » reprend l'objet d'un trésor déjà posé.\n"
            + "Escorter un allié : l'allié doit arriver vivant à l'escalier.\n"
            + "Rapporter des objets : l'objet et combien en rapporter à l'escalier.\n"
            + "Revenir à l'escalier : les héros doivent ressortir du donjon.");
        GUILayout.BeginHorizontal();
        Label("Pour gagner", 110);
        EnumCycle(model.victoryMode, v => model.victoryMode = v, 220, QuestEditorNames.Of);
        GUILayout.EndHorizontal();

        List<string> allyIds = model.AllyIds();
        for (int i = 0; i < model.objectives.Count; i++)
        {
            ObjectiveEntry o = model.objectives[i];
            ObjectiveType ot = QuestEditorModel.ParseEnum(o.type, ObjectiveType.Escalier);

            GUILayout.BeginHorizontal();
            EnumCycle(ot, v => o.type = v.ToString(), 210, QuestEditorNames.Of);

            if (ot == ObjectiveType.Tuer)
                MonsterPicker(o);
            if (ot == ObjectiveType.TrouverObjet)
                TreasurePicker(o);
            if (ot == ObjectiveType.Escorter)
                ListCycle(allyIds, o.target, v => o.target = v, 160);
            if (ot == ObjectiveType.TrouverObjet || ot == ObjectiveType.Rapporter)
            {
                Label("objet", 36);
                o.target = StringField(o.target, 0);
            }
            if (ot == ObjectiveType.TrouverObjet)
                ListCycle(model.Rooms(), o.room, v => o.room = v, 120, RoomName);
            if (ot == ObjectiveType.Rapporter)
            {
                Label("nombre", 50);
                o.count = IntField(o.count, 30);
            }

            int idx = i;
            if (GUILayout.Button("×", GUILayout.Width(22))) Defer(() => model.objectives.RemoveAt(idx));
            GUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Objectif", GUILayout.Width(100)))
            Defer(() => model.objectives.Add(new ObjectiveEntry { type = ObjectiveType.Tuer.ToString() }));

        GUILayout.Space(8);
        TitleHelp("Parchemin", "Le texte d'introduction lu à voix haute aux joueurs au début de la quête. Écrivez votre propre texte.");
        model.parchment = GUILayout.TextArea(model.parchment ?? "", GUILayout.MinHeight(70));
        TitleHelp("Note secrète pour Zargon", "Consignes réservées à Zargon (le maître du jeu) : pièges, surprises, comment jouer les monstres… Les joueurs ne la voient pas.");
        model.zargonNote = GUILayout.TextArea(model.zargonNote ?? "", GUILayout.MinHeight(40));

        GUILayout.Space(8);
        DrawRules();
        GUILayout.Space(8);
        DrawReward();
        GUILayout.Space(8);
        DrawRockZones();
        GUILayout.Space(8);
    }

    // choix du monstre a vaincre parmi TOUS les monstres poses ;
    // un monstre sans nom en recoit un automatiquement quand on le choisit
    void MonsterPicker(ObjectiveEntry o)
    {
        List<MonsterEntry> list = model.monsters;
        int index = list.FindIndex(m => !string.IsNullOrEmpty(m.id) && m.id == o.target);

        GUILayout.BeginHorizontal(GUILayout.Width(300));
        if (GUILayout.Button("<", GUILayout.Width(20)) && list.Count > 0)
        {
            MonsterEntry previous = list[(index - 1 + list.Count) % list.Count];
            Defer(() => o.target = model.EnsureMonsterId(previous));
        }
        string shown;
        if (list.Count == 0) shown = "(posez d'abord un monstre)";
        else if (index < 0) shown = string.IsNullOrEmpty(o.target) ? "(choisir un monstre)" : "\"" + o.target + "\" introuvable";
        else shown = MonsterLabel(list[index]);
        GUILayout.Label(shown, centered, GUILayout.ExpandWidth(true));
        if (GUILayout.Button(">", GUILayout.Width(20)) && list.Count > 0)
        {
            MonsterEntry next = list[(index + 1) % list.Count];
            Defer(() => o.target = model.EnsureMonsterId(next));
        }
        GUILayout.EndHorizontal();
    }

    static string MonsterLabel(MonsterEntry m)
    {
        string name = QuestEditorNames.Of(QuestEditorModel.ParseEnum(m.type, MonsterClass.Gobelin));
        return name + " \"" + m.id + "\" (col. " + m.x + ", lig. " + m.y + ")";
    }

    // choix rapide parmi les objets des tresors poses : remplit l'objet ET la salle
    void TreasurePicker(ObjectiveEntry o)
    {
        List<TreasureEntry> list = model.TreasuresWithItem();
        if (list.Count == 0) return;
        int index = list.FindIndex(tr => tr.item == o.target && model.ZoneAt(tr.x, tr.y) == o.room);

        if (GUILayout.Button(new GUIContent(index < 0 ? "trésor…" : "trésor " + (index + 1) + "/" + list.Count,
            "Prendre l'objet d'un trésor posé sur le plateau : l'objet et la salle se remplissent tout seuls. Cliquez encore pour passer au trésor suivant."), GUILayout.Width(90)))
        {
            TreasureEntry next = list[(index + 1) % list.Count];
            Defer(() => { o.target = next.item; o.room = model.ZoneAt(next.x, next.y); });
        }
    }

    // position, taille et rotation de l'escalier ; si les heros partent de l'escalier, leurs cases suivent
    void StairsFields()
    {
        RectEntry s = model.stairs;
        int oldX = s.x, oldY = s.y, oldW = s.width, oldH = s.height;

        GUILayout.BeginHorizontal();
        CellFields(ref s.x, ref s.y);
        Label("larg.", 34);
        s.width = Mathf.Max(1, IntField(s.width, 30));
        Label("haut.", 34);
        s.height = Mathf.Max(1, IntField(s.height, 30));
        int rot = ((s.rotation % 360) + 360) % 360;
        if (GUILayout.Button("tourner " + rot + "°", GUILayout.Width(90)))
        {
            // les heros tournent avec l'image de l'escalier
            Defer(() =>
            {
                s.rotation = (rot + 90) % 360;
                if (model.startType == "stairs") model.heroStarts = model.DefaultHeroStarts();
            });
        }
        GUILayout.EndHorizontal();

        bool moved = s.x != oldX || s.y != oldY || s.width != oldW || s.height != oldH;
        if (moved && model.startType == "stairs") Defer(() => model.heroStarts = model.DefaultHeroStarts());
    }

    void DrawRules()
    {
        TitleHelp("Règles spéciales", "Des règles propres à cette quête, en plus des règles normales. Cliquez pour activer ou désactiver.");
        foreach (QuestRule rule in Enum.GetValues(typeof(QuestRule)))
        {
            string name = rule.ToString();
            bool on = model.rules.Contains(name);
            ToggleField(on, QuestEditorNames.Of(rule), v =>
            {
                if (v && !model.rules.Contains(name)) model.rules.Add(name);
                if (!v) model.rules.Remove(name);
            });
        }
    }

    void DrawReward()
    {
        TitleHelp("Récompense", "L'or gagné quand la quête est réussie : une somme à partager entre les héros, une somme pour chaque héros, et des primes pour chaque monstre vaincu d'un type donné.");
        GUILayout.BeginHorizontal();
        Label("À partager", 110);
        model.goldShared = Mathf.Max(0, IntField(model.goldShared, 60));
        Label("pièces d'or", 80);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        Label("Pour chaque héros", 110);
        model.goldEach = Mathf.Max(0, IntField(model.goldEach, 60));
        Label("pièces d'or", 80);
        GUILayout.EndHorizontal();

        for (int i = 0; i < model.bounties.Count; i++)
        {
            BountyEntry b = model.bounties[i];
            GUILayout.BeginHorizontal();
            Label("Prime", 50);
            EnumCycle(QuestEditorModel.ParseEnum(b.monster, MonsterClass.Gobelin), v => b.monster = v.ToString(), 190, QuestEditorNames.Of);
            b.gold = Mathf.Max(0, IntField(b.gold, 50));
            Label("or", 20);
            int idx = i;
            if (GUILayout.Button("×", GUILayout.Width(22))) Defer(() => model.bounties.RemoveAt(idx));
            GUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Prime", GUILayout.Width(100)))
            Defer(() => model.bounties.Add(new BountyEntry { monster = MonsterClass.Gobelin.ToString(), gold = 10 }));
    }

    void DrawRockZones()
    {
        TitleHelp("Zones de roche", "Les zones qui ne servent pas dans cette quête et restent en roche pleine (assombries sur le plateau). Les zones qu'aucune porte ne relie à l'escalier deviennent de la roche toutes seules : cochez ici seulement les exceptions, par exemple tous les couloirs d'une quête où l'on se déplace par téléportation.");
        List<string> all = model.zones;
        const int perRow = 6;
        for (int i = 0; i < all.Count; i += perRow)
        {
            GUILayout.BeginHorizontal();
            for (int j = i; j < Mathf.Min(i + perRow, all.Count); j++)
            {
                string z = all[j];
                bool on = model.rockZones.Contains(z);
                bool edited = GUILayout.Toggle(on, RoomName(z), toggleButton, GUILayout.Width(74));
                if (edited != on) Defer(() => { if (edited) model.rockZones.Add(z); else model.rockZones.Remove(z); });
            }
            GUILayout.EndHorizontal();
        }
    }

    // =====================================================================
    //  panneau de droite : listes
    // =====================================================================

    void DrawLists()
    {
        if (FoldHelp("notes", "Notes A, B, C… (" + model.notes.Count + ")", true, "Une lettre posée sur la carte renvoie à un texte réservé à Zargon. La même lettre peut être posée sur plusieurs cases ; son texte ne s'écrit qu'une fois. Colonne et ligne à -1 : la note n'est pas sur la carte."))
        {
            foreach (NoteEntry note in model.notes) NoteRow(note);
            if (GUILayout.Button("+ Note", GUILayout.Width(100)))
                Defer(() => model.notes.Add(new NoteEntry { letter = model.NextNoteLetter(), text = "", x = -1, y = -1 }));
            GUILayout.Space(6);
        }

        if (FoldHelp("monsters", "Monstres (" + model.monsters.Count + ")", false,
            "Chaque monstre : son type, sa case et un nom facultatif.\n\n"
            + "Caractéristiques : dépl. = cases de déplacement, att. / déf. = dés d'attaque / de défense, corps = points de corps, esprit = points d'esprit. Les valeurs affichées sont celles du monstre normal ; changez-les pour un chef. Remettre la valeur normale (ou 0) annule le changement.\n\n"
            + "Caché : le monstre n'est pas sur le plateau au départ. Statue : il ne bouge pas et ne peut pas être blessé avant de se réveiller. Réveil : ce qui le fait apparaître ou s'animer. Objet : ce qu'il laisse en mourant."))
        {
            foreach (MonsterEntry m in model.monsters) MonsterRow(m);
        }

        if (FoldHelp("allies", "Alliés (" + model.allies.Count + ")", false, "Les personnages à secourir ou à escorter. Chacun a un nom (utilisé par l'objectif « Escorter un allié »), une case, des dés d'attaque et de défense et des points de corps."))
        {
            foreach (AllyEntry a in model.allies) AllyRow(a);
        }

        if (FoldHelp("treasures", "Trésors (" + model.treasures.Count + ")", false,
            "Les trésors spéciaux de la quête. Le premier héros qui fouille la salle les trouve.\n\n"
            + "Or : pièces d'or trouvées. Objet : ce qu'on trouve en plus (une potion, un artefact…).\n"
            + "Piégé : fouiller sans avoir désarmé le piège fait perdre des points de corps.\n"
            + "À vaincre d'abord : le trésor n'est trouvable qu'après la mort de ces monstres (leurs noms, séparés par des virgules)."))
        {
            foreach (TreasureEntry tr in model.treasures) TreasureRow(tr);
        }

        if (FoldHelp("markers", "Repères (" + model.markers.Count + ")", false, "Les cases marquées dont parlent les règles de la quête : X, XX, numéros de téléportation… Chaque repère a une étiquette et une case."))
        {
            foreach (MarkerEntry mk in model.markers) MarkerRow(mk);
        }

        if (FoldHelp("furniture", "Meubles (" + model.furniture.Count + ")", false, "Chaque meuble : son type, la case de son coin en haut à gauche, sa largeur et sa hauteur en cases. Le bouton d'angle le tourne d'un quart de tour (la largeur et la hauteur s'échangent)."))
        {
            foreach (FurnitureEntry f in model.furniture) FurnitureRow(f);
        }

        if (FoldHelp("traps", "Pièges (" + model.traps.Count + ")", false, "Chaque piège : son type et sa case. Fosse : le héros tombe dedans. Bloc qui tombe : le plafond s'effondre et bloque le passage. Lance : une lance jaillit du mur."))
        {
            foreach (TrapEntry t in model.traps) TrapRow(t);
        }

        if (FoldHelp("doors", "Portes (" + (model.doors.Count + model.secretDoors.Count + model.falseDoors.Count) + ")", false, "Une porte se place sur un bord de case : la case, puis le côté (haut, bas, gauche, droite). Il doit y avoir un mur à cet endroit."))
        {
            DrawDoorList(model.doors, "Portes");
            DrawDoorList(model.secretDoors, "Portes secrètes");
            DrawDoorList(model.falseDoors, "Fausses portes");
        }

        if (FoldHelp("heroStarts", "Départ des héros (" + model.heroStarts.Count + "/4)", true, "La case où chaque héros commence la partie. H1 joue en premier, puis H2, H3 et H4. Par défaut, les héros sont sur l'escalier et tournent avec son image ; si la quête commence dans une salle, ils prennent ses premières cases libres. Vous pouvez les déplacer avec l'outil Départ ou ici."))
        {
            foreach (HeroStartEntry h in model.SortedHeroStarts()) HeroStartRow(h);
            if (GUILayout.Button(new GUIContent("Positions par défaut", "Replace les 4 héros sur l'escalier, ou dans la salle de départ si la quête commence dans une salle."), GUILayout.Width(150)))
                Defer(() => model.heroStarts = model.DefaultHeroStarts());
        }

        if (FoldHelp("blocked", "Cases bloquées (" + model.blocked.Count + ")", false, "Les cases que personne ne peut traverser."))
        {
            foreach (CellEntry c in model.blocked) BlockedRow(c);
        }
    }

    void DrawDoorList(List<DoorEntry> list, string label)
    {
        if (list.Count == 0) return;
        GUILayout.Label(label, bold);
        foreach (DoorEntry d in list) DoorRow(d, list);
    }

    // =====================================================================
    //  une ligne par element : utilisees par les listes ET par la selection
    //  le bouton x retire l'element de sa liste (par reference, pas par index)
    // =====================================================================

    bool RemoveButton()
    {
        return GUILayout.Button(new GUIContent("×", "Supprimer"), GUILayout.Width(22));
    }

    void NoteRow(NoteEntry note)
    {
        GUILayout.BeginHorizontal();
        Label("lettre", 40);
        note.letter = StringField(note.letter, 30);
        CellFields(ref note.x, ref note.y);
        if (RemoveButton()) Defer(() => model.notes.Remove(note));
        GUILayout.EndHorizontal();
        note.text = GUILayout.TextArea(note.text ?? "", GUILayout.MinHeight(38));
    }

    void MonsterRow(MonsterEntry m)
    {
        MonsterClass mc = QuestEditorModel.ParseEnum(m.type, MonsterClass.Gobelin);
        GUILayout.BeginHorizontal();
        EnumCycle(mc, v => m.type = v.ToString(), 190, QuestEditorNames.Of);
        CellFields(ref m.x, ref m.y);
        Label("nom", 30);
        m.id = StringField(m.id, 0);
        if (RemoveButton()) Defer(() => model.monsters.Remove(m));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        MonsterData baseStats = BaseStats(mc);
        bool custom = m.movement > 0 || m.attack > 0 || m.defense > 0 || m.body > 0 || m.mind > 0;
        GUILayout.Label(custom ? "modifié" : "normal", hint, GUILayout.Width(52));
        Label("dépl.", 34);
        m.movement = StatField(m.movement, baseStats != null ? baseStats.movementCases : 0, 26);
        Label("att.", 26);
        m.attack = StatField(m.attack, baseStats != null ? baseStats.attackDices : 0, 26);
        Label("déf.", 28);
        m.defense = StatField(m.defense, baseStats != null ? baseStats.defenseDices : 0, 26);
        Label("corps", 36);
        m.body = StatField(m.body, baseStats != null ? baseStats.health : 0, 26);
        Label("esprit", 40);
        m.mind = StatField(m.mind, baseStats != null ? baseStats.spirit : 0, 26);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        ToggleField(m.hidden, "caché", v => m.hidden = v, 64);
        ToggleField(m.statue, "statue", v => m.statue = v, 64);
        MonsterTrigger wake = QuestEditorModel.ParseEnum(m.wakeOn, MonsterTrigger.Aucun);
        EnumCycle(wake, v => { m.wakeOn = v == MonsterTrigger.Aucun ? "" : v.ToString(); }, 250, QuestEditorNames.Of);
        if (wake != MonsterTrigger.Aucun)
            ListCycle(model.Rooms(), m.wakeRoom, v => m.wakeRoom = v, 110, RoomName);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        Label("objet laissé à sa mort", 140);
        m.item = StringField(m.item, 0);
        GUILayout.EndHorizontal();
        GUILayout.Space(4);
    }

    void AllyRow(AllyEntry a)
    {
        GUILayout.BeginHorizontal();
        Label("nom", 30);
        a.id = StringField(a.id, 90);
        CellFields(ref a.x, ref a.y);
        Label("att.", 26);
        a.attack = Mathf.Max(0, IntField(a.attack, 26));
        Label("déf.", 28);
        a.defense = Mathf.Max(0, IntField(a.defense, 26));
        Label("corps", 36);
        a.body = Mathf.Max(0, IntField(a.body, 26));
        if (RemoveButton()) Defer(() => model.allies.Remove(a));
        GUILayout.EndHorizontal();
    }

    void TreasureRow(TreasureEntry tr)
    {
        GUILayout.BeginHorizontal();
        CellFields(ref tr.x, ref tr.y);
        Label("or", 20);
        tr.gold = Mathf.Max(0, IntField(tr.gold, 44));
        Label("objet", 36);
        tr.item = StringField(tr.item, 0);
        if (RemoveButton()) Defer(() => model.treasures.Remove(tr));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        ToggleField(tr.trapped, "piégé", v => tr.trapped = v, 64);
        if (tr.trapped)
        {
            Label("perte de corps", 96);
            tr.trapDamage = Mathf.Max(0, IntField(tr.trapDamage, 30));
        }
        Label("à vaincre d'abord", 110);
        string kills = tr.requiredKills != null ? string.Join(", ", tr.requiredKills) : "";
        string editedKills = StringField(kills, 0);
        if (editedKills != kills) tr.requiredKills = SplitNames(editedKills);
        GUILayout.EndHorizontal();
        GUILayout.Space(4);
    }

    void MarkerRow(MarkerEntry mk)
    {
        GUILayout.BeginHorizontal();
        Label("étiquette", 64);
        mk.label = StringField(mk.label, 50);
        CellFields(ref mk.x, ref mk.y);
        if (RemoveButton()) Defer(() => model.markers.Remove(mk));
        GUILayout.EndHorizontal();
    }

    void FurnitureRow(FurnitureEntry f)
    {
        GUILayout.BeginHorizontal();
        EnumCycle(QuestEditorModel.ParseEnum(f.type, FurnitureType.Table), v => f.type = v.ToString(), 190, QuestEditorNames.Of);
        CellFields(ref f.x, ref f.y);
        Label("larg.", 34);
        f.width = Mathf.Max(1, IntField(f.width, 28));
        Label("haut.", 34);
        f.height = Mathf.Max(1, IntField(f.height, 28));

        int rotIndex = Mathf.Max(0, Array.IndexOf(QuestEditorModel.RotationValues, ((f.rotation % 360) + 360) % 360));
        if (GUILayout.Button(QuestEditorModel.RotationValues[rotIndex] + "°", GUILayout.Width(46)))
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
        if (RemoveButton()) Defer(() => model.furniture.Remove(f));
        GUILayout.EndHorizontal();
    }

    void TrapRow(TrapEntry t)
    {
        GUILayout.BeginHorizontal();
        EnumCycle(QuestEditorModel.ParseEnum(t.type, TrapType.Fosse), v => t.type = v.ToString(), 170, QuestEditorNames.Of);
        CellFields(ref t.x, ref t.y);
        if (RemoveButton()) Defer(() => model.traps.Remove(t));
        GUILayout.EndHorizontal();
    }

    void DoorRow(DoorEntry d, List<DoorEntry> list)
    {
        GUILayout.BeginHorizontal();
        CellFields(ref d.x, ref d.y);
        EnumCycle(QuestEditorModel.ParseEnum(d.side, Side.North), v => d.side = v.ToString(), 110, QuestEditorNames.Of);
        if (RemoveButton()) Defer(() => list.Remove(d));
        GUILayout.EndHorizontal();
    }

    void HeroStartRow(HeroStartEntry h)
    {
        GUILayout.BeginHorizontal();
        Label("H" + h.slot, 30);
        CellFields(ref h.x, ref h.y);
        if (RemoveButton()) Defer(() => model.heroStarts.Remove(h));
        GUILayout.EndHorizontal();
    }

    void BlockedRow(CellEntry c)
    {
        GUILayout.BeginHorizontal();
        CellFields(ref c.x, ref c.y);
        if (RemoveButton()) Defer(() => model.blocked.Remove(c));
        GUILayout.EndHorizontal();
    }

    // =====================================================================
    //  selection : l'outil Selection choisit un element du plateau pour le modifier
    // =====================================================================

    object selected;   // l'element choisi (MonsterEntry, DoorEntry...), null si rien

    // cherche ce qui est sur la case cliquee : la porte si on clique pres d'un bord, sinon ce qui est dessus
    void SelectAt(int cx, int cy, float fx, float fy)
    {
        selected = null;
        GUIUtility.keyboardControl = 0;

        float edge = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));
        if (edge < 0.2f)
        {
            string key = QuestEditorModel.EdgeKey(cx, cy, QuestEditorModel.NearestSide(fx, fy));
            foreach (List<DoorEntry> list in new[] { model.doors, model.secretDoors, model.falseDoors })
            {
                DoorEntry door = list.Find(d => QuestEditorModel.EdgeKey(d) == key);
                if (door != null) { selected = door; return; }
            }
        }

        selected = (object)model.monsters.Find(m => m.x == cx && m.y == cy)
            ?? (object)model.allies.Find(a => a.x == cx && a.y == cy)
            ?? (object)model.treasures.Find(tr => tr.x == cx && tr.y == cy)
            ?? (object)model.traps.Find(t => t.x == cx && t.y == cy)
            ?? (object)model.markers.Find(mk => mk.x == cx && mk.y == cy)
            ?? (object)model.notes.Find(nt => nt.x == cx && nt.y == cy)
            ?? (object)model.furniture.Find(f => QuestEditorModel.Covers(f, cx, cy))
            ?? (object)model.heroStarts.Find(c => c.x == cx && c.y == cy)
            ?? (object)model.blocked.Find(c => c.x == cx && c.y == cy);

        if (selected == null
            && cx >= model.stairs.x && cx < model.stairs.x + model.stairs.width
            && cy >= model.stairs.y && cy < model.stairs.y + model.stairs.height)
            selected = model.stairs;
    }

    // l'element est-il toujours dans la quete ? (supprime, annule, autre quete ouverte...)
    bool StillExists(object item)
    {
        if (item == null) return false;
        if (item == model.stairs) return true;
        if (item is DoorEntry) return model.doors.Contains((DoorEntry)item) || model.secretDoors.Contains((DoorEntry)item) || model.falseDoors.Contains((DoorEntry)item);
        if (item is MonsterEntry) return model.monsters.Contains((MonsterEntry)item);
        if (item is AllyEntry) return model.allies.Contains((AllyEntry)item);
        if (item is TreasureEntry) return model.treasures.Contains((TreasureEntry)item);
        if (item is TrapEntry) return model.traps.Contains((TrapEntry)item);
        if (item is MarkerEntry) return model.markers.Contains((MarkerEntry)item);
        if (item is NoteEntry) return model.notes.Contains((NoteEntry)item);
        if (item is FurnitureEntry) return model.furniture.Contains((FurnitureEntry)item);
        if (item is HeroStartEntry) return model.heroStarts.Contains((HeroStartEntry)item);
        if (item is CellEntry) return model.blocked.Contains((CellEntry)item);
        return false;
    }

    // cadre en haut du panneau de droite avec les reglages de l'element choisi
    void DrawSelection()
    {
        if (model.tool != QuestPaintTool.Selection) return;
        if (selected != null && !StillExists(selected)) selected = null;

        GUILayout.BeginVertical(GUI.skin.box);
        if (selected == null)
        {
            GUILayout.Label("Cliquez sur un élément du plateau (monstre, porte, trésor, meuble…) pour le modifier ici.", hint);
            GUILayout.EndVertical();
            GUILayout.Space(8);
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Sélection : " + SelectionName(selected), bold);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Fermer", GUILayout.Width(70))) Defer(() => selected = null);
        GUILayout.EndHorizontal();

        object item = selected;
        if (item == model.stairs) StairsFields();
        else if (item is DoorEntry)
        {
            DoorEntry d = (DoorEntry)item;
            List<DoorEntry> list = model.doors.Contains(d) ? model.doors : model.secretDoors.Contains(d) ? model.secretDoors : model.falseDoors;
            DoorRow(d, list);
        }
        else if (item is MonsterEntry) MonsterRow((MonsterEntry)item);
        else if (item is AllyEntry) AllyRow((AllyEntry)item);
        else if (item is TreasureEntry) TreasureRow((TreasureEntry)item);
        else if (item is TrapEntry) TrapRow((TrapEntry)item);
        else if (item is MarkerEntry) MarkerRow((MarkerEntry)item);
        else if (item is NoteEntry) NoteRow((NoteEntry)item);
        else if (item is FurnitureEntry) FurnitureRow((FurnitureEntry)item);
        else if (item is HeroStartEntry) HeroStartRow((HeroStartEntry)item);
        else if (item is CellEntry) BlockedRow((CellEntry)item);

        GUILayout.EndVertical();
        GUILayout.Space(8);
    }

    string SelectionName(object item)
    {
        if (item == model.stairs) return "l'escalier";
        if (item is DoorEntry)
        {
            DoorEntry d = (DoorEntry)item;
            if (model.secretDoors.Contains(d)) return "porte secrète";
            if (model.falseDoors.Contains(d)) return "fausse porte";
            return "porte";
        }
        if (item is MonsterEntry) return QuestEditorNames.Of(QuestEditorModel.ParseEnum(((MonsterEntry)item).type, MonsterClass.Gobelin));
        if (item is AllyEntry) return "allié";
        if (item is TreasureEntry) return "trésor";
        if (item is TrapEntry) return "piège";
        if (item is MarkerEntry) return "repère";
        if (item is NoteEntry) return "note " + ((NoteEntry)item).letter;
        if (item is FurnitureEntry) return QuestEditorNames.Of(QuestEditorModel.ParseEnum(((FurnitureEntry)item).type, FurnitureType.Table));
        if (item is HeroStartEntry) return "départ du héros H" + ((HeroStartEntry)item).slot;
        if (item is CellEntry) return "case bloquée";
        return "";
    }

    // cadre dore autour de l'element choisi sur le plateau
    void PaintSelection(Rect r)
    {
        if (model.tool != QuestPaintTool.Selection || selected == null) return;

        Rect box;
        object item = selected;
        if (item == model.stairs) box = SpanRect(r, model.stairs.x, model.stairs.y, model.stairs.width, model.stairs.height, 0f);
        else if (item is FurnitureEntry) { FurnitureEntry f = (FurnitureEntry)item; box = SpanRect(r, f.x, f.y, f.width, f.height, 0f); }
        else if (item is DoorEntry) { DoorEntry d = (DoorEntry)item; box = SpanRect(r, d.x, d.y, 1, 1, 0f); }
        else if (item is MonsterEntry) { MonsterEntry m = (MonsterEntry)item; box = SpanRect(r, m.x, m.y, 1, 1, 0f); }
        else if (item is AllyEntry) { AllyEntry a = (AllyEntry)item; box = SpanRect(r, a.x, a.y, 1, 1, 0f); }
        else if (item is TreasureEntry) { TreasureEntry tr = (TreasureEntry)item; box = SpanRect(r, tr.x, tr.y, 1, 1, 0f); }
        else if (item is TrapEntry) { TrapEntry t = (TrapEntry)item; box = SpanRect(r, t.x, t.y, 1, 1, 0f); }
        else if (item is MarkerEntry) { MarkerEntry mk = (MarkerEntry)item; box = SpanRect(r, mk.x, mk.y, 1, 1, 0f); }
        else if (item is NoteEntry) { NoteEntry nt = (NoteEntry)item; box = SpanRect(r, nt.x, nt.y, 1, 1, 0f); }
        else if (item is CellEntry) { CellEntry c = (CellEntry)item; box = SpanRect(r, c.x, c.y, 1, 1, 0f); }
        else if (item is HeroStartEntry) { HeroStartEntry h = (HeroStartEntry)item; box = SpanRect(r, h.x, h.y, 1, 1, 0f); }
        else return;

        const float t2 = 3f;
        Fill(new Rect(box.x - 1f, box.y - 1f, box.width + 2f, t2), Gold);
        Fill(new Rect(box.x - 1f, box.yMax - t2 + 1f, box.width + 2f, t2), Gold);
        Fill(new Rect(box.x - 1f, box.y - 1f, t2, box.height + 2f), Gold);
        Fill(new Rect(box.xMax - t2 + 1f, box.y - 1f, t2, box.height + 2f), Gold);
    }

    // "verag, ulag" -> ["verag", "ulag"]
    static string[] SplitNames(string text)
    {
        List<string> names = new List<string>();
        foreach (string part in text.Split(','))
        {
            string n = part.Trim();
            if (n.Length > 0) names.Add(n);
        }
        return names.ToArray();
    }
}
