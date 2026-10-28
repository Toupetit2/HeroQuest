using System.Collections.Generic;
using UnityEngine;

public class Dices
{
    public static List<CombatDiceFace> combatDiceFaces = new List<CombatDiceFace>
    {
        CombatDiceFace.Skull,
        CombatDiceFace.Skull,
        CombatDiceFace.Skull,
        CombatDiceFace.WhiteShield,
        CombatDiceFace.WhiteShield,
        CombatDiceFace.BlackShield
    };

    public static int RollRedDice(int NumberOfDices = 1) 
    {
        int result = 0;
        for (int i = 0; i < NumberOfDices; i++) 
        {
            result += Random.Range(1, 7);
        }
        return result;
    }

    public static CombatDiceFace RollCombatDice() 
    {
        return combatDiceFaces[Random.Range(0, 6)];
    }
}
