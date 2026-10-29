using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public enum QuestPaintTool { Porte, PorteSecrete, CaseBloquee, Meuble, Monstre, Piege, Escalier, Note, Gomme }

// donnees + logique de l'editeur de quetes, sans aucun dessin et sans UnityEditor :
// la meme classe sert a la fenetre d'editeur (QuestGeneratorWindow) et au menu du jeu (QuestEditorMenu)
// [Serializable] : la fenetre d'editeur garde la quete en cours apres une recompilation
[Serializable]
public class QuestEditorModel
{
    // ---------- quete en cours ----------
    public bool started;
    public int number = 1;
    public string title = "";
    public RectEntry stairs = new RectEntry { x = 1, y = 14, width = 2, height = 2 };
    public string startType = "stairs";                // "stairs" ou "room"
    public string startRoom = "";
    public MonsterClass wanderingMonster = MonsterClass.Orc;
    public VictoryMode victoryMode = VictoryMode.Tous;
    public List<ObjectiveEntry> objectives = new List<ObjectiveEntry>();
    public List<DoorEntry> doors = new List<DoorEntry>();
    public List<DoorEntry> secretDoors = new List<DoorEntry>();
    public List<CellEntry> blocked = new List<CellEntry>();
    public List<FurnitureEntry> furniture = new List<FurnitureEntry>();
    public List<MonsterEntry> monsters = new List<MonsterEntry>();
    public List<TrapEntry> traps = new List<TrapEntry>();
    public string parchment = "";                      // texte lu a voix haute aux joueurs
    public string zargonNote = "";                     // consigne secrete pour Zargon
    public List<NoteEntry> notes = new List<NoteEntry>();

    // champs du JSON que l'editeur ne modifie pas (tresors, allies, marqueurs, regles, recompenses, fausses portes...) :
    // gardes tels quels a l'enregistrement pour ne rien perdre quand on rouvre une quete qui en contient
    public EditorQuestDefinition extras;

    // ---------- reglages de l'outil ----------
    public QuestPaintTool tool = QuestPaintTool.Porte;
    public FurnitureType furnitureType = FurnitureType.Table;
    public int furnitureW = 3;
    public int furnitureH = 2;
    public int furnitureRot = 0;
    public MonsterClass monsterType = MonsterClass.Gobelin;
    public string monsterId = "";
    public TrapType trapType = TrapType.Fosse;
    public string noteLetter = "";                     // vide = prochaine lettre libre

    // ---------- fichier ----------
    public string loadedFile = "";                     // ex : "quest_01.json", vide pour une nouvelle quete
    public string snapshot = "";                       // etat enregistre/charge, pour detecter les modifications
    public int selectedFile;

    // ---------- non serialise ----------
    [NonSerialized] public Map map;
    [NonSerialized] public string boardError = "";
    [NonSerialized] public List<string> zones = new List<string>();
    [NonSerialized] public Dictionary<string, Vector2Int> zoneFirst = new Dictionary<string, Vector2Int>();
    [NonSerialized] public Dictionary<string, Color> zoneColors = new Dictionary<string, Color>();
    [NonSerialized] public List<string> fileNames = new List<string>();
    [NonSerialized] public List<string> errors = new List<string>();
    [NonSerialized] public List<string> warnings = new List<string>();
    [NonSerialized] string message;

    // =====================================================================
    //  initialisation
    // =====================================================================

    // apres une deserialisation, les champs non serialises peuvent etre null
    public void EnsureInit()
    {
        if (zones == null) zones = new List<string>();
        if (zoneFirst == null) zoneFirst = new Dictionary<string, Vector2Int>();
        if (zoneColors == null) zoneColors = new Dictionary<string, Color>();
        if (fileNames == null) fileNames = new List<string>();
        if (errors == null) errors = new List<string>();
        if (warnings == null) warnings = new List<string>();
        if (stairs == null) stairs = new RectEntry { x = 1, y = 14, width = 2, height = 2 };
        if (objectives == null) objectives = new List<ObjectiveEntry>();
        if (doors == null) doors = new List<DoorEntry>();
        if (secretDoors == null) secretDoors = new List<DoorEntry>();
        if (blocked == null) blocked = new List<CellEntry>();
        if (furniture == null) furniture = new List<FurnitureEntry>();
        if (monsters == null) monsters = new List<MonsterEntry>();
        if (traps == null) traps = new List<TrapEntry>();
        if (notes == null) notes = new List<NoteEntry>();
    }

    // message a afficher a l'utilisateur (une seule fois)
    public string TakeMessage()
    {
        string m = message;
        message = null;
        return m;
    }

    // text = contenu de board.txt ; missingMessage = erreur affichee si text est vide
    public void LoadBoard(string text, string missingMessage)
    {
        EnsureInit();
        map = null;
        zones.Clear();
        zoneFirst.Clear();
        zoneColors.Clear();

        if (string.IsNullOrEmpty(text))
        {
            boardError = missingMessage;
            return;
        }

        try
        {
            map = new Map(text);
            boardError = "";
        }
        catch (Exception e)
        {
            map = null;
            boardError = e.Message;
            return;
        }

        for (int y = 0; y < map.height; y++)
        {
            for (int x = 0; x < map.width; x++)
            {
                string z = map.Get(x, y).zone;
                if (zoneFirst.ContainsKey(z)) continue;
                zoneFirst[z] = new Vector2Int(x, y);
                zones.Add(z);
            }
        }

        for (int i = 0; i < zones.Count; i++)
        {
            if (zones[i] == ".") zoneColors[zones[i]] = new Color(0.82f, 0.82f, 0.82f);
            else zoneColors[zones[i]] = Color.HSVToRGB((i * 0.61803f) % 1f, 0.28f, 0.95f);
        }
    }

    // =====================================================================
    //  fichiers
    // =====================================================================

    public void RefreshFileList()
    {
        EnsureInit();
        fileNames = QuestStorage.ListFiles();
        if (selectedFile >= fileNames.Count || selectedFile < 0) selectedFile = 0;
    }

    public string FileNameFor(int questNumber)
    {
        return "quest_" + questNumber.ToString("00") + ".json";
    }

    // numero de la prochaine quete : plus grand numero existant + 1
    public int NextNumber()
    {
        int max = 0;
        foreach (string name in fileNames)
        {
            Match m = Regex.Match(name, @"quest_(\d+)\.json");
            if (m.Success) max = Mathf.Max(max, int.Parse(m.Groups[1].Value));
        }
        return max + 1;
    }

    // nouvelle quete avec le numero suivant ; copyCurrent = repartir du contenu actuel
    public void NewQuest(bool copyCurrent)
    {
        number = NextNumber();
        title = "";
        loadedFile = "";
        if (!copyCurrent)
        {
            ResetContent();
            extras = null;
        }
        snapshot = Serialize();
    }

    void ResetContent()
    {
        stairs = new RectEntry { x = 1, y = 14, width = 2, height = 2 };
        startType = "stairs";
        startRoom = "";
        wanderingMonster = MonsterClass.Orc;
        victoryMode = VictoryMode.Tous;
        objectives = new List<ObjectiveEntry> { new ObjectiveEntry { type = ObjectiveType.Escalier.ToString() } };
        doors = new List<DoorEntry>();
        secretDoors = new List<DoorEntry>();
        blocked = new List<CellEntry>();
        furniture = new List<FurnitureEntry>();
        monsters = new List<MonsterEntry>();
        traps = new List<TrapEntry>();
        parchment = "";
        zargonNote = "";
        notes = new List<NoteEntry>();
    }

    public void LoadQuest(string fileName)
    {
        EditorQuestDefinition q;
        try
        {
            q = JsonUtility.FromJson<EditorQuestDefinition>(QuestStorage.Read(fileName));
        }
        catch (Exception e)
        {
            message = "Lecture impossible : " + e.Message;
            return;
        }
        if (q == null) return;

        number = q.number;
        title = q.title ?? "";
        stairs = q.stairs ?? new RectEntry { x = 1, y = 14, width = 2, height = 2 };
        startType = (q.start != null && q.start.type == "room") ? "room" : "stairs";
        startRoom = (q.start != null && q.start.room != null) ? q.start.room : "";
        doors = ToList(q.doors);
        secretDoors = ToList(q.secretDoors);
        blocked = ToList(q.blocked);
        furniture = ToList(q.furniture);
        monsters = ToList(q.monsters);
        traps = ToList(q.traps);
        parchment = !string.IsNullOrEmpty(q.parchment) ? q.parchment : (q.intro ?? "");
        zargonNote = q.zargonNote ?? "";
        notes = ToList(q.notes);
        wanderingMonster = ParseEnum(q.wanderingMonster, MonsterClass.Orc);
        if (q.victory != null)
        {
            victoryMode = ParseEnum(q.victory.mode, VictoryMode.Tous);
            objectives = ToList(q.victory.objectives);
        }
        else
        {
            victoryMode = VictoryMode.Tous;
            objectives = new List<ObjectiveEntry>();
        }

        extras = q;
        loadedFile = fileName;
        snapshot = Serialize();
    }

    public EditorQuestDefinition BuildDefinition()
    {
        EditorQuestDefinition q = new EditorQuestDefinition();
        q.number = number;
        q.title = title;
        q.stairs = stairs;
        q.start = new StartEntry { type = startType, room = startType == "room" ? startRoom : "" };
        q.doors = doors.ToArray();
        q.secretDoors = secretDoors.ToArray();
        q.blocked = blocked.ToArray();
        q.furniture = furniture.ToArray();
        q.monsters = monsters.ToArray();
        q.traps = traps.ToArray();
        q.parchment = parchment ?? "";
        q.intro = parchment ?? "";   // "intro" est le champ lu par le jeu (GameState.intro)
        q.zargonNote = zargonNote ?? "";
        q.notes = notes.ToArray();
        q.wanderingMonster = wanderingMonster.ToString();
        q.victory = new VictoryEntry { mode = victoryMode.ToString(), objectives = objectives.ToArray() };

        if (extras != null)
        {
            q.falseDoors = extras.falseDoors;
            q.rockZones = extras.rockZones;
            q.treasures = extras.treasures;
            q.allies = extras.allies;
            q.markers = extras.markers;
            q.rules = extras.rules;
            q.reward = extras.reward;
        }
        return q;
    }

    public string Serialize()
    {
        return JsonUtility.ToJson(BuildDefinition(), true);
    }

    public bool IsDirty()
    {
        return Serialize() != snapshot;
    }

    // vrai si l'enregistrement remplacerait un autre fichier que celui qu'on a charge
    public bool NeedsOverwriteConfirm()
    {
        string name = FileNameFor(number);
        return QuestStorage.Exists(name) && name != loadedFile;
    }

    public void Save()
    {
        string json = Serialize();
        string name = FileNameFor(number);
        try
        {
            QuestStorage.Write(name, json);
        }
        catch (Exception e)
        {
            message = "Ecriture impossible : " + e.Message;
            return;
        }

        loadedFile = name;
        snapshot = json;
        RefreshFileList();
        int index = fileNames.IndexOf(name);
        if (index >= 0) selectedFile = index;
        message = "Quete " + number + " enregistree";
    }

    // =====================================================================
    //  actions de l'outil (clics sur le plateau)
    // =====================================================================

    public void ClickCell(int cx, int cy, float fx, float fy, bool erase, QuestPaintTool t)
    {
        if (map == null) return;
        if (erase)
        {
            Erase(cx, cy, fx, fy);
            return;
        }

        switch (t)
        {
            case QuestPaintTool.Porte:
                ToggleDoor(cx, cy, NearestSide(fx, fy), false);
                break;

            case QuestPaintTool.PorteSecrete:
                ToggleDoor(cx, cy, NearestSide(fx, fy), true);
                break;

            case QuestPaintTool.CaseBloquee:
                if (blocked.RemoveAll(c => c.x == cx && c.y == cy) == 0)
                    blocked.Add(new CellEntry { x = cx, y = cy });
                break;

            case QuestPaintTool.Meuble:
                if (cx + furnitureW > map.width || cy + furnitureH > map.height)
                {
                    message = "Le meuble sort du plateau";
                    break;
                }
                furniture.Add(new FurnitureEntry
                {
                    type = furnitureType.ToString(), x = cx, y = cy,
                    width = furnitureW, height = furnitureH, rotation = furnitureRot
                });
                break;

            case QuestPaintTool.Monstre:
                monsters.RemoveAll(m => m.x == cx && m.y == cy);
                monsters.Add(new MonsterEntry { id = monsterId, type = monsterType.ToString(), x = cx, y = cy });
                monsterId = ""; // un id ne doit servir qu'a un seul monstre
                break;

            case QuestPaintTool.Piege:
                traps.RemoveAll(p => p.x == cx && p.y == cy);
                traps.Add(new TrapEntry { type = trapType.ToString(), x = cx, y = cy });
                break;

            case QuestPaintTool.Escalier:
                stairs.x = cx;
                stairs.y = cy;
                break;

            case QuestPaintTool.Note:
                string letter = string.IsNullOrEmpty(noteLetter) ? NextNoteLetter() : noteLetter.ToUpper();
                if (!notes.Exists(nt => nt.x == cx && nt.y == cy && nt.letter == letter))
                    notes.Add(new NoteEntry { letter = letter, text = "", x = cx, y = cy });
                break;
        }
    }

    void Erase(int cx, int cy, float fx, float fy)
    {
        float edge = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));
        if (edge < 0.2f)
        {
            string key = EdgeKey(cx, cy, NearestSide(fx, fy));
            if (doors.RemoveAll(d => EdgeKey(d) == key) > 0) return;
            if (secretDoors.RemoveAll(d => EdgeKey(d) == key) > 0) return;
        }

        if (monsters.RemoveAll(m => m.x == cx && m.y == cy) > 0) return;
        if (traps.RemoveAll(p => p.x == cx && p.y == cy) > 0) return;
        if (notes.RemoveAll(nt => nt.x == cx && nt.y == cy) > 0) return;
        if (furniture.RemoveAll(f => Covers(f, cx, cy)) > 0) return;
        blocked.RemoveAll(c => c.x == cx && c.y == cy);
    }

    void ToggleDoor(int x, int y, Side side, bool secret)
    {
        string key = EdgeKey(x, y, side);
        List<DoorEntry> target = secret ? secretDoors : doors;
        List<DoorEntry> other = secret ? doors : secretDoors;

        if (target.RemoveAll(d => EdgeKey(d) == key) > 0) return;   // deja la : on la retire
        other.RemoveAll(d => EdgeKey(d) == key);                    // sinon on remplace l'autre type
        target.Add(new DoorEntry { x = x, y = y, side = side.ToString() });
    }

    // taille par defaut d'un meuble : grand cote le long de X (rotation 0)
    public void ApplyFurnitureDefault()
    {
        furnitureRot = 0;
        switch (furnitureType)
        {
            case FurnitureType.Trone:
            case FurnitureType.Coffre:
                furnitureW = 1; furnitureH = 1; break;
            case FurnitureType.Bibliotheque:
            case FurnitureType.Cheminee:
            case FurnitureType.RatelierArmes:
            case FurnitureType.Placard:
                furnitureW = 3; furnitureH = 1; break;
            default:
                furnitureW = 3; furnitureH = 2; break;
        }
    }

    // tourne le meuble a poser de +90 ou -90 degres ; la largeur et la hauteur s'echangent
    // a chaque quart de tour (elles sont deja "tournees" dans le JSON)
    public void RotateFurnitureTool(int step)
    {
        int tmp = furnitureW;
        furnitureW = furnitureH;
        furnitureH = tmp;
        furnitureRot = ((furnitureRot + step) % 360 + 360) % 360;
    }

    // premiere lettre A..Z qui n'est pas encore utilisee par une note
    public string NextNoteLetter()
    {
        for (char ch = 'A'; ch <= 'Z'; ch++)
        {
            string letter = ch.ToString();
            if (!notes.Exists(nt => nt.letter == letter)) return letter;
        }
        return "";
    }

    public static Side NearestSide(float fx, float fy)
    {
        float n = fy, s = 1f - fy, w = fx, e = 1f - fx;
        float min = Mathf.Min(Mathf.Min(n, s), Mathf.Min(w, e));
        if (min == n) return Side.North;
        if (min == s) return Side.South;
        if (min == w) return Side.West;
        return Side.East;
    }

    // deux portes decrites depuis deux cases voisines (4,2,East) et (5,2,West) ont la meme cle
    public static string EdgeKey(int x, int y, Side side)
    {
        if (side == Side.East) return x + "," + y + ",V";
        if (side == Side.West) return (x - 1) + "," + y + ",V";
        if (side == Side.South) return x + "," + y + ",H";
        return x + "," + (y - 1) + ",H";
    }

    public static string EdgeKey(DoorEntry d)
    {
        return EdgeKey(d.x, d.y, ParseEnum(d.side, Side.North));
    }

    public static bool Covers(FurnitureEntry f, int x, int y)
    {
        return x >= f.x && x < f.x + f.width && y >= f.y && y < f.y + f.height;
    }

    // =====================================================================
    //  validation (memes regles que le QuestLoader, plus quelques controles de coherence)
    // =====================================================================

    public void Validate()
    {
        EnsureInit();
        errors.Clear();
        warnings.Clear();
        if (map == null) return;

        if (string.IsNullOrWhiteSpace(title)) warnings.Add("La quete n'a pas de titre.");
        if (number < 1) errors.Add("Le numero de quete doit etre >= 1.");

        // escalier et depart
        if (!InRect(stairs.x, stairs.y, stairs.width, stairs.height))
            errors.Add("L'escalier sort du plateau.");
        if (startType == "room" && (startRoom == "." || !zones.Contains(startRoom)))
            errors.Add("Salle de depart inconnue : \"" + startRoom + "\".");

        // portes
        HashSet<string> edges = new HashSet<string>();
        CheckDoors(doors, "Porte", edges);
        CheckDoors(secretDoors, "Porte secrete", edges);

        // cases bloquees
        foreach (CellEntry c in blocked)
            if (!map.InBounds(c.x, c.y)) errors.Add("Case bloquee hors du plateau : (" + c.x + ", " + c.y + ").");

        // meubles
        Dictionary<Vector2Int, string> furnitureCells = new Dictionary<Vector2Int, string>();
        foreach (FurnitureEntry f in furniture)
        {
            if (!InRect(f.x, f.y, f.width, f.height))
            {
                errors.Add("Meuble " + f.type + " hors du plateau : (" + f.x + ", " + f.y + ").");
                continue;
            }
            if (Array.IndexOf(RotationValues, ((f.rotation % 360) + 360) % 360) < 0)
                warnings.Add("Meuble " + f.type + " : rotation " + f.rotation + " (attendu : 0, 90, 180 ou 270).");
            if (!Enum.IsDefined(typeof(FurnitureType), f.type ?? ""))
                errors.Add("Type de meuble inconnu : \"" + f.type + "\".");

            for (int y = f.y; y < f.y + f.height; y++)
            {
                for (int x = f.x; x < f.x + f.width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (furnitureCells.ContainsKey(cell))
                        errors.Add("Meubles qui se chevauchent en (" + x + ", " + y + ").");
                    furnitureCells[cell] = f.type;
                }
            }
        }

        // monstres
        HashSet<string> ids = new HashSet<string>();
        HashSet<Vector2Int> monsterCells = new HashSet<Vector2Int>();
        foreach (MonsterEntry m in monsters)
        {
            if (!map.InBounds(m.x, m.y))
            {
                errors.Add("Monstre " + m.type + " hors du plateau : (" + m.x + ", " + m.y + ").");
                continue;
            }
            Vector2Int cell = new Vector2Int(m.x, m.y);
            if (!monsterCells.Add(cell)) errors.Add("Deux monstres sur la case (" + m.x + ", " + m.y + ").");
            if (furnitureCells.ContainsKey(cell)) errors.Add("Monstre " + m.type + " sur un meuble en (" + m.x + ", " + m.y + ").");
            if (IsBlocked(m.x, m.y)) errors.Add("Monstre " + m.type + " sur une case bloquee en (" + m.x + ", " + m.y + ").");
            if (!string.IsNullOrEmpty(m.id) && !ids.Add(m.id)) errors.Add("Id de monstre en double : \"" + m.id + "\".");
        }

        // pieges
        foreach (TrapEntry t in traps)
        {
            if (!map.InBounds(t.x, t.y)) errors.Add("Piege hors du plateau : (" + t.x + ", " + t.y + ").");
            else if (furnitureCells.ContainsKey(new Vector2Int(t.x, t.y))) errors.Add("Piege sous un meuble en (" + t.x + ", " + t.y + ").");
        }

        // l'escalier ne doit pas etre sous un meuble
        bool stairsCovered = false;
        for (int y = stairs.y; y < stairs.y + stairs.height; y++)
            for (int x = stairs.x; x < stairs.x + stairs.width; x++)
                if (furnitureCells.ContainsKey(new Vector2Int(x, y))) stairsCovered = true;
        if (stairsCovered) warnings.Add("Un meuble recouvre l'escalier.");

        // notes : une meme lettre peut etre posee plusieurs fois sur la carte (ex : quete 7),
        // le texte n'est ecrit qu'une fois
        HashSet<string> lettersWithText = new HashSet<string>();
        foreach (NoteEntry note in notes)
            if (!string.IsNullOrEmpty(note.letter) && !string.IsNullOrWhiteSpace(note.text)) lettersWithText.Add(note.letter);

        foreach (NoteEntry note in notes)
        {
            bool placed = note.x >= 0 || note.y >= 0;
            if (placed && !map.InBounds(note.x, note.y))
                errors.Add("Note " + note.letter + " hors du plateau : (" + note.x + ", " + note.y + ").");
            if (string.IsNullOrWhiteSpace(note.text) && (string.IsNullOrEmpty(note.letter) || !lettersWithText.Contains(note.letter)))
                warnings.Add("La note " + (string.IsNullOrEmpty(note.letter) ? "(sans lettre)" : note.letter) + " est vide.");
        }
        if (string.IsNullOrWhiteSpace(parchment)) warnings.Add("Pas de texte de parchemin.");

        // victoire
        if (objectives.Count == 0) errors.Add("Aucun objectif de victoire : la quete ne pourra jamais etre gagnee.");
        foreach (ObjectiveEntry o in objectives)
        {
            ObjectiveType ot = ParseEnum(o.type, ObjectiveType.Escalier);
            if (ot == ObjectiveType.Tuer)
            {
                if (string.IsNullOrEmpty(o.target)) errors.Add("Objectif Tuer sans cible.");
                else if (!ids.Contains(o.target)) errors.Add("Objectif Tuer : aucun monstre n'a l'id \"" + o.target + "\".");
            }
            if (ot == ObjectiveType.TrouverObjet && (string.IsNullOrEmpty(o.room) || !zones.Contains(o.room)))
                errors.Add("Objectif TrouverObjet : salle inconnue \"" + o.room + "\".");
            if (ot == ObjectiveType.Rapporter && o.count < 1)
                errors.Add("Objectif Rapporter : le nombre doit etre >= 1.");
        }
    }

    void CheckDoors(List<DoorEntry> list, string label, HashSet<string> edges)
    {
        foreach (DoorEntry d in list)
        {
            Side side;
            if (!Enum.TryParse(d.side, out side) || !Enum.IsDefined(typeof(Side), side))
            {
                errors.Add(label + " (" + d.x + ", " + d.y + ") : cote inconnu \"" + d.side + "\".");
                continue;
            }

            Vector2Int n = Neighbor(d.x, d.y, side);
            if (!map.InBounds(d.x, d.y) || !map.InBounds(n.x, n.y))
            {
                errors.Add(label + " (" + d.x + ", " + d.y + ", " + d.side + ") : hors du plateau.");
                continue;
            }
            if (!map.HasWall(d.x, d.y, n.x, n.y))
                errors.Add(label + " (" + d.x + ", " + d.y + ", " + d.side + ") : il n'y a pas de mur a cet endroit.");
            if (!edges.Add(EdgeKey(d.x, d.y, side)))
                errors.Add(label + " en double sur le meme bord : (" + d.x + ", " + d.y + ", " + d.side + ").");
        }
    }

    bool InRect(int x, int y, int w, int h)
    {
        return w >= 1 && h >= 1 && map.InBounds(x, y) && map.InBounds(x + w - 1, y + h - 1);
    }

    bool IsBlocked(int x, int y)
    {
        foreach (CellEntry c in blocked)
            if (c.x == x && c.y == y) return true;
        return false;
    }

    static Vector2Int Neighbor(int x, int y, Side side)
    {
        if (side == Side.North) return new Vector2Int(x, y - 1);
        if (side == Side.South) return new Vector2Int(x, y + 1);
        if (side == Side.East) return new Vector2Int(x + 1, y);
        return new Vector2Int(x - 1, y);
    }

    // =====================================================================
    //  utilitaires
    // =====================================================================

    public static readonly int[] RotationValues = { 0, 90, 180, 270 };

    public Color ZoneColor(string zone)
    {
        Color c;
        return zoneColors.TryGetValue(zone, out c) ? c : Color.gray;
    }

    static List<T> ToList<T>(T[] array)
    {
        return array == null ? new List<T>() : new List<T>(array);
    }

    public static T ParseEnum<T>(string text, T fallback) where T : struct
    {
        T value;
        if (!string.IsNullOrEmpty(text) && Enum.TryParse(text, out value) && Enum.IsDefined(typeof(T), value)) return value;
        return fallback;
    }
}
