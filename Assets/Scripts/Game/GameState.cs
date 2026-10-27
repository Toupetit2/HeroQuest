using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public List<Hero> heroList;
    public List<Monster> monsterList;
    public List<Door> doorList;
    public List<Furniture> furnitureList;
    public List<Trap> trapList;
    public GameStatus gameStatus = GameStatus.Playing;
    //MAP temp ?
    public Map map;
}
