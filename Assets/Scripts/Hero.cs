using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Hero", menuName = "Scriptable Objects/Hero")]
public class Hero : ScriptableObject
{
    public HeroClass heroClass;
    public HeroData data;
    public List<SpellType> spells;
    public Vector2Int position;
    public int attackDices;
    public int defenseDices;
    public int health;
    public int spirit;

    public int movementDice;
    public Weapon baseWeapon;
    public Armor baseArmor;

}
