using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameState", menuName = "Scriptable Objects/GameState")]
public class GameState : ScriptableObject
{
    public List<Hero> heroList;
    public GameStatus gameStatus = GameStatus.Playing;
    //MAP
}
