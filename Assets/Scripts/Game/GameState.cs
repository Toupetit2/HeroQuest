using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public List<Hero> heroList = new List<Hero>();
    public List<Monster> monsterList;
    public List<Door> doorList;
    public List<Furniture> furnitureList;
    public List<Trap> trapList;
    public RectInt stairs;
    public int stairsRotation;   // 0, 90, 180 ou 270 : rotation de l'image de l'escalier
    public MonsterClass wanderingMonster;
    public bool hasWanderingMonster;    // faux pour la quete 10
    public Victory victory;
    public List<Treasure> treasureList;
    public List<Ally> allyList;
    public List<Marker> markerList;
    public List<QuestRule> rules;
    public Reward reward;
    public string intro;                // texte lu aux joueurs au debut (ecrit par nous)
    public GameStatus gameStatus = GameStatus.Playing;
    public Map map;
}
