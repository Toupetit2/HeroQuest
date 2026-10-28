using UnityEngine;

public class Monster
{
    public MonsterData data;
    public int currentHealth;
    public Vector2Int position;

    // rempli par le QuestLoader
    public string id;                   // nom dans la quete (ex : "verag"), vide sinon
    public string item;                 // objet lache a sa mort (ex : quete 11), vide = aucun
    public bool isHidden;               // pas encore sur le plateau (ex : Seigneur Sorcier, quete 12)
    public bool isStatue;               // ne bouge pas et ne peut pas etre blesse avant d'avoir agi
    public MonsterTrigger wakeOn;       // ce qui le fait apparaitre ou le reveille
    public string wakeRoom;             // salle concernee par wakeOn

    public Monster(MonsterData monsterData)
    {
        data = monsterData;

        currentHealth = data.health;
    }
}