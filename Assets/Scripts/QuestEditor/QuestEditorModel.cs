using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public enum QuestPaintTool { Selection, Porte, PorteSecrete, FaussePorte, CaseBloquee, Meuble, Monstre, Piege, Tresor, Allie, Repere, Escalier, DepartHeros, Note, Gomme }

// donnees + logique de l'editeur de quetes, sans aucun dessin et sans UnityEditor :
// la meme classe sert a la fenetre d'editeur (QuestGeneratorWindow) et au menu du jeu (QuestEditorMenu)
// [Serializable] : la fenetre d'editeur garde la quete en cours apres une recompilation
// l'editeur gere TOUS les champs du format de quete (QuestDefinition) : rien n'est perdu a l'enregistrement
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
    public bool noWanderingMonster;                    // vrai = aucun monstre errant (quete 10)
    public MonsterClass wanderingMonster = MonsterClass.Orc;
    public VictoryMode victoryMode = VictoryMode.Tous;
    public List<ObjectiveEntry> objectives = new List<ObjectiveEntry>();
    public List<DoorEntry> doors = new List<DoorEntry>();
    public List<DoorEntry> secretDoors = new List<DoorEntry>();
    public List<DoorEntry> falseDoors = new List<DoorEntry>();
    public List<string> rockZones = new List<string>();
    public List<CellEntry> blocked = new List<CellEntry>();
    public List<HeroStartEntry> heroStarts = new List<HeroStartEntry>();   // case de depart de H1 a H4 (slot 1 a 4)
    public int heroSlot = 1;                                                // heros place par l'outil Depart (1 a 4)
    public List<FurnitureEntry> furniture = new List<FurnitureEntry>();
    public List<MonsterEntry> monsters = new List<MonsterEntry>();
    public List<TrapEntry> traps = new List<TrapEntry>();
    public List<TreasureEntry> treasures = new List<TreasureEntry>();
    public List<AllyEntry> allies = new List<AllyEntry>();
    public List<MarkerEntry> markers = new List<MarkerEntry>();
    public List<string> rules = new List<string>();
    public int goldShared;
    public int goldEach;
    public List<BountyEntry> bounties = new List<BountyEntry>();
    public string parchment = "";                      // texte lu a voix haute aux joueurs
    public string zargonNote = "";                     // consigne secrete pour Zargon
    public List<NoteEntry> notes = new List<NoteEntry>();

    // ---------- reglages de l'outil ----------
    public QuestPaintTool tool = QuestPaintTool.Porte;
    public FurnitureType furnitureType = FurnitureType.Table;
    public int furnitureW = 3;
    public int furnitureH = 2;
    public int furnitureRot = 0;
    public MonsterClass monsterType = MonsterClass.Gobelin;
    public string monsterId = "";
    public TrapType trapType = TrapType.Fosse;
    public string allyId = "";
    public string markerLabel = "X";
    public string noteLetter = "";                     // vide = prochaine lettre libre

    // ---------- fichier ----------
    public string loadedFile = "";                     // ex : "custom_01.json", vide pour une nouvelle quete
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

    // historique pour le bouton Annuler : etats precedents de la quete (JSON)
    [NonSerialized] List<string> history = new List<string>();
    [NonSerialized] string lastState;
    [NonSerialized] float lastChangeTime;
    const int MaxHistory = 100;

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
        if (falseDoors == null) falseDoors = new List<DoorEntry>();
        if (rockZones == null) rockZones = new List<string>();
        if (blocked == null) blocked = new List<CellEntry>();
        if (heroStarts == null) heroStarts = new List<HeroStartEntry>();
        if (furniture == null) furniture = new List<FurnitureEntry>();
        if (monsters == null) monsters = new List<MonsterEntry>();
        if (traps == null) traps = new List<TrapEntry>();
        if (treasures == null) treasures = new List<TreasureEntry>();
        if (allies == null) allies = new List<AllyEntry>();
        if (markers == null) markers = new List<MarkerEntry>();
        if (rules == null) rules = new List<string>();
        if (bounties == null) bounties = new List<BountyEntry>();
        if (notes == null) notes = new List<NoteEntry>();
    }

    // =====================================================================
    //  annuler
    // =====================================================================

    // a appeler apres chaque modification possible (une fois par image) : garde l'etat precedent
    // les frappes rapides au clavier (moins d'une seconde d'ecart) comptent pour une seule etape
    public void TrackChanges()
    {
        if (history == null) history = new List<string>();
        string now = Serialize();
        if (lastState == null)
        {
            lastState = now;
            return;
        }
        if (now == lastState) return;

        float time = Time.realtimeSinceStartup;
        if (history.Count == 0 || time - lastChangeTime > 1f)
        {
            history.Add(lastState);
            if (history.Count > MaxHistory) history.RemoveAt(0);
        }
        lastChangeTime = time;
        lastState = now;
    }

    public bool CanUndo
    {
        get { return history != null && history.Count > 0; }
    }

    public void Undo()
    {
        if (!CanUndo) return;
        string previous = history[history.Count - 1];
        history.RemoveAt(history.Count - 1);

        string file = loadedFile;
        string saved = snapshot;
        Apply(JsonUtility.FromJson<EditorQuestDefinition>(previous));
        loadedFile = file;
        snapshot = saved;
        lastState = Serialize();
        lastChangeTime = 0f;
    }

    // nouvelle quete ou quete ouverte : on ne revient pas en arriere vers la precedente
    void ClearHistory()
    {
        if (history == null) history = new List<string>();
        history.Clear();
        lastState = null;
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

    // l'editeur liste aussi les quetes du livret : on peut les ouvrir pour s'en inspirer
    public void RefreshFileList()
    {
        EnsureInit();
        fileNames = QuestStorage.ListFiles(true);
        if (selectedFile >= fileNames.Count || selectedFile < 0) selectedFile = 0;
    }

    // l'editeur n'enregistre que des quetes perso
    public string FileNameFor(int questNumber)
    {
        return QuestStorage.CustomFileName(questNumber);
    }

    // numero de la prochaine quete perso : plus grand numero existant + 1
    public int NextNumber()
    {
        int max = 0;
        foreach (string name in fileNames)
        {
            Match m = Regex.Match(name, "^" + QuestStorage.CustomPrefix + @"(\d+)\.json$");
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
        if (!copyCurrent) ResetContent();
        snapshot = Serialize();
        ClearHistory();
    }

    void ResetContent()
    {
        stairs = new RectEntry { x = 1, y = 14, width = 2, height = 2 };
        startType = "stairs";
        startRoom = "";
        noWanderingMonster = false;
        wanderingMonster = MonsterClass.Orc;
        victoryMode = VictoryMode.Tous;
        objectives = new List<ObjectiveEntry> { new ObjectiveEntry { type = ObjectiveType.Escalier.ToString() } };
        doors = new List<DoorEntry>();
        secretDoors = new List<DoorEntry>();
        falseDoors = new List<DoorEntry>();
        rockZones = new List<string>();
        blocked = new List<CellEntry>();
        heroStarts = DefaultHeroStarts();
        furniture = new List<FurnitureEntry>();
        monsters = new List<MonsterEntry>();
        traps = new List<TrapEntry>();
        treasures = new List<TreasureEntry>();
        allies = new List<AllyEntry>();
        markers = new List<MarkerEntry>();
        rules = new List<string>();
        goldShared = 0;
        goldEach = 0;
        bounties = new List<BountyEntry>();
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
            message = "Cannot open the quest: " + e.Message;
            return;
        }
        if (q == null) return;

        Apply(q);
        ClearHistory();

        if (QuestStorage.IsOfficial(fileName))
        {
            // une quete du livret ne s'ecrase jamais : elle devient une nouvelle quete perso
            number = NextNumber();
            loadedFile = "";
            snapshot = "";
            message = "Rulebook quest opened: your changes will be saved as a new quest (no. " + number + ").";
            return;
        }

        loadedFile = fileName;
        snapshot = Serialize();
    }

    // remplit l'editeur avec le contenu d'une quete
    void Apply(EditorQuestDefinition q)
    {
        number = q.number;
        title = q.title ?? "";
        stairs = q.stairs ?? new RectEntry { x = 1, y = 14, width = 2, height = 2 };
        startType = (q.start != null && q.start.type == "room") ? "room" : "stairs";
        startRoom = (q.start != null && q.start.room != null) ? q.start.room : "";
        doors = ToList(q.doors);
        secretDoors = ToList(q.secretDoors);
        falseDoors = ToList(q.falseDoors);
        rockZones = ToList(q.rockZones);
        blocked = ToList(q.blocked);
        heroStarts = q.start != null ? ToList(q.start.heroes) : new List<HeroStartEntry>();
        if (heroStarts.Count == 0) heroStarts = DefaultHeroStarts();   // ancienne quete sans le champ
        furniture = ToList(q.furniture);
        monsters = ToList(q.monsters);
        traps = ToList(q.traps);
        treasures = ToList(q.treasures);
        allies = ToList(q.allies);
        markers = ToList(q.markers);
        rules = ToList(q.rules);
        goldShared = q.reward != null ? q.reward.goldShared : 0;
        goldEach = q.reward != null ? q.reward.goldEach : 0;
        bounties = q.reward != null ? ToList(q.reward.bounties) : new List<BountyEntry>();
        parchment = !string.IsNullOrEmpty(q.parchment) ? q.parchment : (q.intro ?? "");
        zargonNote = q.zargonNote ?? "";
        notes = ToList(q.notes);

        // vide dans le JSON = aucun monstre errant (ne pas le remplacer par un Orc)
        noWanderingMonster = string.IsNullOrEmpty(q.wanderingMonster);
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
        q.falseDoors = falseDoors.ToArray();
        q.rockZones = rockZones.ToArray();
        q.blocked = blocked.ToArray();
        q.start.heroes = SortedHeroStarts();
        q.furniture = furniture.ToArray();
        q.monsters = monsters.ToArray();
        q.traps = traps.ToArray();
        q.wanderingMonster = noWanderingMonster ? "" : wanderingMonster.ToString();
        q.victory = new VictoryEntry { mode = victoryMode.ToString(), objectives = objectives.ToArray() };
        q.intro = parchment ?? "";   // "intro" est le champ lu par le jeu (GameState.intro)
        q.treasures = treasures.ToArray();
        q.allies = allies.ToArray();
        q.markers = markers.ToArray();
        q.rules = rules.ToArray();
        q.reward = new RewardEntry { goldShared = goldShared, goldEach = goldEach, bounties = bounties.ToArray() };
        q.parchment = parchment ?? "";
        q.zargonNote = zargonNote ?? "";
        q.notes = notes.ToArray();
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
            message = "Cannot save: " + e.Message;
            return;
        }

        loadedFile = name;
        snapshot = json;
        RefreshFileList();
        int index = fileNames.IndexOf(name);
        if (index >= 0) selectedFile = index;
        message = "Quest no. " + number + " saved";
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
                ToggleDoor(cx, cy, NearestSide(fx, fy), doors);
                break;

            case QuestPaintTool.PorteSecrete:
                ToggleDoor(cx, cy, NearestSide(fx, fy), secretDoors);
                break;

            case QuestPaintTool.FaussePorte:
                ToggleDoor(cx, cy, NearestSide(fx, fy), falseDoors);
                break;

            case QuestPaintTool.CaseBloquee:
                if (blocked.RemoveAll(c => c.x == cx && c.y == cy) == 0)
                    blocked.Add(new CellEntry { x = cx, y = cy });
                break;

            case QuestPaintTool.Meuble:
                if (cx + furnitureW > map.width || cy + furnitureH > map.height)
                {
                    message = "The furniture goes off the board";
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
                monsterId = ""; // un nom ne doit servir qu'a un seul monstre
                break;

            case QuestPaintTool.Piege:
                traps.RemoveAll(p => p.x == cx && p.y == cy);
                traps.Add(new TrapEntry { type = trapType.ToString(), x = cx, y = cy });
                break;

            case QuestPaintTool.Tresor:
                if (map.Get(cx, cy).IsCorridor())
                {
                    message = "A treasure is always in a room, never in a corridor";
                    break;
                }
                if (!treasures.Exists(tr => tr.x == cx && tr.y == cy))
                    treasures.Add(new TreasureEntry { x = cx, y = cy, item = "" });
                break;

            case QuestPaintTool.Allie:
                allies.RemoveAll(a => a.x == cx && a.y == cy);
                allies.Add(new AllyEntry { id = string.IsNullOrEmpty(allyId) ? NextAllyId() : allyId, x = cx, y = cy, attack = 2, defense = 2, body = 4 });
                allyId = "";
                break;

            case QuestPaintTool.Repere:
                string label = string.IsNullOrEmpty(markerLabel) ? "X" : markerLabel.ToUpper();
                if (!markers.Exists(mk => mk.x == cx && mk.y == cy && mk.label == label))
                    markers.Add(new MarkerEntry { label = label, x = cx, y = cy });
                break;

            case QuestPaintTool.Escalier:
                stairs.x = cx;
                stairs.y = cy;
                if (startType == "stairs") heroStarts = DefaultHeroStarts();   // les heros restent sur l'escalier
                break;

            case QuestPaintTool.DepartHeros:
                SetHeroStart(heroSlot, cx, cy);
                heroSlot = heroSlot % HeroStartLayout.Count + 1;   // on enchaine : H1, H2, H3, H4, puis H1
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
            if (falseDoors.RemoveAll(d => EdgeKey(d) == key) > 0) return;
        }

        if (monsters.RemoveAll(m => m.x == cx && m.y == cy) > 0) return;
        if (allies.RemoveAll(a => a.x == cx && a.y == cy) > 0) return;
        if (traps.RemoveAll(p => p.x == cx && p.y == cy) > 0) return;
        if (treasures.RemoveAll(tr => tr.x == cx && tr.y == cy) > 0) return;
        if (markers.RemoveAll(mk => mk.x == cx && mk.y == cy) > 0) return;
        if (notes.RemoveAll(nt => nt.x == cx && nt.y == cy) > 0) return;
        if (heroStarts.RemoveAll(c => c.x == cx && c.y == cy) > 0) return;
        if (furniture.RemoveAll(f => Covers(f, cx, cy)) > 0) return;
        blocked.RemoveAll(c => c.x == cx && c.y == cy);
    }

    // un bord ne porte qu'une seule porte : poser un type remplace les deux autres
    void ToggleDoor(int x, int y, Side side, List<DoorEntry> target)
    {
        string key = EdgeKey(x, y, side);
        if (target.RemoveAll(d => EdgeKey(d) == key) > 0) return;   // deja la : on la retire

        foreach (List<DoorEntry> other in new[] { doors, secretDoors, falseDoors })
            if (other != target) other.RemoveAll(d => EdgeKey(d) == key);
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

    // positions par defaut (HeroStartLayout, partage avec le QuestLoader) :
    // sur l'escalier, elles tournent avec son image ; dans une salle, les 4 premieres cases libres
    public List<HeroStartEntry> DefaultHeroStarts()
    {
        Vector2Int[] cells;
        if (startType == "room" && map != null)
        {
            string room = startRoom;
            cells = HeroStartLayout.InRoom(map.width, map.height, (x, y) =>
            {
                if (map.Get(x, y).zone != room || IsBlocked(x, y)) return false;
                foreach (FurnitureEntry f in furniture) if (Covers(f, x, y)) return false;
                foreach (MonsterEntry m in monsters) if (m.x == x && m.y == y) return false;
                return true;
            });
        }
        else cells = HeroStartLayout.OnStairs(stairs.x, stairs.y, stairs.width, stairs.height, stairs.rotation);

        List<HeroStartEntry> list = new List<HeroStartEntry>();
        for (int i = 0; i < cells.Length; i++)
            list.Add(new HeroStartEntry { slot = i + 1, x = cells[i].x, y = cells[i].y });
        return list;
    }

    // place le heros "slot" sur la case ; un autre heros deja sur cette case lui laisse la place
    public void SetHeroStart(int slot, int cx, int cy)
    {
        if (slot < 1 || slot > HeroStartLayout.Count) return;
        heroStarts.RemoveAll(h => h.slot == slot || (h.x == cx && h.y == cy));
        heroStarts.Add(new HeroStartEntry { slot = slot, x = cx, y = cy });
        heroStarts.Sort((a, b) => a.slot.CompareTo(b.slot));
    }

    public HeroStartEntry[] SortedHeroStarts()
    {
        List<HeroStartEntry> sorted = new List<HeroStartEntry>(heroStarts);
        sorted.Sort((a, b) => a.slot.CompareTo(b.slot));
        return sorted.ToArray();
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

    // nom libre pour un nouvel allie : "allie1", "allie2"...
    string NextAllyId()
    {
        for (int i = 1; ; i++)
        {
            string id = "ally" + i;
            if (!allies.Exists(a => a.id == id)) return id;
        }
    }

    // noms donnes aux monstres (pour les objectifs, les tresors et les primes)
    public List<string> MonsterIds()
    {
        List<string> ids = new List<string>();
        foreach (MonsterEntry m in monsters)
            if (!string.IsNullOrEmpty(m.id) && !ids.Contains(m.id)) ids.Add(m.id);
        return ids;
    }

    // donne un nom au monstre s'il n'en a pas ("orc1", "orc2"...) et le renvoie
    // permet de choisir n'importe quel monstre pose comme cible d'un objectif
    public string EnsureMonsterId(MonsterEntry monster)
    {
        if (!string.IsNullOrEmpty(monster.id)) return monster.id;
        string baseName = (monster.type ?? "monster").ToLower();
        for (int i = 1; ; i++)
        {
            string id = baseName + i;
            if (!monsters.Exists(m => m.id == id))
            {
                monster.id = id;
                return id;
            }
        }
    }

    // objets des tresors poses (pour l'objectif "Find an item")
    public List<TreasureEntry> TreasuresWithItem()
    {
        return treasures.FindAll(tr => !string.IsNullOrWhiteSpace(tr.item));
    }

    public string ZoneAt(int x, int y)
    {
        return map != null && map.InBounds(x, y) ? map.Get(x, y).zone : "";
    }

    public List<string> AllyIds()
    {
        List<string> ids = new List<string>();
        foreach (AllyEntry a in allies)
            if (!string.IsNullOrEmpty(a.id) && !ids.Contains(a.id)) ids.Add(a.id);
        return ids;
    }

    // salles du plateau, sans le couloir
    public List<string> Rooms()
    {
        return zones.FindAll(z => z != ".");
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
    //  les messages sont lus par les joueurs : phrases simples, sans noms de code
    // =====================================================================

    public void Validate()
    {
        EnsureInit();
        errors.Clear();
        warnings.Clear();
        if (map == null) return;

        if (string.IsNullOrWhiteSpace(title)) warnings.Add("The quest has no title.");
        if (number < 1) errors.Add("The quest number must be at least 1.");

        // escalier et depart
        if (!InRect(stairs.x, stairs.y, stairs.width, stairs.height))
            errors.Add("The stairway goes off the board.");
        if (startType == "room" && (startRoom == "." || !zones.Contains(startRoom)))
            errors.Add("Choose the room where the heroes start.");

        // portes
        HashSet<string> edges = new HashSet<string>();
        CheckDoors(doors, "A door", edges);
        CheckDoors(secretDoors, "A secret door", edges);
        CheckDoors(falseDoors, "A false door", edges);

        // zones de roche
        foreach (string z in rockZones)
            if (!zones.Contains(z)) errors.Add("Unknown solid rock area: " + ZoneName(z) + ".");

        // cases bloquees
        foreach (CellEntry c in blocked)
            if (!map.InBounds(c.x, c.y)) errors.Add("A blocked square is off the board " + Cell(c.x, c.y) + ".");

        // meubles
        Dictionary<Vector2Int, string> furnitureCells = new Dictionary<Vector2Int, string>();
        foreach (FurnitureEntry f in furniture)
        {
            string name = QuestEditorNames.Of(ParseEnum(f.type, FurnitureType.Table));
            if (!InRect(f.x, f.y, f.width, f.height))
            {
                errors.Add(name + " goes off the board " + Cell(f.x, f.y) + ".");
                continue;
            }
            if (Array.IndexOf(RotationValues, ((f.rotation % 360) + 360) % 360) < 0)
                warnings.Add(name + ": the orientation must be 0, 90, 180 or 270 degrees.");
            if (!Enum.IsDefined(typeof(FurnitureType), f.type ?? ""))
                errors.Add("Unknown furniture type: \"" + f.type + "\".");
            if (!SingleZone(f.x, f.y, f.width, f.height))
                errors.Add(name + " " + Cell(f.x, f.y) + " overlaps two rooms.");

            for (int y = f.y; y < f.y + f.height; y++)
            {
                for (int x = f.x; x < f.x + f.width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (furnitureCells.ContainsKey(cell))
                        errors.Add("Two pieces of furniture overlap " + Cell(x, y) + ".");
                    furnitureCells[cell] = f.type;
                }
            }
        }

        // monstres
        HashSet<string> ids = new HashSet<string>();
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        foreach (MonsterEntry m in monsters)
        {
            string name = QuestEditorNames.Of(ParseEnum(m.type, MonsterClass.Gobelin));
            if (!map.InBounds(m.x, m.y))
            {
                errors.Add(name + " is off the board " + Cell(m.x, m.y) + ".");
                continue;
            }
            Vector2Int cell = new Vector2Int(m.x, m.y);
            if (!occupied.Add(cell)) errors.Add("Two characters on the same square " + Cell(m.x, m.y) + ".");
            if (furnitureCells.ContainsKey(cell)) errors.Add(name + " stands on furniture " + Cell(m.x, m.y) + ".");
            if (IsBlocked(m.x, m.y)) errors.Add(name + " is on a blocked square " + Cell(m.x, m.y) + ".");
            if (!string.IsNullOrEmpty(m.id) && !ids.Add(m.id)) errors.Add("Two monsters have the same name: \"" + m.id + "\".");

            MonsterTrigger wake = ParseEnum(m.wakeOn, MonsterTrigger.Aucun);
            if (wake != MonsterTrigger.Aucun && !Rooms().Contains(m.wakeRoom ?? ""))
                errors.Add(name + " " + Cell(m.x, m.y) + ": choose the room that wakes it up.");
        }

        // allies
        HashSet<string> allyIds = new HashSet<string>();
        foreach (AllyEntry a in allies)
        {
            if (!map.InBounds(a.x, a.y))
            {
                errors.Add("The ally \"" + a.id + "\" is off the board " + Cell(a.x, a.y) + ".");
                continue;
            }
            if (string.IsNullOrEmpty(a.id)) errors.Add("An ally has no name " + Cell(a.x, a.y) + ".");
            else if (!allyIds.Add(a.id)) errors.Add("Two allies have the same name: \"" + a.id + "\".");
            if (!occupied.Add(new Vector2Int(a.x, a.y))) errors.Add("Two characters on the same square " + Cell(a.x, a.y) + ".");
            if (a.body < 1) errors.Add("The ally \"" + a.id + "\" must have at least 1 Body point.");
        }

        // pieges
        foreach (TrapEntry t in traps)
        {
            if (!map.InBounds(t.x, t.y)) errors.Add("A trap is off the board " + Cell(t.x, t.y) + ".");
            else if (furnitureCells.ContainsKey(new Vector2Int(t.x, t.y))) errors.Add("A trap is under furniture " + Cell(t.x, t.y) + ".");
        }

        // tresors : toujours dans une salle (on ne fouille pas les couloirs)
        foreach (TreasureEntry tr in treasures)
        {
            if (!map.InBounds(tr.x, tr.y))
            {
                errors.Add("A treasure is off the board " + Cell(tr.x, tr.y) + ".");
                continue;
            }
            if (map.Get(tr.x, tr.y).IsCorridor()) errors.Add("A treasure is in a corridor " + Cell(tr.x, tr.y) + ": it must be in a room.");
            if (tr.gold <= 0 && string.IsNullOrWhiteSpace(tr.item)) warnings.Add("The treasure " + Cell(tr.x, tr.y) + " contains nothing.");
            if (tr.trapped && tr.trapDamage < 1) warnings.Add("The trapped treasure " + Cell(tr.x, tr.y) + " does not remove any Body point.");
            if (tr.requiredKills != null)
                foreach (string id in tr.requiredKills)
                    if (!ids.Contains(id)) errors.Add("The treasure " + Cell(tr.x, tr.y) + " waits for the death of \"" + id + "\", but no monster has this name.");
        }

        // reperes
        foreach (MarkerEntry mk in markers)
        {
            if (!map.InBounds(mk.x, mk.y)) errors.Add("The marker " + mk.label + " is off the board " + Cell(mk.x, mk.y) + ".");
            if (string.IsNullOrWhiteSpace(mk.label)) warnings.Add("A marker has no label " + Cell(mk.x, mk.y) + ".");
        }

        // depart des heros : 4 cases libres, sur l'escalier ou dans la salle de depart
        for (int slot = 1; slot <= HeroStartLayout.Count; slot++)
            if (!heroStarts.Exists(h => h.slot == slot)) errors.Add("Hero H" + slot + " has no starting square (Heroes tool).");
        HashSet<Vector2Int> startCells = new HashSet<Vector2Int>();
        HashSet<int> slots = new HashSet<int>();
        foreach (HeroStartEntry c in heroStarts)
        {
            string who = "The start of hero H" + c.slot + " " + Cell(c.x, c.y);
            if (c.slot < 1 || c.slot > HeroStartLayout.Count) errors.Add(who + ": the number must be between 1 and " + HeroStartLayout.Count + ".");
            else if (!slots.Add(c.slot)) errors.Add("Hero H" + c.slot + " has two starting squares.");
            if (!map.InBounds(c.x, c.y))
            {
                errors.Add(who + " is off the board.");
                continue;
            }
            Vector2Int cell = new Vector2Int(c.x, c.y);
            if (!startCells.Add(cell)) errors.Add("Two heroes start on the same square " + Cell(c.x, c.y) + ".");
            if (furnitureCells.ContainsKey(cell) || IsBlocked(c.x, c.y)) errors.Add(who + " is on furniture or a blocked square.");
            if (occupied.Contains(cell)) errors.Add(who + " is on the square of a monster or an ally.");
            bool onStairs = c.x >= stairs.x && c.x < stairs.x + stairs.width && c.y >= stairs.y && c.y < stairs.y + stairs.height;
            if (startType == "stairs" && !onStairs) warnings.Add(who + " is not on the stairway.");
            if (startType == "room" && map.Get(c.x, c.y).zone != startRoom) warnings.Add(who + " is not in the starting room.");
        }

        // l'escalier ne doit pas etre sous un meuble
        bool stairsCovered = false;
        for (int y = stairs.y; y < stairs.y + stairs.height; y++)
            for (int x = stairs.x; x < stairs.x + stairs.width; x++)
                if (furnitureCells.ContainsKey(new Vector2Int(x, y))) stairsCovered = true;
        if (stairsCovered) warnings.Add("Furniture covers the stairway.");

        // notes : une meme lettre peut etre posee plusieurs fois sur la carte (ex : quete 7),
        // le texte n'est ecrit qu'une fois
        HashSet<string> lettersWithText = new HashSet<string>();
        foreach (NoteEntry note in notes)
            if (!string.IsNullOrEmpty(note.letter) && !string.IsNullOrWhiteSpace(note.text)) lettersWithText.Add(note.letter);

        foreach (NoteEntry note in notes)
        {
            bool placed = note.x >= 0 || note.y >= 0;
            if (placed && !map.InBounds(note.x, note.y))
                errors.Add("Note " + note.letter + " is off the board " + Cell(note.x, note.y) + ".");
            if (string.IsNullOrWhiteSpace(note.text) && (string.IsNullOrEmpty(note.letter) || !lettersWithText.Contains(note.letter)))
                warnings.Add("Note " + (string.IsNullOrEmpty(note.letter) ? "without letter" : note.letter) + " is empty.");
        }
        if (string.IsNullOrWhiteSpace(parchment)) warnings.Add("The introduction parchment is empty.");

        // regles speciales
        foreach (string r in rules)
            if (!Enum.IsDefined(typeof(QuestRule), r ?? "")) errors.Add("Unknown special rule: \"" + r + "\".");

        // recompense
        if (goldShared < 0 || goldEach < 0) errors.Add("The reward cannot be negative.");
        foreach (BountyEntry b in bounties)
            if (b.gold < 0) errors.Add("A bounty cannot be negative.");

        // victoire
        if (objectives.Count == 0) errors.Add("No objective: the quest can never be won.");
        foreach (ObjectiveEntry o in objectives)
        {
            ObjectiveType ot = ParseEnum(o.type, ObjectiveType.Escalier);
            if (ot == ObjectiveType.Tuer)
            {
                if (string.IsNullOrEmpty(o.target)) errors.Add("Objective \"Defeat a monster\": choose the monster.");
                else if (!ids.Contains(o.target)) errors.Add("Objective \"Defeat a monster\": no monster is called \"" + o.target + "\".");
            }
            if (ot == ObjectiveType.Escorter)
            {
                if (string.IsNullOrEmpty(o.target)) errors.Add("Objective \"Escort\": choose the ally to escort.");
                else if (!allyIds.Contains(o.target)) errors.Add("Objective \"Escort\": no ally is called \"" + o.target + "\".");
            }
            if (ot == ObjectiveType.TrouverObjet)
            {
                if (string.IsNullOrWhiteSpace(o.target)) errors.Add("Objective \"Find an item\": enter the item.");
                if (string.IsNullOrEmpty(o.room) || !Rooms().Contains(o.room)) errors.Add("Objective \"Find an item\": choose the room.");
            }
            if (ot == ObjectiveType.Rapporter)
            {
                if (string.IsNullOrWhiteSpace(o.target)) errors.Add("Objective \"Bring back\": enter the item.");
                if (o.count < 1) errors.Add("Objective \"Bring back\": the number must be at least 1.");
            }
        }
    }

    void CheckDoors(List<DoorEntry> list, string label, HashSet<string> edges)
    {
        foreach (DoorEntry d in list)
        {
            Side side;
            if (!Enum.TryParse(d.side, out side) || !Enum.IsDefined(typeof(Side), side))
            {
                errors.Add(label + " " + Cell(d.x, d.y) + " has an unknown side.");
                continue;
            }

            Vector2Int n = Neighbor(d.x, d.y, side);
            if (!map.InBounds(d.x, d.y) || !map.InBounds(n.x, n.y))
            {
                errors.Add(label + " " + Cell(d.x, d.y) + " leads off the board.");
                continue;
            }
            if (!map.HasWall(d.x, d.y, n.x, n.y))
                errors.Add(label + " " + Cell(d.x, d.y) + ", side " + QuestEditorNames.Of(side) + ", is not on a wall.");
            if (!edges.Add(EdgeKey(d.x, d.y, side)))
                errors.Add("Two doors at the same place " + Cell(d.x, d.y) + ", side " + QuestEditorNames.Of(side) + ".");
        }
    }

    // position lisible par un joueur : "(colonne 5, ligne 3)"
    public static string Cell(int x, int y)
    {
        return "(column " + x + ", row " + y + ")";
    }

    public static string ZoneName(string zone)
    {
        if (zone == ".") return "the corridors";
        if (!string.IsNullOrEmpty(zone) && zone.StartsWith("S")) return "room " + zone.Substring(1);
        return "\"" + zone + "\"";
    }

    bool InRect(int x, int y, int w, int h)
    {
        return w >= 1 && h >= 1 && map.InBounds(x, y) && map.InBounds(x + w - 1, y + h - 1);
    }

    bool SingleZone(int x, int y, int w, int h)
    {
        string zone = map.Get(x, y).zone;
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                if (map.Get(xx, yy).zone != zone) return false;
        return true;
    }

    // cases ou les heros peuvent aller : meme calcul que le QuestLoader (MarkRock)
    // on part de l'escalier et des deux cotes de chaque porte (secretes comprises, fausses exclues),
    // sans traverser une case bloquee ni une zone de roche cochee
    // tout ce qui n'est pas atteint est de la roche inaccessible
    public bool[,] ReachableCells()
    {
        if (map == null) return new bool[0, 0];

        bool[,] reached = new bool[map.width, map.height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        HashSet<string> passages = new HashSet<string>();
        List<DoorEntry> usable = new List<DoorEntry>();
        if (doors != null) usable.AddRange(doors);
        if (secretDoors != null) usable.AddRange(secretDoors);
        foreach (DoorEntry d in usable) passages.Add(EdgeKey(d));

        HashSet<Vector2Int> closed = new HashSet<Vector2Int>();
        foreach (CellEntry c in blocked) closed.Add(new Vector2Int(c.x, c.y));

        Action<int, int> reach = (x, y) =>
        {
            if (!map.InBounds(x, y) || reached[x, y]) return;
            if (closed.Contains(new Vector2Int(x, y))) return;
            if (rockZones.Contains(map.Get(x, y).zone)) return;
            reached[x, y] = true;
            queue.Enqueue(new Vector2Int(x, y));
        };

        reach(stairs.x, stairs.y);
        foreach (DoorEntry d in usable)
        {
            Vector2Int n = Neighbor(d.x, d.y, ParseEnum(d.side, Side.North));
            reach(d.x, d.y);
            reach(n.x, n.y);
        }

        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            foreach (Side side in new[] { Side.North, Side.East, Side.South, Side.West })
            {
                Vector2Int n = Neighbor(c.x, c.y, side);
                if (map.HasWall(c.x, c.y, n.x, n.y) && !passages.Contains(EdgeKey(c.x, c.y, side))) continue;
                reach(n.x, n.y);
            }
        }
        return reached;
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

// noms affiches aux joueurs a la place des noms du code (enum)
public static class QuestEditorNames
{
    public static string Of(MonsterClass v)
    {
        switch (v)
        {
            case MonsterClass.GuerrierDeLaTerreur: return "Dread Warrior";
            default: return v.ToString();
        }
    }

    public static string Of(FurnitureType v)
    {
        switch (v)
        {
            case FurnitureType.Trone: return "Throne";
            case FurnitureType.EtabliAlchimiste: return "Alchemist's Bench";
            case FurnitureType.TableSorcier: return "Sorcerer's Table";
            case FurnitureType.Bibliotheque: return "Bookcase";
            case FurnitureType.Cheminee: return "Fireplace";
            case FurnitureType.RatelierArmes: return "Weapons Rack";
            default: return v.ToString();
        }
    }

    public static string Of(TrapType v)
    {
        switch (v)
        {
            case TrapType.Fosse: return "Pit";
            case TrapType.BlocQuiTombe: return "Falling block";
            default: return "Spear";
        }
    }

    public static string Of(Side v)
    {
        switch (v)
        {
            case Side.North: return "top";
            case Side.South: return "bottom";
            case Side.East: return "right";
            default: return "left";
        }
    }

    public static string Of(VictoryMode v)
    {
        return v == VictoryMode.Tous ? "All objectives" : "Any one objective";
    }

    public static string Of(ObjectiveType v)
    {
        switch (v)
        {
            case ObjectiveType.Tuer: return "Defeat a monster";
            case ObjectiveType.TuerTous: return "Defeat all monsters";
            case ObjectiveType.TrouverObjet: return "Find an item";
            case ObjectiveType.Escorter: return "Escort an ally";
            case ObjectiveType.Rapporter: return "Bring back items";
            default: return "Return to the stairway";
        }
    }

    public static string Of(MonsterTrigger v)
    {
        switch (v)
        {
            case MonsterTrigger.EntrerSalle: return "A hero enters the room";
            case MonsterTrigger.OuvrirPorte: return "A hero opens the room's door";
            case MonsterTrigger.PiegeDeclenche: return "The chest's trap is sprung";
            default: return "Always active";
        }
    }

    public static string Of(QuestRule v)
    {
        switch (v)
        {
            case QuestRule.SauvetageRagnar: return "Rescue: when the ally is found, the alarm sounds and every door opens";
            case QuestRule.CoffresLourds: return "Heavy chests: a hero carries only one chest and rolls only one die to move";
            case QuestRule.SansEquipement: return "No equipment: no weapons and no spells until the equipment is found";
            case QuestRule.Teleportation: return "Magic doors: going through a door teleports the hero to a numbered marker (2 dice)";
            case QuestRule.OrDeLaMine: return "Mine gold: carrying it prevents attacking and defending, and it is worthless at the end";
            case QuestRule.EboulementDerriere: return "Cave-in: the block falls by itself after the last hero has passed";
            case QuestRule.BlocsSansBlocage: return "Falling rocks: blocks do not block the way, but can wound (1 die)";
            default: return v.ToString();
        }
    }
}
