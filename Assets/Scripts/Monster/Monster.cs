using UnityEngine;

public class Monster
{
    public MonsterData data;
    public int currentHealth;
    public Vector2Int position;

    public Monster(MonsterData monsterData)
    {
        data = monsterData;

        currentHealth = data.health;
    }
}