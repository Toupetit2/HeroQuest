using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Player
{
    public int playerID;
    public List<Hero> heroes = new List<Hero>();
}