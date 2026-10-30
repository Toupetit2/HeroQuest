using System;
using System.Collections.Generic;
using UnityEngine;

// lit un fichier de quete JSON et remplit le GameState (logique seulement, pas d'affichage)
// gameState.map doit deja contenir le plateau (new Map(board.txt))
public class QuestLoader
{
    GameState gameState;
    List<MonsterData> monsterDatas;

    // monstres nommes dans le JSON (ex : "verag"), pour la victoire
    public Dictionary<string, Monster> monstersById = new Dictionary<string, Monster>();

    public QuestLoader(GameState gameState)
    {
        this.gameState = gameState;
        // tous les MonsterData du dossier Resources/Monsters
        monsterDatas = new List<MonsterData>(Resources.LoadAll<MonsterData>("Monsters"));
    }

    public void Load(string json)
    {
        QuestDefinition quest = JsonUtility.FromJson<QuestDefinition>(json);
        Map map = gameState.map;

        gameState.monsterList = new List<Monster>();
        gameState.doorList = new List<Door>();
        gameState.furnitureList = new List<Furniture>();
        gameState.trapList = new List<Trap>();
        gameState.treasureList = new List<Treasure>();
        gameState.allyList = new List<Ally>();
        gameState.markerList = new List<Marker>();
        gameState.rules = new List<QuestRule>();
        gameState.reward = new Reward();
        gameState.intro = quest.intro;
        monstersById.Clear();

        // escalier
        RectEntry s = quest.stairs;
        gameState.stairs = new RectInt(s.x, s.y, s.width, s.height);
        gameState.stairsRotation = ((s.rotation % 360) + 360) % 360;

        // portes : elles doivent etre sur un mur
        foreach (DoorEntry d in quest.doors) AddDoor(d, false, false);
        foreach (DoorEntry d in quest.secretDoors) AddDoor(d, true, false);
        if (quest.falseDoors != null)
            foreach (DoorEntry d in quest.falseDoors) AddDoor(d, false, true);

        // cases bloquees
        foreach (CellEntry c in quest.blocked)
            GetTile(c.x, c.y, "case bloquee").blocked = true;

        // meubles : chaque case couverte pointe vers le meuble
        foreach (FurnitureEntry f in quest.furniture)
        {
            FurnitureType type = ParseEnum<FurnitureType>(f.type, "meuble");
            Furniture furniture = new Furniture(type, f.x, f.y, f.width, f.height, f.rotation);
            gameState.furnitureList.Add(furniture);

            for (int y = f.y; y < f.y + f.height; y++)
                for (int x = f.x; x < f.x + f.width; x++)
                    GetTile(x, y, "meuble " + f.type).furniture = furniture;
        }

        // pieges
        foreach (TrapEntry t in quest.traps)
        {
            TrapType type = ParseEnum<TrapType>(t.type, "piege");
            Trap trap = new Trap(type, t.x, t.y);
            gameState.trapList.Add(trap);
            GetTile(t.x, t.y, "piege").trap = trap;
        }

        // monstres
        foreach (MonsterEntry m in quest.monsters) AddMonster(m);

        // depart des heros (apres les meubles et les monstres, pour verifier que les cases sont libres)
        AddHeroStarts(quest.start, s);

        // monstre errant : vide = aucun
        gameState.hasWanderingMonster = !string.IsNullOrEmpty(quest.wanderingMonster);
        if (gameState.hasWanderingMonster)
            gameState.wanderingMonster = ParseEnum<MonsterClass>(quest.wanderingMonster, "monstre errant");

        // tresors speciaux (apres les monstres, pour verifier les ids)
        if (quest.treasures != null)
            foreach (TreasureEntry t in quest.treasures) AddTreasure(t);

        // allies a escorter
        if (quest.allies != null)
        {
            foreach (AllyEntry a in quest.allies)
            {
                GetTile(a.x, a.y, "allie " + a.id);
                Ally ally = new Ally();
                ally.id = a.id;
                ally.position = new Vector2Int(a.x, a.y);
                ally.attack = a.attack;
                ally.defense = a.defense;
                ally.body = a.body;
                ally.currentHealth = a.body;
                gameState.allyList.Add(ally);
            }
        }

        // reperes
        if (quest.markers != null)
        {
            foreach (MarkerEntry m in quest.markers)
            {
                GetTile(m.x, m.y, "repere " + m.label);
                gameState.markerList.Add(new Marker(m.label, m.x, m.y));
            }
        }

        // regles speciales
        if (quest.rules != null)
            foreach (string r in quest.rules) gameState.rules.Add(ParseEnum<QuestRule>(r, "regle"));

        // recompense
        if (quest.reward != null)
        {
            gameState.reward.goldShared = quest.reward.goldShared;
            gameState.reward.goldEach = quest.reward.goldEach;
            if (quest.reward.bounties != null)
                foreach (BountyEntry b in quest.reward.bounties)
                    gameState.reward.bounties[ParseEnum<MonsterClass>(b.monster, "prime")] = b.gold;
        }

        // victoire
        Victory victory = new Victory();
        victory.mode = ParseEnum<VictoryMode>(quest.victory.mode, "mode de victoire");
        foreach (ObjectiveEntry o in quest.victory.objectives)
        {
            Objective objective = new Objective(ParseEnum<ObjectiveType>(o.type, "objectif"));
            objective.target = o.target;
            objective.count = o.count;
            objective.room = o.room;
            victory.objectives.Add(objective);
        }
        gameState.victory = victory;

        // brouillard : seule la salle de depart est visible
        string startZone = quest.start.type == "room" ? quest.start.room : map.Get(s.x, s.y).zone;
        foreach (Tile tile in map.tiles)
            tile.revealed = tile.zone == startZone;

        MarkRock(s.x, s.y, quest.rockZones);

        Debug.Log("Quete " + quest.number + " chargee : " + gameState.doorList.Count + " portes, "
            + gameState.furnitureList.Count + " meubles, " + gameState.monsterList.Count + " monstres, "
            + gameState.trapList.Count + " pieges, " + gameState.treasureList.Count + " tresors");
    }

    void AddDoor(DoorEntry d, bool isSecret, bool isFalse)
    {
        Side side = ParseEnum<Side>(d.side, "cote de porte");
        Door door = new Door(d.x, d.y, side, isSecret);
        door.isFalse = isFalse;
        Vector2Int n = Neighbor(d.x, d.y, side);

        if (!gameState.map.HasWall(d.x, d.y, n.x, n.y))
            throw new Exception("Porte (" + d.x + ", " + d.y + ", " + d.side + ") : il n'y a pas de mur a cet endroit");

        gameState.doorList.Add(door);
    }

    void AddMonster(MonsterEntry m)
    {
        MonsterClass monsterClass = ParseEnum<MonsterClass>(m.type, "monstre");
        MonsterData data = FindData(monsterClass);
        if (data == null)
        {
            Debug.LogWarning("Pas de MonsterData pour " + m.type + " : monstre (" + m.x + ", " + m.y + ") ignore");
            return;
        }

        // stats speciales (boss) : on copie le MonsterData pour ne pas modifier l'original
        if (m.movement > 0 || m.attack > 0 || m.defense > 0 || m.body > 0 || m.mind > 0)
        {
            data = ScriptableObject.Instantiate(data);
            if (m.movement > 0) data.movementCases = m.movement;
            if (m.attack > 0) data.attackDices = m.attack;
            if (m.defense > 0) data.defenseDices = m.defense;
            if (m.body > 0) data.health = m.body;
            if (m.mind > 0) data.spirit = m.mind;
        }

        GetTile(m.x, m.y, "monstre " + m.type);
        Monster monster = new Monster(data);
        monster.position = new Vector2Int(m.x, m.y);
        monster.id = m.id;
        monster.item = m.item;
        monster.isHidden = m.hidden;
        monster.isStatue = m.statue;

        // reveil : vide = aucun ; sinon il faut une salle qui existe
        if (!string.IsNullOrEmpty(m.wakeOn))
            monster.wakeOn = ParseEnum<MonsterTrigger>(m.wakeOn, "reveil");
        if (monster.wakeOn != MonsterTrigger.Aucun)
        {
            if (!ZoneExists(m.wakeRoom))
                throw new Exception("Monstre (" + m.x + ", " + m.y + ") : salle de reveil inconnue \"" + m.wakeRoom + "\"");
            monster.wakeRoom = m.wakeRoom;
        }

        gameState.monsterList.Add(monster);

        if (!string.IsNullOrEmpty(m.id)) monstersById[m.id] = monster;
    }

    // case de depart de H1 a H4 (heroStartCells[0] = H1) ; ancienne quete sans le champ = positions par defaut
    void AddHeroStarts(StartEntry start, RectEntry stairs)
    {
        gameState.heroStartCells = new List<Vector2Int>();

        if (start == null || start.heroes == null || start.heroes.Length == 0)
        {
            Debug.LogWarning("Pas de depart des heros dans la quete : positions par defaut sur l'escalier");
            gameState.heroStartCells.AddRange(HeroStartLayout.OnStairs(stairs.x, stairs.y, stairs.width, stairs.height, stairs.rotation));
            return;
        }

        Vector2Int?[] cells = new Vector2Int?[HeroStartLayout.Count];
        foreach (HeroStartEntry h in start.heroes)
        {
            string who = "Depart du heros H" + h.slot + " (" + h.x + ", " + h.y + ")";
            if (h.slot < 1 || h.slot > HeroStartLayout.Count) throw new Exception(who + " : le numero doit aller de 1 a " + HeroStartLayout.Count);
            if (cells[h.slot - 1] != null) throw new Exception(who + " : ce heros a deja une case de depart");

            Tile tile = GetTile(h.x, h.y, "depart du heros H" + h.slot);
            Vector2Int cell = new Vector2Int(h.x, h.y);
            if (tile.blocked || tile.furniture != null) throw new Exception(who + " : la case est bloquee ou couverte par un meuble");
            if (Array.IndexOf(cells, cell) >= 0) throw new Exception(who + " : deux heros sur la meme case");
            foreach (Monster m in gameState.monsterList)
                if (m.position == cell) throw new Exception(who + " : un monstre est deja sur la case");
            cells[h.slot - 1] = cell;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null) throw new Exception("Depart du heros H" + (i + 1) + " manquant");
            gameState.heroStartCells.Add(cells[i].Value);
        }
    }

    void AddTreasure(TreasureEntry t)
    {
        // les regles : on ne trouve des tresors que dans les salles
        Tile tile = GetTile(t.x, t.y, "tresor");
        if (tile.zone == ".")
            throw new Exception("Tresor (" + t.x + ", " + t.y + ") : la case est dans un couloir");

        Treasure treasure = new Treasure();
        treasure.x = t.x;
        treasure.y = t.y;
        treasure.room = tile.zone;
        treasure.gold = t.gold;
        treasure.item = t.item;
        treasure.isTrapped = t.trapped;
        treasure.trapDamage = t.trapDamage;

        if (t.requiredKills != null)
        {
            foreach (string id in t.requiredKills)
            {
                if (!monstersById.ContainsKey(id))
                    throw new Exception("Tresor (" + t.x + ", " + t.y + ") : monstre inconnu \"" + id + "\"");
                treasure.requiredKills.Add(id);
            }
        }

        gameState.treasureList.Add(treasure);
    }

    bool ZoneExists(string zone)
    {
        if (string.IsNullOrEmpty(zone)) return false;
        foreach (Tile tile in gameState.map.tiles)
        {
            if (tile.zone == zone) return true;
        }
        return false;
    }

    MonsterData FindData(MonsterClass monsterClass)
    {
        foreach (MonsterData data in monsterDatas)
        {
            if (data != null && data.monsterClass == monsterClass) return data;
        }
        return null;
    }

    // roche : toute case qu'on ne peut pas atteindre en passant par les portes (secretes comprises),
    // sans traverser de case bloquee ni une zone de rockZones
    // on part de la salle de depart ET des deux cotes de chaque porte : une salle qui a une porte
    // n'est jamais de la roche (quete 10 : on passe d'une salle a l'autre par teleportation)
    void MarkRock(int startX, int startY, string[] rockZones)
    {
        Map map = gameState.map;
        bool[,] reached = new bool[map.width, map.height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        Reach(startX, startY, rockZones, reached, queue);
        foreach (Door door in gameState.doorList)
        {
            if (door.isFalse) continue;
            Vector2Int n = Neighbor(door.x, door.y, door.side);
            Reach(door.x, door.y, rockZones, reached, queue);
            Reach(n.x, n.y, rockZones, reached, queue);
        }

        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            foreach (Side side in Enum.GetValues(typeof(Side)))
            {
                Vector2Int n = Neighbor(c.x, c.y, side);
                if (map.HasWall(c.x, c.y, n.x, n.y) && !HasDoor(c.x, c.y, n.x, n.y)) continue;
                Reach(n.x, n.y, rockZones, reached, queue);
            }
        }

        foreach (Tile tile in map.tiles)
            tile.rock = !reached[tile.x, tile.y] && !tile.blocked;
    }

    // marque la case comme atteinte et l'ajoute a la file, si on a le droit d'y aller
    void Reach(int x, int y, string[] rockZones, bool[,] reached, Queue<Vector2Int> queue)
    {
        Map map = gameState.map;
        if (!map.InBounds(x, y) || reached[x, y]) return;

        Tile tile = map.Get(x, y);
        if (tile.blocked) return;
        if (rockZones != null && Array.IndexOf(rockZones, tile.zone) >= 0) return;

        reached[x, y] = true;
        queue.Enqueue(new Vector2Int(x, y));
    }

    // une fausse porte ne laisse jamais passer
    bool HasDoor(int x1, int y1, int x2, int y2)
    {
        foreach (Door door in gameState.doorList)
        {
            if (!door.isFalse && door.IsBetween(x1, y1, x2, y2)) return true;
        }
        return false;
    }

    Vector2Int Neighbor(int x, int y, Side side)
    {
        if (side == Side.North) return new Vector2Int(x, y - 1);
        if (side == Side.South) return new Vector2Int(x, y + 1);
        if (side == Side.East) return new Vector2Int(x + 1, y);
        return new Vector2Int(x - 1, y);
    }

    // renvoie la case, ou une erreur claire si elle est hors du plateau
    Tile GetTile(int x, int y, string what)
    {
        Tile tile = gameState.map.Get(x, y);
        if (tile == null) throw new Exception(what + " hors du plateau : (" + x + ", " + y + ")");
        return tile;
    }

    // convertit un nom du JSON en enum, ou une erreur claire si le nom n'existe pas
    T ParseEnum<T>(string text, string what) where T : struct
    {
        T value;
        if (!Enum.TryParse(text, out value) || !Enum.IsDefined(typeof(T), value))
            throw new Exception(what + " inconnu : \"" + text + "\"");
        return value;
    }

    // charge une quete du livret : Resources/Quests/official_01.json ... official_14.json
    public void LoadQuest(int questNumber)
    {
        TextAsset file = Resources.Load<TextAsset>("Quests/official_" + questNumber.ToString("00"));
        if (file == null) throw new Exception("Quete " + questNumber + " introuvable dans Resources/Quests");
        Load(file.text);
    }
}
