using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AttackSystem
{
    private GameManager gameManager;
    private GameState gameState;

    private List<Vector2Int> diagonals = new List<Vector2Int>();
    

    public AttackSystem(GameManager gameManager)
    {
        this.gameManager = gameManager;
        this.gameState = gameManager.GetGameState();

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

            if (Pathfinding.IsInRange(gameManager, hero.position, monster.position, 1))
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

    public int ResolveAttack(Hero hero, Monster target, int customAttackDice = -1) 
    {

        int totalAttack = 0;
        int heroAttackDices = 0;
        if (customAttackDice == -1) //Si pas de custom attaque
        {
            heroAttackDices = hero.GetAttackDice();
            heroAttackDices += hero.bonusAttackDice;
            hero.bonusAttackDice = 0;
        }
        else 
        {
            heroAttackDices = customAttackDice;
        }

        for (int i = 0; i < heroAttackDices; i++)
        {
            if (Dices.RollCombatDice() == CombatDiceFace.Skull) 
            {
                totalAttack += 1;
            }
        }

        int totalDefense = 0;

        if (!target.isSleeping) 
        { 
            for (int i = 0; i < target.GetDefenseDice(); i++)
            {
                if (Dices.RollCombatDice() == CombatDiceFace.BlackShield)
                {
                    totalDefense += 1;
                }
            }
        }

        target.currentHealth -= Mathf.Max(0, totalAttack - totalDefense);


        if (IsThrown(hero.position, target.position, hero.CanAttackDiagonaly())) 
        {
            foreach (Weapon weapon in hero.inventory.OfType<Weapon>().ToList())
            {
                if (weapon.lostWhenThrown)
                {
                    hero.inventory.Remove(weapon);
                    hero.equippedItems.Remove(weapon);
                    gameManager.DropLoot(weapon, target.position);
                }
            }
        }

        

        return target.currentHealth;
        
    }

    private bool IsThrown(Vector2Int pos1, Vector2Int pos2, bool canAttackDiagonaly)
    {
        int dx = Mathf.Abs(pos1.x - pos2.x);
        int dy = Mathf.Abs(pos1.y - pos2.y);

        if (canAttackDiagonaly)
        {
            return Mathf.Max(dx, dy) > 1;
        }

        return dx + dy > 1;
    }

    public int MonsterResolveAttack(Monster monster, Hero target) 
    {
        int totalAttack = 0;

        for (int i = 0; i < monster.GetAttackDice(); i++)
        {
            if (Dices.RollCombatDice() == CombatDiceFace.Skull)
            {
                totalAttack += 1;
            }
        }

        // GetDefenseDice compte deja les armures equipees
        int defenseDice = target.GetDefenseDice();

        int totalDefense = 0;

        for (int i = 0; i < defenseDice; i++)
        {
            if (Dices.RollCombatDice() == CombatDiceFace.WhiteShield)
            {
                totalDefense += 1;
            }
        }

        int damage = Mathf.Max(0, totalAttack - totalDefense);
        target.currentHealth -= damage;

        if (damage > 0)
        {
            target.bonusDefenseDice = 0;
        }

        return target.currentHealth;
    }
}
