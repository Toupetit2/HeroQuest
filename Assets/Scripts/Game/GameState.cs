using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public List<Hero> heroList = new();
    public List<Monster> monsterList = new();
    public List<Door> doorList = new();
    public List<Furniture> furnitureList = new();
    public List<Trap> trapList = new();

    public RectInt stairs;
    public List<Vector2Int> heroStartCells = new List<Vector2Int>();   // case de depart de H1 a H4 ([0] = H1), rempli par le QuestLoader
    public int stairsRotation;   // 0, 90, 180 ou 270 : rotation de l'image de l'escalier
    public MonsterClass wanderingMonster;
    public bool hasWanderingMonster;    // faux pour la quete 10
    public Victory victory;
    public List<Treasure> treasureList = new();
    public List<Ally> allyList = new();
    public List<Marker> markerList = new();
    public List<QuestRule> rules = new();
    public Reward reward;
    public string intro;                // texte lu aux joueurs au debut (ecrit par nous)
    public GameStatus gameStatus = GameStatus.Playing;
    public List<string> killedMonsterIds = new List<string>();   // ids des monstres nommes deja tues (objectifs Tuer, tresors requiredKills)
    public Map map;
}
