using UnityEngine;

[CreateAssetMenu(fileName = "MonsterData", menuName = "Scriptable Objects/MonsterData")]
public class MonsterData : ScriptableObject
{
    public MonsterClass monsterClass;
    public MonsterTypes monsterType;

    public int attackDices;
    public int defenseDices;
    public int health;
    public int spirit;

    public int movementCases;
}
