using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Scriptable Objects/Weapon")]
public class Weapon : Equipment
{
    public int attackDice;
    public bool canAttackDiagonally;
    public bool canBeThrown;
    public bool lostWhenThrown;
}
