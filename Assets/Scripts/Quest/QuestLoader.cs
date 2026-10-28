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

    public QuestLoader(GameState gameState, List<MonsterData> monsterDatas)
    {
        this.gameState = gameState;
        this.monsterDatas = monsterDatas;
    }

    public void Load(string json)
    {
        QuestDefinition quest = JsonUtility.FromJson<QuestDefinition>(json);
        Map map = gameState.map;

        gameState.monsterList = new List<Monster>();
        gameState.doorList = new List<Door>();
        gameState.furnitureList = new List<Furniture>();
        gameState.trapList = new List<Trap>();
        monstersById.Clear();

        // escalier
        RectEntry s = quest.stairs;
        gameState.stairs = new RectInt(s.x, s.y, s.width, s.height);

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

        gameState.wanderingMonster = ParseEnum<MonsterClass>(quest.wanderingMonster, "monstre errant");

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
            + gameState.trapList.Count + " pieges");
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
        gameState.monsterList.Add(monster);

        if (!string.IsNullOrEmpty(m.id)) monstersById[m.id] = monster;
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
}
