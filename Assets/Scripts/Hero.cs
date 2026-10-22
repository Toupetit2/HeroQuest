using NUnit.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "Hero", menuName = "Scriptable Objects/Hero")]
public class Hero : ScriptableObject
{
    public HeroClass heroClass;
    public int attackDices;
    public int defenseDices;
    public int health;
    public int spirit;

    public int movementDice;
    public Weapon baseWeapon;
    public Armor baseArmor;

}
