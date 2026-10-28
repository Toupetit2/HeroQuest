using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class Hero
{
    public HeroData data;

    public int currentHealth;
    public int currentSpirit;
    public int currentGold;

    public List<Equipment> inventory;
    public List<Equipment> equippedItems;
    public List<SpellType> spells;

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
            equippedItems.Add(data.baseWeapon);
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
        int total = data.defenseDices;

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
        foreach (Weapon item in equippedItems) 
        {
            if (item.canBeThrown) 
            {
                return true;
            }
        }
        return false;
    }

    public bool CanAttackDiagonaly() 
    {
        foreach (Weapon item in equippedItems)
        {
            if (item.canAttackDiagonally)
            {
                return true;
            }
        }
        return false;
    }
}