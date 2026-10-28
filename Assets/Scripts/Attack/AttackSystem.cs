using UnityEngine;
using System.Collections.Generic;

public class AttackSystem
{
    private GameState gameState;

    public AttackSystem(GameState gameState)
    {
        this.gameState = gameState;
    }

    public List<Monster> GetTargetInRange(Hero hero) 
    {
        List<Monster> inRange = new List<Monster>();
        foreach (Monster monster in gameState.monsterList) 
        {
            if (Pathfinding.IsInRange(gameState.map, hero.position, monster.position, 1))  // Return si a 1 de distance
            {
                inRange.Add(monster);
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
