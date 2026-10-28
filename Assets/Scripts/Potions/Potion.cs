using UnityEngine;

[CreateAssetMenu(fileName = "Potion", menuName = "Scriptable Objects/Potion")]
public class Potion : ScriptableObject
{
    public int healingDices;
    public bool attackTwice;
    public int bonusAttackDices;
}
