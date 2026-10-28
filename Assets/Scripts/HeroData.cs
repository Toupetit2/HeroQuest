using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HeroData", menuName = "Scriptable Objects/Hero")]
public class HeroData : ScriptableObject
{
    public HeroClass heroClass;
    public int attackDices;
    public int defenseDices;
    public int health;
    public int spirit;

    public int movementDice;
    public Weapon baseWeapon;
    public Armor baseArmor;

    public bool canUseSpells;

    public int baseGold;
}
