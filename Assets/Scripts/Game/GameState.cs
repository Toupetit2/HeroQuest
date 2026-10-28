using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public List<Hero> heroList;
    public List<Monster> monsterList;
    public GameStatus gameStatus = GameStatus.Playing;
    public Map map;
}
