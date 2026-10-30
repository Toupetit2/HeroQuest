using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class Hero
{
    public HeroData data;

    public int currentHealth;
    public int currentSpirit;
    public int currentGold;

    public int bonusAttackDice = 0;
    public int bonusMovementDice = 0;
    public bool canAttackTwice = false;
    public int movementDiceMultiplier = 1;

    public bool isInvisible = false;
    public bool isGhosted = false;

    public int bonusDefenseDice = 0;

    public bool isDead = false;     // mort : ne joue plus et n'occupe plus de case

    public List<Equipment> inventory = new List<Equipment>();
    public List<Equipment> equippedItems = new List<Equipment>();
    public List<SpellType> spells = new List<SpellType>();
    public List<Spell> spellList = new List<Spell>();

    public Vector2Int position;


    public Hero(HeroData heroData)
    {
        data = heroData;

        currentHealth = data.health;
        currentSpirit = data.spirit;
        currentGold = data.baseGold;

        if (data.baseWeapon != null) 
        {
            inventory.Add(data.baseWeapon);
            equippedItems.Add(data.baseWeapon);
        }
        if (data.baseArmor != null)
        {
            inventory.Add(data.baseArmor);
            equippedItems.Add(data.baseArmor);
        }
    }

    public int GetDefenseDice() 
    {
        int total = data.defenseDices;

        foreach (Equipment item in equippedItems)
        {
            if (item is Armor armor)
            {
                total += armor.defenseDice;
            }
        }

        return total;
    }

    public int GetAttackDice()
    {
        int total = data.attackDices;

        foreach (Equipment item in equippedItems)
        {
            if (item is Weapon weapon)
            {
                total += weapon.attackDice;
            }
        }

        return total;
    }

    public void GiveSpellType(SpellType type) 
    {
        spells.Add(type);
    }

    public bool CanThrow() 
    {
        foreach (Equipment item in equippedItems)
        {
            if (item is Weapon weapon && weapon.canBeThrown)
            {
                return true;
            }
        }
        return false;
    }

    public bool CanAttackDiagonaly() 
    {
        foreach (Equipment item in equippedItems)
        {
            if (item is Weapon weapon && weapon.canAttackDiagonally)
            {
                return true;
            }
        }
        return false;
    }

    public int GetHealth() 
    {
        return currentHealth;
    }
    public void SetHealth(int newHealth) 
    {
        currentHealth = newHealth;
    }

    public void AddEquipment(Equipment equipment) 
    {
        inventory.Add(equipment);
    }

    public void RemoveEquipment(Equipment equipment) 
    {
        inventory.Remove(equipment);
    }
}