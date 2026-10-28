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

    public int GetAttackDice() 
    {
        return data.attackDices;
    }

    public int GetDefenseDice() 
    {
        return data.defenseDices;
    }

    public int GetHealth()
    {
        return currentHealth;
    }

    public void SetHealth(int newHealth) 
    {
        currentHealth = newHealth;
    }
}