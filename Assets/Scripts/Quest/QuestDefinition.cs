using System;

// moule d'un fichier de quete JSON (un fichier par quete, lu avec JsonUtility)
// les types sont ecrits en texte dans le JSON (JsonUtility ecrirait les enums en chiffres)
// et convertis en enum par le QuestLoader : seul lui utilise ces classes
[Serializable]
public class QuestDefinition
{
    public int number;
    public string title;
    public RectEntry stairs;            // escalier 2x2
    public StartEntry start;
    public DoorEntry[] doors;
    public DoorEntry[] secretDoors;
    public DoorEntry[] falseDoors;      // optionnel : ne s'ouvrent jamais (quete 12)
    public string[] rockZones;          // optionnel : zones toujours en roche (quete 10 : "." = tous les couloirs)
    public CellEntry[] blocked;         // une entree par case bloquee
    public FurnitureEntry[] furniture;
    public MonsterEntry[] monsters;
    public TrapEntry[] traps;
    public string wanderingMonster;     // nom de MonsterClass, ex : "Orc" ; vide = aucun (quete 10)
    public VictoryEntry victory;

    // tout ce qui suit est optionnel
    public string intro;                // texte lu aux joueurs au debut, ecrit par nous (pas le texte officiel)
    public TreasureEntry[] treasures;   // tresors speciaux des notes
    public AllyEntry[] allies;          // personnages a escorter
    public MarkerEntry[] markers;       // reperes X, XX, numeros...
    public string[] rules;              // noms de QuestRule
    public RewardEntry reward;
}

[Serializable]
public class CellEntry
{
    public int x;
    public int y;
}

[Serializable]
public class RectEntry
{
    public int x;          // case en haut a gauche
    public int y;
    public int width;
    public int height;
}

[Serializable]
public class StartEntry
{
    public string type;    // "stairs" ou "room"
    public string room;    // ex : "S7", seulement si type = "room"
}

// une porte est sur le bord "side" de la case (x, y)
[Serializable]
public class DoorEntry
{
    public int x;
    public int y;
    public string side;    // nom de Side : "North", "East", "South" ou "West"
}

[Serializable]
public class FurnitureEntry
{
    public string type;    // nom de FurnitureType, ex : "Table"
    public int x;          // case en haut a gauche
    public int y;
    public int width;      // en cases, deja tourne
    public int height;
    public int rotation;   // 0, 90, 180 ou 270 (affichage seulement)
}

[Serializable]
public class MonsterEntry
{
    public string id;      // optionnel, pour la victoire (ex : "verag")
    public string type;    // nom de MonsterClass, ex : "Gargouille"
    public int x;
    public int y;

    // stats speciales (boss) : 0 = valeur normale du MonsterData
    public int movement;
    public int attack;
    public int defense;
    public int body;
    public int mind;

    public string item;    // objet lache a sa mort
    public bool hidden;    // pas sur le plateau au depart
    public bool statue;    // statue jusqu'a son reveil
    public string wakeOn;  // nom de MonsterTrigger, vide = "Aucun"
    public string wakeRoom;
}

[Serializable]
public class TrapEntry
{
    public string type;    // nom de TrapType : "Fosse", "BlocQuiTombe" ou "Lance"
    public int x;
    public int y;
}

[Serializable]
public class VictoryEntry
{
    public string mode;                 // nom de VictoryMode : "Tous" ou "AuMoinsUn"
    public ObjectiveEntry[] objectives;
}

[Serializable]
public class ObjectiveEntry
{
    public string type;    // nom de ObjectiveType, ex : "Tuer", "Escalier"
    public string target;
    public int count;
    public string room;
}

// la case (x, y) donne la salle du tresor
[Serializable]
public class TreasureEntry
{
    public int x;
    public int y;
    public int gold;
    public string item;             // vide = aucun
    public bool trapped;
    public int trapDamage;
    public string[] requiredKills;  // ids des monstres a tuer avant
}

[Serializable]
public class AllyEntry
{
    public string id;      // ex : "ragnar"
    public int x;
    public int y;
    public int attack;
    public int defense;
    public int body;
}

[Serializable]
public class MarkerEntry
{
    public string label;   // ex : "X", "7", "Mine"
    public int x;
    public int y;
}

[Serializable]
public class RewardEntry
{
    public int goldShared;
    public int goldEach;
    public BountyEntry[] bounties;
}

[Serializable]
public class BountyEntry
{
    public string monster; // nom de MonsterClass
    public int gold;
}
