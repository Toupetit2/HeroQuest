using UnityEngine;
using System.Collections.Generic;

public class AttackSystem
{
    private GameState gameState;

    private List<Vector2Int> diagonals = new List<Vector2Int>();
    

    public AttackSystem(GameState gameState)
    {
        this.gameState = gameState;

        diagonals.Add(new Vector2Int(1, 1));
        diagonals.Add(new Vector2Int(-1, 1));
        diagonals.Add(new Vector2Int(1, -1));
        diagonals.Add(new Vector2Int(-1, -1));
    }

    public List<Monster> GetTargetInRange(Hero hero) 
    {
        List<Monster> inRange = new List<Monster>();
        foreach (Monster monster in gameState.monsterList) 
        {
            if (hero.CanThrow() && Raycast.CanSee(gameState.map, hero.position, monster.position)) 
            {
                inRange.Add(monster);
            }

            if (Pathfinding.IsInRange(gameState.map, hero.position, monster.position, 1))
            {
                inRange.Add(monster);
            }

            if (hero.CanAttackDiagonaly()) {
                foreach (Vector2Int offset in diagonals) 
                {
                    if (monster.position == hero.position + offset) 
                    {
                        inRange.Add(monster);
                    }
                }
            }
        }
        return inRange;
    }

    public bool CanAttack(Hero hero, Monster target) 
    {
        return true;
    }

    public int ResolveAttack(Hero hero, Monster target) 
    {

        int totalAttack = 0;

        for (int i = 0; i < hero.GetAttackDice(); i++)
        {
            if (Dices.RollCombatDice() == CombatDiceFace.Skull) 
            {
                totalAttack += 1;
            }
        }

        int totalDefense = 0;

        for (int i = 0; i < target.GetDefenseDice(); i++)
        {
            if (Dices.RollCombatDice() == CombatDiceFace.BlackShield)
            {
                totalDefense += 1;
            }
        }

        target.currentHealth -= totalAttack - totalDefense;

        return target.currentHealth;
        
    }
}
